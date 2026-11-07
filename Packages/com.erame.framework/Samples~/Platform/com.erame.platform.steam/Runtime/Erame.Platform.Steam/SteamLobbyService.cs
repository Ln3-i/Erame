using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Erame.Core;

#if STEAMWORKS_AVAILABLE
using Steamworks;
#endif

namespace Erame.Platform.Steam{
#if STEAMWORKS_AVAILABLE
    /// <summary>
    /// Steam 联机大厅：创建/加入/离开、成员与数据变更、房间聊天。
    /// 除通用 <see cref="IPlatformLobbyService"/> 外还实现 <see cref="ISteamLobbyExtras"/>：
    /// 聊天接收、大厅邀请、被踢事件、人数上限、数据刷新与删除。
    /// </summary>
    public sealed class SteamLobbyService : IPlatformLobbyService, ISteamLobbyExtras, IService{
        private const int DataKeyBufferSize = 256;
        private const int DataValueBufferSize = 8192;
        private const int ChatBufferSize = 4096;

        private readonly ILogService _log;
        private readonly IPlatformService _platform;

        private Callback<LobbyEnter_t> _lobbyEnterCallback;
        private Callback<LobbyChatUpdate_t> _chatUpdateCallback;
        private Callback<LobbyDataUpdate_t> _dataUpdateCallback;
        private Callback<GameLobbyJoinRequested_t> _joinRequestedCallback;
        private Callback<LobbyChatMsg_t> _chatMessageCallback;
        private Callback<LobbyInvite_t> _lobbyInviteCallback;
        private Callback<LobbyKicked_t> _lobbyKickedCallback;

        private CallResult<LobbyCreated_t> _createCall;
        private CallResult<LobbyEnter_t> _joinCall;

        private TaskCompletionSource<PlatformLobbyResult> _createTcs;
        private TaskCompletionSource<PlatformLobbyResult> _joinTcs;

        private CSteamID _lobby = new CSteamID(0);
        private bool _inLobby;
        private readonly Dictionary<string, string> _lobbyData = new Dictionary<string, string>();
        private readonly byte[] _chatBuffer = new byte[ChatBufferSize];

        public SteamLobbyService(ILogService log, IPlatformService platform){
            _log = log;
            _platform = platform;
        }

        public event Action LobbyEntered;
        public event Action LobbyLeft;
        public event Action<PlatformLobbyMember> MemberJoined;
        public event Action<PlatformLobbyMember> MemberLeft;
        public event Action<string, string> LobbyDataChanged;
        public event Action<string, string> ChatMessageReceived;
        public event Action<string> LobbyInviteReceived;
        public event Action<bool> Kicked;

        private bool Available => _platform != null && _platform.IsInitialized;

        public bool InLobby => _inLobby && Available;
        public string CurrentLobbyId => InLobby ? _lobby.ToString() : null;
        public int MemberCount => InLobby ? SteamMatchmaking.GetNumLobbyMembers(_lobby) : 0;
        public bool IsOwner => InLobby && SteamMatchmaking.GetLobbyOwner(_lobby) == SteamUser.GetSteamID();
        public string OwnerId => InLobby ? SteamMatchmaking.GetLobbyOwner(_lobby).ToString() : null;
        public int MaxMembers => InLobby ? SteamMatchmaking.GetLobbyMemberLimit(_lobby) : 0;

        public void Initialize(){
            if (_lobbyEnterCallback != null) return;

            // SteamAPI 未初始化时注册回调不安全，直接跳过（后续所有调用都会被 Available 拦住）
            if (!Available){
                _log?.Warn("[Steam] Steam 未初始化，联机大厅回调未注册");
                return;
            }

            _lobbyEnterCallback = new Callback<LobbyEnter_t>(OnLobbyEnterCallback);
            _chatUpdateCallback = new Callback<LobbyChatUpdate_t>(OnChatUpdate);
            _dataUpdateCallback = new Callback<LobbyDataUpdate_t>(OnDataUpdate);
            _joinRequestedCallback = new Callback<GameLobbyJoinRequested_t>(OnJoinRequested);
            _chatMessageCallback = new Callback<LobbyChatMsg_t>(OnChatMessage);
            _lobbyInviteCallback = new Callback<LobbyInvite_t>(OnLobbyInvite);
            _lobbyKickedCallback = new Callback<LobbyKicked_t>(OnLobbyKicked);
        }

