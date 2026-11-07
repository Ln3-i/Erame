using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Erame.Core;

namespace Erame.Platform.Epic{
    /// <summary>
    /// Epic Online Services 占位实现：接口齐全、行为安全降级，用于在没有 SDK 时也能编译与运行。
    /// 接入 EOS SDK 时，替换本类内部实现即可，业务代码无需改动。
    /// </summary>
    public sealed class EpicService :
        IPlatformService,
        IPlatformAchievementService,
        IPlatformStatsService,
        IPlatformPresenceService,
        IPlatformOverlayService,
        IPlatformFriendService,
        IPlatformDlcService,
        IPlatformCloudService,
        IPlatformLobbyService,
        IPlatformWorkshopService,
        IService{
        private readonly ILogService _log;
        private bool _announced;

        public EpicService(ILogService log){
            _log = log;
        }

        public void Initialize(){
            // 同一实例会以多个能力接口类型注册，Initialize 会被多次调用，这里只提示一次
            if (_announced) return;
            _announced = true;
            _log.Warn("[Epic] 尚未接入 Epic Online Services SDK，平台能力按空实现降级。");
        }

        public void Dispose(){ }

        // ---------------- 身份 / 生命周期 ----------------

        public bool IsInitialized => false;
        public string PlatformName => "Epic";
        public string PlayerId => "Unknown";
        public string PlayerName => "Unknown";
        public void Update(){ }
        public void Shutdown(){ }
        public Task<bool> LoginAsync() => Task.FromResult(false);

        // ---------------- 成就 ----------------

        public int Count => 0;
        public string GetName(int index) => null;
        public bool UnlockAchievement(string id) => false;
        public bool IsAchievementUnlocked(string id) => false;
        public bool ClearAchievement(string id) => false;
        public bool IndicateProgress(string id, int current, int max) => false;
        public void Store(){ }

        // ---------------- 统计 ----------------

        public bool SetInt(string name, int value) => false;
        public bool SetFloat(string name, float value) => false;

        public bool GetInt(string name, out int value){
            value = 0;
            return false;
        }

        public bool GetFloat(string name, out float value){
            value = 0f;
            return false;
        }

        // ---------------- 富状态 / Overlay ----------------

        public bool SetRichPresence(string key, string value) => false;
        public void ClearRichPresence(){ }
        public void Open(string dialog){ }
        public void OpenUrl(string url){ }
        public void OpenStore(uint appId){ }
        public void OpenInviteDialog(string lobbyId){ }

        // ---------------- 好友 ----------------

        public IReadOnlyList<PlatformFriend> GetFriends() => Array.Empty<PlatformFriend>();

        // ---------------- DLC ----------------

        int IPlatformDlcService.Count => 0;
        public bool IsInstalled(uint dlcId) => false;

        public bool TryGetInfo(int index, out PlatformDlcInfo info){
            info = default;
            return false;
        }

        // ---------------- 云存档 ----------------

        public bool IsEnabled => false;
        public bool Exists(string fileName) => false;
        public Task<bool> WriteAsync(string fileName, byte[] data) => Task.FromResult(false);
        public Task<byte[]> ReadAsync(string fileName) => Task.FromResult<byte[]>(null);
        public Task<bool> DeleteAsync(string fileName) => Task.FromResult(false);

        // ---------------- 大厅 ----------------

        // 占位实现不会触发这些事件，抑制“从未使用”警告
#pragma warning disable CS0067
        public event Action LobbyEntered;
        public event Action LobbyLeft;
        public event Action<PlatformLobbyMember> MemberJoined;
        public event Action<PlatformLobbyMember> MemberLeft;
        public event Action<string, string> LobbyDataChanged;
#pragma warning restore CS0067

        public bool InLobby => false;
        public string CurrentLobbyId => null;
        public bool IsOwner => false;
        public string OwnerId => null;
        public int MemberCount => 0;

        public IReadOnlyList<PlatformLobbyMember> GetMembers() => Array.Empty<PlatformLobbyMember>();

        public Task<PlatformLobbyResult> CreateAsync(int maxMembers, bool isPrivate = false) =>
            Task.FromResult(PlatformLobbyResult.Fail("Epic 平台尚未接入"));

        public Task<PlatformLobbyResult> JoinAsync(string lobbyId) =>
            Task.FromResult(PlatformLobbyResult.Fail("Epic 平台尚未接入"));

        public void Leave(){ }
        public bool TryGetData(string key, out string value){ value = null; return false; }
        public bool SetData(string key, string value) => false;
        public bool SendChatMessage(string message) => false;

        // ---------------- 创意工坊 ----------------

        public Task<ulong> CreateItemAsync() => Task.FromResult(0UL);
        public Task<bool> SubmitAsync(ulong itemId, PlatformWorkshopUpdate update) => Task.FromResult(false);
        public int SubscribedCount => 0;
        public IReadOnlyList<ulong> GetSubscribedItems() => Array.Empty<ulong>();
        public bool IsSubscribed(ulong itemId) => false;
        public Task<bool> SubscribeAsync(ulong itemId) => Task.FromResult(false);
        public Task<bool> UnsubscribeAsync(ulong itemId) => Task.FromResult(false);
        public bool IsInstalled(ulong itemId) => false;
        public bool TryGetInstallInfo(ulong itemId, out string folder, out ulong sizeOnDisk){
            folder = null;
            sizeOnDisk = 0;
            return false;
        }
    }
}
