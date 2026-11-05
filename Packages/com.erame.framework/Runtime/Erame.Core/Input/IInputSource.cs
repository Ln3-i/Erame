using System;

namespace Erame.Core{
    // 只要实现了这个接口，就可以作为“输入源”接入框架
    public interface IInputSource : IDisposable{
        // 获取这一帧的输入数据
        InputFrame GetInputFrame();
    }
}