        public void Dispose(){
            if (_lobbyEnterCallback == null) return;

            _lobbyEnterCallback?.Dispose(); _lobbyEnterCallback = null;
            _chatUpdateCallback?.Dispose(); _chatUpdateCallback = null;
            _dataUpdateCallback?.Dispose(); _dataUpdateCallback = null;
            _joinRequestedCallback?.Dispose(); _joinRequestedCallback = null;
            _chatMessageCallback?.Dispose(); _chatMessageCallback = null;
            _lobbyInviteCallback?.Dispose(); _lobbyInviteCallback = null;
            _lobbyKickedCallback?.Dispose(); _lobbyKickedCallback = null;

            _createCall = null;
            _joinCall = null;
            _createTcs = null;
            _joinTcs = null;
            _lobbyData.Clear();
            _inLobby = false;
        }

        public Task<PlatformLobbyResult> CreateAsync(int maxMembers, bool isPrivate = false){
            if (!Available) return Task.FromResult(PlatformLobbyResult.Fail("Steam 未初始化"));
            if (_createTcs != null) return Task.FromResult(PlatformLobbyResult.Fail("已有创建请求进行中"));

            // 一次只能在一个大厅里：先离开旧的，避免状态错乱
            Leave();

            var tcs = new TaskCompletionSource<PlatformLobbyResult>();
            _createTcs = tcs;

            var type = isPrivate ? ELobbyType.k_ELobbyTypePrivate : ELobbyType.k_ELobbyTypePublic;
            var call = SteamMatchmaking.CreateLobby(type, Math.Max(1, maxMembers));
            _createCall = new CallResult<LobbyCreated_t>(OnLobbyCreatedResult);
            _createCall.Set(call);

            return tcs.Task;
        }

        public Task<PlatformLobbyResult> JoinAsync(string lobbyId){
            if (!Available) return Task.FromResult(PlatformLobbyResult.Fail("Steam 未初始化"));
            if (!ulong.TryParse(lobbyId, out var id)) return Task.FromResult(PlatformLobbyResult.Fail("大厅 ID 非法"));
            if (_joinTcs != null) return Task.FromResult(PlatformLobbyResult.Fail("已有加入请求进行中"));

            // 已在同一大厅里就直接成功，避免重复进入
            if (InLobby && _lobby.m_SteamID == id) return Task.FromResult(PlatformLobbyResult.Ok(_lobby.ToString()));

            // 切换大厅前先离开旧的（Steam 要求一次只能在一个大厅）
            Leave();

            var tcs = new TaskCompletionSource<PlatformLobbyResult>();
            _joinTcs = tcs;

            var call = SteamMatchmaking.JoinLobby(new CSteamID(id));
            _joinCall = new CallResult<LobbyEnter_t>(OnLobbyEnterResult);
            _joinCall.Set(call);

            return tcs.Task;
        }

        public void Leave(){
            // 先把未完成的请求收尾，否则调用方会永远等下去
            FailPending("已离开大厅");

            if (!_inLobby) return;

            SteamMatchmaking.LeaveLobby(_lobby);
            ClearLobbyState();
            LobbyLeft?.Invoke();
        }

        public IReadOnlyList<PlatformLobbyMember> GetMembers(){
            var list = new List<PlatformLobbyMember>();
            if (!InLobby) return list;

            var owner = OwnerId;
            int count = SteamMatchmaking.GetNumLobbyMembers(_lobby);
            for (int i = 0; i < count; i++){
                var id = SteamMatchmaking.GetLobbyMemberByIndex(_lobby, i);
                var idText = id.ToString();
                var name = SteamFriends.GetFriendPersonaName(id);
                if (string.IsNullOrEmpty(name)) name = idText;
                list.Add(new PlatformLobbyMember(idText, name, idText == owner));
            }

            return list;
        }

