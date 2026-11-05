namespace Erame.Core{
    public interface IInputService{
        // 切换输入源（单机切联机、切回放时用）
        void SetSource(IInputSource source);
        
        // 业务逻辑每帧调用这个，拿当前输入
        InputFrame GetCurrentFrame();

        // 压入输入上下文（比如打开 UI）
        void PushContext(IInputContext context);
        
        // 弹出输入上下文（比如关闭 UI）
        void PopContext(IInputContext context);
    }
}