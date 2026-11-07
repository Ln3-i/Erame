using System;

namespace __NS__.Runtime{
    /// <summary>
    /// 旧版输入源：固定返回 <see cref="InputFrame"/>，仅为兼容保留。
    /// 新项目请直接实现 <see cref="IInputSource{TFrame}"/>，用自己定义的输入帧类型。
    /// </summary>
    public interface IInputSource : IInputSource<InputFrame>{ }

    /// <summary>
    /// 输入源：每帧产出一次输入数据。
    /// <typeparamref name="TFrame"/> 由业务自定义，框架不关心它的字段
    /// （可以是光标位置、框选矩形、指令序列……）。
    /// </summary>
    public interface IInputSource<out TFrame> : IDisposable{
        TFrame GetInputFrame();
    }
}