        public bool TryGetData(string key, out string value){
            value = null;
            return InLobby && _lobbyData.TryGetValue(key, out value);
        }

        public bool SetData(string key, string value){
            return InLobby && SteamMatchmaking.SetLobbyData(_lobby, key, value ?? string.Empty);
        }

        public bool RemoveData(string key){
            if (!InLobby || string.IsNullOrEmpty(key)) return false;
            if (!SteamMatchmaking.SetLobbyData(_lobby, key, string.Empty)) return false;

            if (_lobbyData.Remove(key)){
                LobbyDataChanged?.Invoke(key, null);
            }
            return true;
        }

        public bool SendChatMessage(string message){
            if (!InLobby || string.IsNullOrEmpty(message)) return false;
            var bytes = Encoding.UTF8.GetBytes(message);
            return SteamMatchmaking.SendLobbyChatMsg(_lobby, bytes, bytes.Length);
        }

        public void RefreshLobbyData(){
            RefreshLobbyDataInternal(true);
        }

        // ---------------- 回调 ----------------

        private void OnLobbyCreatedResult(LobbyCreated_t result, bool ioFailure){
            var tcs = _createTcs;
            _createTcs = null;
            _createCall = null;

            if (ioFailure || result.m_eResult != EResult.k_EResultOK){
                LogLobbyError("创建大厅", $"{result.m_eResult}", ioFailure);
                tcs?.TrySetResult(PlatformLobbyResult.Fail($"[Steam] 创建大厅失败: {(ioFailure ? "IO" : result.m_eResult.ToString())}"));
                return;
            }

            _lobby = new CSteamID(result.m_ulSteamIDLobby);
            _inLobby = true;
            tcs?.TrySetResult(PlatformLobbyResult.Ok(_lobby.ToString()));
            RaiseEntered();
        }

        private void OnLobbyEnterResult(LobbyEnter_t result, bool ioFailure){
            var tcs = _joinTcs;
            _joinTcs = null;
            _joinCall = null;

            if (ioFailure || result.m_EChatRoomEnterResponse != 1){
                LogLobbyError("加入大厅", result.m_EChatRoomEnterResponse.ToString(), ioFailure);
                tcs?.TrySetResult(PlatformLobbyResult.Fail($"[Steam] 加入大厅失败: {(ioFailure ? "IO" : result.m_EChatRoomEnterResponse.ToString())}"));
                return;
            }

            EnterLobby(new CSteamID(result.m_ulSteamIDLobby));
            tcs?.TrySetResult(PlatformLobbyResult.Ok(_lobby.ToString()));
        }

        private void OnLobbyEnterCallback(LobbyEnter_t result){
            // 通过好友邀请 / 接受邀请进入时不会经过 JoinAsync，这里补齐状态
            if (_inLobby && _lobby.m_SteamID == result.m_ulSteamIDLobby) return;
            EnterLobby(new CSteamID(result.m_ulSteamIDLobby));
        }

        private void OnChatUpdate(LobbyChatUpdate_t update){
            if (!InLobby || update.m_ulSteamIDLobby != _lobby.m_SteamID) return;

            var change = (EChatMemberStateChange)update.m_rgfChatMemberStateChange;
            var changed = new CSteamID(update.m_ulSteamIDUserChanged);
            var name = SteamFriends.GetFriendPersonaName(changed);
            var member = new PlatformLobbyMember(changed.ToString(), string.IsNullOrEmpty(name) ? changed.ToString() : name,
                                                 changed.ToString() == OwnerId);

            if ((change & EChatMemberStateChange.k_EChatMemberStateChangeEntered) != 0){
                MemberJoined?.Invoke(member);
            }
            else if ((change & (EChatMemberStateChange.k_EChatMemberStateChangeLeft |
                                EChatMemberStateChange.k_EChatMemberStateChangeDisconnected |
                                EChatMemberStateChange.k_EChatMemberStateChangeKicked |
                                EChatMemberStateChange.k_EChatMemberStateChangeBanned)) != 0){
                MemberLeft?.Invoke(member);
            }
        }

