using System.Threading.Tasks;

namespace Erame.Core{
    /// <summary>
    /// 游戏平台服务接口
    /// </summary>
    public interface IPlatformService{
        // 基础信息
        bool IsInitialized { get; }
        string PlayerId { get; }
        string PlayerName { get; }
        
        // 生命周期
        void Initialize();
        void Update(); // 有些平台 SDK 需要每帧驱动（如 EOS）
        void Shutdown();

        // 账号相关
        Task<bool> LoginAsync();

        // 成就/数据统计
        void UnlockAchievement(string achievementId);
        void SetStat(string statName, int value);

        // 平台专属功能（如 DLC 校验）
        bool IsDlcInstalled(uint dlcId);
    }
}