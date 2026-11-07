using System;

namespace __NS__.Runtime{
    /// <summary>
    /// 输入门闸：与“输入帧长什么样”无关的模态信号。
    /// <para>
    /// UI 打开、对话进行、过场播放时，只需要“暂时吞掉游戏输入”，并不关心具体输入类型；
    /// 因此门闸独立于 <see cref="IInputService{TFrame}"/>，任何输入实现（新旧输入系统、
    /// 事件驱动、回放）都可以查询它。可嵌套：Block 返回的凭证 Dispose 时归还。
    /// </para>
    /// </summary>
    public interface IInputGate{
        /// <summary>当前叠加的拦截层数。</summary>
        int BlockCount { get; }

        /// <summary>是否处于拦截状态（BlockCount &gt; 0）。</summary>
        bool IsBlocked { get; }

        /// <summary>拦截状态发生变化时触发（true = 进入拦截）。</summary>
        event Action<bool> BlockedChanged;

        /// <summary>
        /// 申请一层拦截。owner 仅用于诊断/去重，可为 null。
        /// 返回的凭证 Dispose 时释放这一层；重复 Dispose 无副作用。
        /// </summary>
        IDisposable Block(object owner = null);
    }
}