        private void OnDataUpdate(LobbyDataUpdate_t update){
            if (!InLobby || update.m_ulSteamIDLobby != _lobby.m_SteamID) return;
            if (update.m_bSuccess == 0) return;
            RefreshLobbyDataInternal(true);
        }

        private void OnJoinRequested(GameLobbyJoinRequested_t request){
            if (!Available) return;
            var _ = JoinAsync(request.m_steamIDLobby.ToString());
        }

        private void OnChatMessage(LobbyChatMsg_t evt){
            if (!InLobby || evt.m_ulSteamIDLobby != _lobby.m_SteamID) return;

            var written = SteamMatchmaking.GetLobbyChatEntry(
                _lobby, (int)evt.m_iChatID, out var chatter, _chatBuffer, _chatBuffer.Length, out _);
            if (written <= 0) return;

            var text = Encoding.UTF8.GetString(_chatBuffer, 0, written);
            ChatMessageReceived?.Invoke(chatter.ToString(), text);
        }

        private void OnLobbyInvite(LobbyInvite_t invite){
            LobbyInviteReceived?.Invoke(new CSteamID(invite.m_ulSteamIDLobby).ToString());
        }

        private void OnLobbyKicked(LobbyKicked_t kicked){
            if (!_inLobby || kicked.m_ulSteamIDLobby != _lobby.m_SteamID) return;

            ClearLobbyState();
            LobbyLeft?.Invoke();
            Kicked?.Invoke(kicked.m_bKickedDueToDisconnect != 0);
        }

        // ---------------- 内部 ----------------

        private void EnterLobby(CSteamID lobby){
            _lobby = lobby;
            _inLobby = true;
            RefreshLobbyDataInternal(false);
            RaiseEntered();
        }

        private void RaiseEntered(){
            LobbyEntered?.Invoke();
        }

        private void ClearLobbyState(){
            _inLobby = false;
            _lobby = new CSteamID(0);
            _lobbyData.Clear();
        }

        private void FailPending(string reason){
            _createTcs?.TrySetResult(PlatformLobbyResult.Fail(reason));
            _joinTcs?.TrySetResult(PlatformLobbyResult.Fail(reason));
            _createTcs = null;
            _joinTcs = null;
            _createCall = null;
            _joinCall = null;
        }

        private void LogLobbyError(string action, string detail, bool ioFailure){
            _log?.Error($"[Steam] {action}失败: {(ioFailure ? "IO 错误" : detail)}");
        }

        // 重建大厅数据并对外抛出变更；Steam 回调不告诉具体 key，只能做快照 diff
        private void RefreshLobbyDataInternal(bool raiseEvents){
            if (!InLobby) return;

            var snapshot = new Dictionary<string, string>();
            int count = SteamMatchmaking.GetLobbyDataCount(_lobby);
            for (int i = 0; i < count; i++){
                if (SteamMatchmaking.GetLobbyDataByIndex(_lobby, i, out var key, DataKeyBufferSize,
                                                        out var value, DataValueBufferSize)){
                    snapshot[key] = value;
                }
            }

            if (raiseEvents){
                foreach (var pair in snapshot){
                    if (!_lobbyData.TryGetValue(pair.Key, out var old) || old != pair.Value){
                        LobbyDataChanged?.Invoke(pair.Key, pair.Value);
                    }
                }
                foreach (var pair in _lobbyData){
                    if (!snapshot.ContainsKey(pair.Key)) LobbyDataChanged?.Invoke(pair.Key, null);
                }
            }

            _lobbyData.Clear();
            foreach (var pair in snapshot) _lobbyData[pair.Key] = pair.Value;
        }
    }
#endif
}
