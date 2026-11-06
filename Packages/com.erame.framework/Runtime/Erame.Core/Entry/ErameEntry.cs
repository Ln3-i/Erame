namespace Erame.Core{
    /// <summary>
    /// 纯 C# 入口（高级/内部使用）。普通 Unity 项目请直接继承 ErameMonoEntry，
    /// 它已封装单例去重、场景常驻与生命周期驱动。本类型仅用于无 UnityEngine 的宿主或测试。
    /// </summary>
    public abstract class ErameEntry : IErameEntry{
        /// <summary>服务容器。由 ErameRuntime 在启动过程中注入，未启动时为 null。</summary>
        public ServiceContainer Container { get; internal set; }

        /// <summary>是否已成功启动。</summary>
        public bool IsStarted => Container != null;

        /// <summary>解析已注册的服务。未启动时访问会抛异常。</summary>
        public T Get<T>() where T : class => Container.Get<T>();

        /// <inheritdoc />
        public abstract void Configure(ErameBuilder builder);

        /// <inheritdoc />
        public virtual void OnStart(){
        }

        /// <inheritdoc />
        public virtual void OnUpdate(float deltaTime){
        }

        /// <inheritdoc />
        public virtual void OnQuit(){
        }
    }
}
