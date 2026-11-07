namespace Erame.Platform{
    /// <summary>成就能力。</summary>
    public interface IPlatformAchievementService{
        int Count { get; }

        string GetName(int index);
        bool UnlockAchievement(string id);
        bool IsAchievementUnlocked(string id);
        bool ClearAchievement(string id);

        /// <summary>展示进度型成就（不会解锁）。</summary>
        bool IndicateProgress(string id, int current, int max);

        /// <summary>把本地改动提交到平台服务端。</summary>
        void Store();
    }
}
