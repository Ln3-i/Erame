using Erame.Core;
using System.Threading.Tasks;

#if STEAMWORKS_AVAILABLE
using Steamworks;
#endif

namespace Erame.Platform.Steam{
#if STEAMWORKS_AVAILABLE
    /// <summary>
    /// Steam 平台服务。
    /// 该程序集仅在 STEAMWORKS_AVAILABLE（安装了 Steamworks.NET 包）且 USE_STEAM（构建开关打开）时参与编译，
    /// 因此核心运行时不需要硬引用 Steamworks。
    /// </summary>
    public class SteamService : IPlatformService, IService{
        private readonly ILogService _log;
        private readonly uint _appId;
        private bool _initialized;

        public SteamService(ILogService log, uint appId){
            _log = log;
            _appId = appId;
        }

        public bool IsInitialized => _initialized;
        public string PlayerId => _initialized ? SteamUser.GetSteamID().ToString() : "Unknown";
        public string PlayerName => _initialized ? SteamFriends.GetPersonaName() : "Unknown";

        public void Initialize(){
            if (_initialized) return;
#if !UNITY_EDITOR
            if (SteamAPI.RestartAppIfNecessary(new AppId_t(_appId))){
                _log.Error("[Steam] 请通过 Steam 客户端启动游戏！");
                return;
            }
#endif
            _initialized = SteamAPI.Init();

            if (_initialized){
                _log.Info($"[Steam] 初始化成功 (AppID: {_appId})！玩家: {PlayerName}");
            }
            else{
                _log.Warn("[Steam] 初始化失败！请检查 Steam 是否已登录，以及根目录是否有 steam_appid.txt。");
            }
        }

        public void Update(){
            if (_initialized){
                SteamAPI.RunCallbacks();
            }
        }

        public void Shutdown(){
            if (_initialized){
                SteamAPI.Shutdown();
                _initialized = false;
                _log.Info("[Steam] SteamAPI 已关闭");
            }
        }

        public void Dispose(){
            Shutdown();
        }

        public Task<bool> LoginAsync(){
            return Task.FromResult(_initialized);
        }

        public void UnlockAchievement(string achievementId){
            if (!_initialized) return;
            SteamUserStats.SetAchievement(achievementId);
            SteamUserStats.StoreStats();
            _log.Info($"[Steam] 解锁成就: {achievementId}");
        }

        public void SetStat(string statName, int value){
            if (!_initialized) return;
            SteamUserStats.SetStat(statName, value);
            SteamUserStats.StoreStats();
            _log.Info($"[Steam] 更新统计: {statName} = {value}");
        }

        public bool IsDlcInstalled(uint dlcId){
            if (!_initialized) return false;
            return SteamApps.BIsDlcInstalled(new AppId_t(dlcId));
        }
    }
#endif
}
