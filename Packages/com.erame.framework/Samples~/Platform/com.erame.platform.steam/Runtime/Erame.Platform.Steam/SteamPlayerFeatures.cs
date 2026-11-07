using System.Collections.Generic;
using Erame.Core;

#if STEAMWORKS_AVAILABLE
using Steamworks;
#endif

namespace Erame.Platform.Steam{
#if STEAMWORKS_AVAILABLE
    /// <summary>
    /// 玩家向能力集合：成就、统计、富状态、Overlay、好友、DLC。
    /// 无状态，不实现 IService（同一实例会以多个接口类型注册）。
    /// </summary>
    public sealed class SteamPlayerFeatures :
        IPlatformAchievementService,
        IPlatformStatsService,
        IPlatformPresenceService,
        IPlatformOverlayService,
        IPlatformFriendService,
        IPlatformDlcService{
        private const EFriendFlags FriendFlags = EFriendFlags.k_EFriendFlagImmediate;
        private const int NameBufferSize = 256;

        private readonly IPlatformService _platform;
        private readonly ISteamIntegration _integration;

        public SteamPlayerFeatures(IPlatformService platform, ISteamIntegration integration = null){
            _platform = platform;
            _integration = integration;
        }

        private bool Available => _platform != null && _platform.IsInitialized;

        /// <summary>
        /// 成就与统计必须等 <c>UserStatsReceived</c> 之后才能读写，否则 Steam 会静默丢弃。
        /// 没有注入 ISteamIntegration 时退回旧行为（不门控），便于单独使用。
        /// </summary>
        private bool StatsAvailable => Available && (_integration == null || _integration.StatsReady);

        // ---------------- 成就 ----------------

        int IPlatformAchievementService.Count => Available ? (int)SteamUserStats.GetNumAchievements() : 0;

        public string GetName(int index){
            return Available ? SteamUserStats.GetAchievementName((uint)index) : null;
        }

        public bool UnlockAchievement(string id){
            return StatsAvailable && SteamUserStats.SetAchievement(id);
        }

        public bool IsAchievementUnlocked(string id){
            if (!StatsAvailable) return false;
            return SteamUserStats.GetAchievement(id, out var achieved) && achieved;
        }

        public bool ClearAchievement(string id){
            return StatsAvailable && SteamUserStats.ClearAchievement(id);
        }

        public bool IndicateProgress(string id, int current, int max){
            if (max <= 0) return false;
            return StatsAvailable && SteamUserStats.IndicateAchievementProgress(id, (uint)current, (uint)max);
        }

        // ---------------- 统计 ----------------

        public bool SetInt(string name, int value){
            return StatsAvailable && SteamUserStats.SetStat(name, value);
        }

        public bool SetFloat(string name, float value){
            return StatsAvailable && SteamUserStats.SetStat(name, value);
        }

        public bool GetInt(string name, out int value){
            value = 0;
            return StatsAvailable && SteamUserStats.GetStat(name, out value);
        }

        public bool GetFloat(string name, out float value){
            value = 0f;
            return StatsAvailable && SteamUserStats.GetStat(name, out value);
        }

        public void Store(){
            if (StatsAvailable) SteamUserStats.StoreStats();
        }

        // ---------------- 富状态 ----------------

        public bool SetRichPresence(string key, string value){
            return Available && SteamFriends.SetRichPresence(key, value);
        }

        public void ClearRichPresence(){
            if (Available) SteamFriends.ClearRichPresence();
        }

        // ---------------- Overlay ----------------

        public void Open(string dialog){
            if (Available) SteamFriends.ActivateGameOverlay(dialog);
        }

        public void OpenUrl(string url){
            if (Available) SteamFriends.ActivateGameOverlayToWebPage(url);
        }

        public void OpenStore(uint appId){
            if (Available) SteamFriends.ActivateGameOverlayToStore(new AppId_t(appId), EOverlayToStoreFlag.k_EOverlayToStoreFlag_None);
        }

        public void OpenInviteDialog(string lobbyId){
            if (!Available) return;
            if (!ulong.TryParse(lobbyId, out var id)) return;
            SteamFriends.ActivateGameOverlayInviteDialog(new CSteamID(id));
        }

        // ---------------- 好友 ----------------

        public IReadOnlyList<PlatformFriend> GetFriends(){
            var result = new List<PlatformFriend>();
            if (!Available) return result;

            int count = SteamFriends.GetFriendCount(FriendFlags);
            for (int i = 0; i < count; i++){
                var id = SteamFriends.GetFriendByIndex(i, FriendFlags);
                var name = SteamFriends.GetFriendPersonaName(id);
                var state = SteamPresenceMap.FromSteamState((int)SteamFriends.GetFriendPersonaState(id));
                result.Add(new PlatformFriend(id.ToString(), name, state));
            }

            return result;
        }

        // ---------------- DLC ----------------

        int IPlatformDlcService.Count => Available ? SteamApps.GetDLCCount() : 0;

        public bool IsInstalled(uint dlcId){
            return Available && SteamApps.BIsDlcInstalled(new AppId_t(dlcId));
        }

        public bool TryGetInfo(int index, out PlatformDlcInfo info){
            info = default;
            if (!Available) return false;

            if (!SteamApps.BGetDLCDataByIndex(index, out var appId, out var available, out var name, NameBufferSize)){
                return false;
            }

            info = new PlatformDlcInfo(appId.m_AppId, available, name);
            return true;
        }
    }
#endif
}
