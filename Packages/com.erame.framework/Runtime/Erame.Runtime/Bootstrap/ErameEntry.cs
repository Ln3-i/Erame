using Erame.Core;
using Erame.Runtime.Config;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 应用入口抽象类。开发者只需重写 <see cref="Configure"/> 完成模块与服务注册，
    /// 再由场景中的 MonoBehaviour 驱动 <see cref="Start"/>/<see cref="Update"/>/<see cref="Quit"/>，
    /// 即可启动整个项目。
    /// </summary>
    public abstract class ErameEntry{
        /// <summary>本次启动使用的框架配置；未显式传入时为运行时默认配置。</summary>
        public ErameSettings Settings { get; private set; }

        /// <summary>服务容器。未启动时为 null。</summary>
        public ServiceContainer Container { get; private set; }

        /// <summary>是否已成功启动。</summary>
        public bool IsStarted => Container != null;

        /// <summary>
        /// 启动：Configure（注册模块与服务）→ Build → InitializeAll → OnStarted。
        /// 重复调用无效果；启动失败会回滚容器并抛出异常。
        /// </summary>
        public void Start(ErameSettings settings = null){
            if (IsStarted) return;

            Settings = settings != null ? settings : ErameSettings.Default;
            var container = new ServiceContainer();
            Container = container;

            try{
                Configure(container, Settings);
                container.Build();
                container.InitializeAll();
                OnStarted();
            }
            catch{
                // 启动失败：回滚容器，保持未启动状态，允许修复后重试
                Container = null;
                container.Dispose();
                throw;
            }
        }

        /// <summary>主循环，由 MonoBehaviour 的 Update 驱动。</summary>
        public void Update(){
            if (!IsStarted) return;
            OnUpdate();
        }

        /// <summary>释放服务容器。</summary>
        public void Quit(){
            if (Container == null) return;

            var container = Container;
            Container = null;
            container.Dispose();
        }

        /// <summary>解析已注册的服务。未启动时访问会抛异常。</summary>
        public T Get<T>() where T : class => Container.Get<T>();

        /// <summary>
        /// 注册模块与服务。开发者重写此方法：
        /// 可逐个注册 <see cref="IModule"/>，或直接调用 <see cref="ErameInstaller"/> 的装配方法，
        /// 也可用 <see cref="IServiceRegistrar.RegisterSingleton{T}(System.Func{IServiceProvider,T})"/> 注册业务自定义服务。
        /// </summary>
        protected abstract void Configure(ServiceContainer container, ErameSettings settings);

        /// <summary>启动完成后的收尾钩子，例如注册游戏流程状态。</summary>
        protected virtual void OnStarted(){
        }

        /// <summary>
        /// 默认主循环：Tick → 流程状态机 → 平台。
        /// 这些服务缺失时会抛异常（属配置错误，尽早暴露）；确有需要可重写。
        /// </summary>
        protected virtual void OnUpdate(){
            Container.Get<ITickService>().Update(Container.Get<ITimeService>().DeltaTime);
            Container.Get<IGameStateMachine>().Update();
            Container.Get<IPlatformService>().Update();
        }
    }
}
