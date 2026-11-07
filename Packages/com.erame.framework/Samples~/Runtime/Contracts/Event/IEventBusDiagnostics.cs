using System;
using System.Collections.Generic;

namespace __NS__.Runtime{
    /// <summary>
    /// 事件总线诊断契约：只给编辑器工具与调试面板读取订阅情况，业务代码仍然通过 <see cref="IEventBus"/> 收发事件。
    /// </summary>
    /// <remarks>
    /// 这条契约属于业务层而不是底座：底座不假设"一定有事件总线"。
    /// 删除事件总线时，把本文件与 IEventBus/EventBus 一起删掉即可，不会在 Erame.Core 里留下无人实现的接口。
    /// </remarks>
    public interface IEventBusDiagnostics{
        IReadOnlyList<KeyValuePair<Type, int>> GetSubscriptionCounts();
    }
}
