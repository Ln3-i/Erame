namespace Game.Runtime{
    /// <summary>
    /// 泛型输入服务：输入帧的形状由业务定义，框架只负责“谁在消费输入”的仲裁。
    /// <para>
    /// 示例：
    /// <code>
    /// public struct BuildFrame { public int CursorX, CursorY; public bool Confirm; }
    /// registrar.RegisterSingleton&lt;IInputService&lt;BuildFrame&gt;&gt;(p => new InputService&lt;BuildFrame&gt;(p.Get&lt;ILogService&gt;(), p.Get&lt;IInputGate&gt;()));
    /// </code>
    /// </para>
    /// 模态屏蔽（UI/对话/过场）请用 <see cref="IInputGate"/>，不要为每种玩法再造一个“空帧”上下文。
    /// </summary>
    public interface IInputService<TFrame>{
        /// <summary>设置默认输入源（通常是键鼠/手柄轮询）。换源会 Dispose 旧源。</summary>
        void SetSource(IInputSource<TFrame> source);

        /// <summary>取当前帧：门闸被拦或栈顶上下文屏蔽时返回 TFrame 的默认值。</summary>
        TFrame GetCurrentFrame();

        void PushContext(IInputContext<TFrame> context);

        void PopContext(IInputContext<TFrame> context);
    }
}
