namespace Erame.Core{
    /// <summary>
    /// 应用入口抽象类（纯 C#，不依赖 UnityEngine）。
    /// 具体项目重写此类型来承载业务注册与生命周期；
    /// 生命周期由 Erame.Runtime 的 ErameRuntime 依序驱动：
    /// Configure → OnStart → OnUpdate（每帧） → OnQuit。
    /// </summary>
    public abstract class ErameEntry{
        /// <summary>服务容器。由 ErameRuntime 在启动过程中注入，未启动时为 null。</summary>
        public ServiceContainer Container { get; internal set; }

        /// <summary>是否已成功启动。</summary>
        public bool IsStarted => Container != null;

        /// <summary>解析已注册的服务。未启动时访问会抛异常。</summary>
        public T Get<T>() where T : class => Container.Get<T>();

        /// <summary>虚方法 1 · 业务注册：框架默认服务已注册完毕，可在此注册业务自定义服务。</summary>
        public virtual void Configure(ServiceContainer container){
        }

        /// <summary>虚方法 2 · 启动完成：容器 Build + InitializeAll 之后调用，用于启动收尾。</summary>
        public virtual void OnStart(){
        }

        /// <summary>虚方法 3 · 主循环：由 ErameRuntime.Update 每帧驱动。</summary>
        public virtual void OnUpdate(){
        }

        /// <summary>虚方法 4 · 退出清理：容器 Dispose 之前调用。</summary>
        public virtual void OnQuit(){
        }
    }
}
