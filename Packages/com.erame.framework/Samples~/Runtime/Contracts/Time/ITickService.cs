using System;

namespace __NS__.Runtime{
    public interface ITickService{
        // 当前 Tick 的间隔（比如 0.1 秒算一 Tick）
        float TickInterval { get; set; }
        
        // 当前是第几个 Tick（游戏内时间流逝）
        long CurrentTick { get; }

        /// <summary>是否暂停：暂停期间不累积逻辑时间，CurrentTick 保持不变。</summary>
        bool IsPaused { get; }

        /// <summary>
        /// 逻辑时间缩放：1 为正常速度，2 为两倍速，0.5 为半速。
        /// 与 <c>Time.timeScale</c>（影响渲染/物理）解耦，模拟类游戏可以“加速推演但不加速动画”。
        /// </summary>
        float TimeScale { get; set; }

        /// <summary>暂停逻辑 Tick（渲染与 UI 不受影响）。</summary>
        void Pause();

        /// <summary>恢复逻辑 Tick。</summary>
        void Resume();

        // 注册每个 Tick 触发的逻辑
        void AddTickListener(Action onTick);
        void RemoveTickListener(Action onTick);
        
        // 由主循环（GameBootstrap的Update）驱动，传入真实的 DeltaTime
        void Update(float deltaTime);
    }
}
