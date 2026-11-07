namespace __NS__.Runtime{
    /// <summary>
    /// 旧版输入上下文：固定返回 <see cref="InputFrame"/>，仅为兼容保留。
    /// </summary>
    public interface IInputContext : IInputContext<InputFrame>{ }

    /// <summary>
    /// 输入上下文：压入输入栈后参与“谁在消费输入”的仲裁。
    /// 优先级高者优先；第一个 <see cref="IsBlocking"/> 为 true 的上下文决定当前帧。
    /// </summary>
    public interface IInputContext<out TFrame>{
        int Priority { get; }

        bool IsBlocking { get; }

        TFrame GetInputFrame();
    }
}
