using System;
using System.Collections.Generic;
using Erame.Core;
using UnityEngine;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 运行时编排器：启动、每帧驱动、退出。
    /// 启动顺序：业务注册（entry.Configure）→ 模块统一 Configure（默认服务由各 *Module 在这里注册）
    /// → Build + InitializeAll → entry.OnStart。
    /// 每帧：entry.OnUpdate + 所有实现 IFrameUpdate 的服务。
    /// Start 幂等；启动失败会回滚容器并保持未启动状态。
    /// </summary>
    public static class ErameRuntime{
        private static bool _started;
        private static IReadOnlyList<IFrameUpdate> _updatables;

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

                // 1. 收集模块（模块只收集，统一 Configure 在下面执行）
                var builder = new ErameBuilder(new TaggedRegistrar(container, $"入口 {entry.GetType().Name}"));
                entry.Configure(builder);

                // 2. 统一 Configure（收集阶段已全部完成；打上模块来源标签供快照显示）
                foreach (var module in builder.ConsumeModules()){
                    module.Configure(new TaggedRegistrar(container, module.GetType().Name));
                }

                // 3. 构建、初始化、启动完成
                container.Build();
                container.InitializeAll();
                _updatables = container.GetAll<IFrameUpdate>();
                entry.OnStart();
                _started = true;
            }
            catch{
                _started = false;
                _updatables = null;
                Entry = null;
                Services = null;
                ShareContainer(entry, null);
                container.Dispose();
                throw;
            }
        }

        /// <summary>主循环：驱动入口与所有实现 IFrameUpdate 的服务（Tick、平台回调等）。</summary>
        public static void Update(float deltaTime){
            if (!_started) return;

            Entry.OnUpdate(deltaTime);

            if (_updatables != null){
                for (int i = 0; i < _updatables.Count; i++){
                    _updatables[i].Update(deltaTime);
                }
            }
        }

        /// <summary>退出：OnQuit → 容器 Dispose。重复调用无效果。</summary>
        public static void Quit(){
            if (!_started) return;

            var entry = Entry;
            var services = Services;
            _started = false;
            _updatables = null;
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

        /// <summary>
        /// 把注册行为打上来源标签的薄装饰器：容器快照据此显示“这个服务是谁注册的”
        /// （LogModule / 入口 GameEntry / UIModule / EconomyModule …）。
        /// </summary>
        private sealed class TaggedRegistrar : IServiceRegistrar{
            private readonly ServiceContainer _container;
            private readonly string _source;

            public TaggedRegistrar(ServiceContainer container, string source){
                _container = container;
                _source = source;
            }

            public void RegisterSingleton<T>(T instance) where T : class{
                _container.RegisterSingleton(instance);
                _container.NoteRegistrationSource(typeof(T), _source);
            }

            public void RegisterSingleton<T>(Func<Erame.Core.IServiceProvider, T> factory) where T : class{
                _container.RegisterSingleton(factory);
                _container.NoteRegistrationSource(typeof(T), _source);
            }
        }
    }
}
