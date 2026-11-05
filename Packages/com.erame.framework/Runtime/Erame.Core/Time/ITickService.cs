using System;

namespace Erame.Core{
    public interface ITickService{
        // 当前 Tick 的间隔（比如 0.1 秒算一 Tick）
        float TickInterval { get; set; }
        
        // 当前是第几个 Tick（游戏内时间流逝）
        long CurrentTick { get; }
        
        // 注册每个 Tick 触发的逻辑
        void AddTickListener(Action onTick);
        void RemoveTickListener(Action onTick);
        
        // 由主循环（GameBootstrap的Update）驱动，传入真实的 DeltaTime
        void Update(float deltaTime);
    }
}