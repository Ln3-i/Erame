using System;
using Erame.Core;
using Erame.Runtime.Config;
using Erame.Runtime.Services;
using UnityEngine;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 运行时编排器：启动、每帧驱动、退出。
    /// 启动顺序：注册默认服务（日志/事件/时间）→ 业务注册（entry.Configure）→
    /// 模块统一 Configure → Build + InitializeAll → entry.OnStart。
    /// Start 幂等；启动失败会回滚容器并保持未启动状态。
    /// </summary>
    public static class ErameRuntime{
        private static bool _started;

        /// <summary>当前入口。未启动时为 null。</summary>
        public static IErameEntry Entry { get; private set; }

        /// <summary>当前服务容器（诊断用）。未启动时为 null。</summary>
        public static ServiceContainer Services { get; private set; }

        /// <summary>是否已成功启动。</summary>
        public static bool IsStarted => _started;

        public static void Start(IErameEntry entry){
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (_started) return;   // 幂等

            var container = new ServiceContainer();
            container.ErrorReporter = message => Debug.LogError(message);
            Services = container;
            Entry = entry;

            try{
                ShareContainer(entry, container);

                // 1. 默认服务
                var settings = ErameSettings.Default;
                container.RegisterSingleton<ILogService>(_ => new UnityLogService{ MinLevel = settings.LogLevel });
                container.RegisterSingleton<IEventBus>(p => new EventBus(p.Get<ILogService>()));
                container.RegisterSingleton<ITimeService>(p => new UnityTimeService(p.Get<ILogService>()));

                // 2. 业务注册
                var builder = new ErameBuilder(container);
                entry.Configure(builder);

                // 3. 模块统一 Configure（收集阶段已完成）
                foreach (var module in builder.ConsumeModules()){
                    module.Configure(container);
                }

                // 4. 构建、初始化、启动完成
                container.Build();
                container.InitializeAll();
                entry.OnStart();
                _started = true;
            }
            catch{
                _started = false;
                Entry = null;
                Services = null;
                ShareContainer(entry, null);
                container.Dispose();
                throw;
            }
        }

        /// <summary>主循环：驱动入口与 Tick 服务。</summary>
        public static void Update(float deltaTime){
            if (!_started) return;

            Entry.OnUpdate(deltaTime);
            if (Services.TryGet<ITickService>(out var tick)) tick.Update(deltaTime);
        }

        /// <summary>退出：OnQuit → 容器 Dispose。重复调用无效果。</summary>
        public static void Quit(){
            if (!_started) return;

            var entry = Entry;
            var services = Services;
            _started = false;
            Entry = null;
            Services = null;

            try{
                entry.OnQuit();
            }
            finally{
                ShareContainer(entry, null);
                services.Dispose();
            }
        }

        /// <summary>纯 C# 入口使用容器；Mono 入口由自身持有生命周期，无需注入。</summary>
        private static void ShareContainer(IErameEntry entry, ServiceContainer container){
            if (entry is ErameEntry classic) classic.Container = container;
        }
    }
}
