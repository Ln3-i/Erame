using System;
using Erame.Core;
using Erame.Runtime.Config;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 运行时编排器：负责入口的完整启动/驱动/退出。
    /// Start 幂等——重复调用第二次直接返回。
    /// 启动顺序：注册默认服务 → 业务注册（entry.Configure）→ 模块 Configure →
    /// Build + InitializeAll → OnStart；任何一步失败都回滚容器并保持未启动状态。
    /// </summary>
    public static class ErameRuntime{
        /// <summary>当前运行的入口。未启动时为 null。</summary>
        public static ErameEntry Entry { get; private set; }

        /// <summary>当前服务容器（诊断用）。未启动时为 null。</summary>
        public static ServiceContainer Container => Entry?.Container;

        /// <summary>是否已成功启动。</summary>
        public static bool IsStarted => Entry != null && Entry.IsStarted;

        public static void Start(ErameEntry entry, ErameBuilder builder = null){
            if (entry == null) throw new ArgumentNullException(nameof(entry));
            if (IsStarted) return;   // 幂等

            builder ??= new ErameBuilder();
            var settings = builder.Settings != null ? builder.Settings : ErameSettings.Default;
            var container = new ServiceContainer();

            Entry = entry;
            entry.Container = container;
            try{
                // 1. 注册默认服务（框架装配）
                ErameInstaller.InstallFramework(container, settings);
                ErameInstaller.InstallGameplay(container, settings, builder.InputSourceFactory);
                ErameInstaller.InstallPlatform(container, settings);

                // 2. 业务注册
                entry.Configure(container);

                // 3. 模块先收集后 Configure（收集于 builder）
                foreach (var module in builder.Modules){
                    module.Configure(container);
                }

                // 构建并初始化（容器按依赖拓扑序初始化，失败自动回滚）
                container.Build();
                container.InitializeAll();

                // 4. 启动完成
                entry.OnStart();
            }
            catch{
                entry.Container = null;
                Entry = null;
                container.Dispose();
                throw;
            }
        }

        /// <summary>主循环，由启动组件每帧调用。</summary>
        public static void Update(){
            Entry?.OnUpdate();
        }

        /// <summary>退出：OnQuit → 容器 Dispose。重复调用无效果。</summary>
        public static void Quit(){
            if (Entry == null) return;

            var entry = Entry;
            Entry = null;
            entry.OnQuit();
            entry.Container?.Dispose();
            entry.Container = null;
        }
    }
}
