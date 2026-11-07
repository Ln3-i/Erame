namespace Erame.Core{
    /// <summary>
    /// 入口契约：同时被 <c>ErameMonoEntry</c>（Unity 场景组件，推荐）
    /// 与 <c>ErameEntry</c>（纯 C#，高级/内部使用）实现。
    /// 生命周期由 ErameRuntime 依序驱动：Configure → OnStart → OnUpdate（每帧）→ OnQuit。
    /// </summary>
    public interface IErameEntry{
        /// <summary>业务注册：在此注册业务自定义服务与模块；默认服务由模块在后续 Configure 阶段注册。</summary>
        void Configure(ErameBuilder builder);

        /// <summary>启动完成：容器 Build + InitializeAll 之后调用。</summary>
        void OnStart(){
        }

        /// <summary>主循环：由 ErameRuntime.Update 每帧驱动。</summary>
        void OnUpdate(float deltaTime){
        }

        /// <summary>退出清理：容器 Dispose 之前调用。</summary>
        void OnQuit(){
        }
    }
}
