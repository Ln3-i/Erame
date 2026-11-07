namespace Game.Runtime{
    public interface ITimeService{
        // 受时间缩放影响（游戏逻辑用）
        float DeltaTime { get; }
        float FixedDeltaTime { get; }
        
        // 不受时间缩放影响（UI动画、特效、背景音乐用）
        float UnscaledDeltaTime { get; }
        
        // 全局时间流速（0 = 暂停，1 = 正常，0.5 = 慢动作）
        float TimeScale { get; set; }
    }
}