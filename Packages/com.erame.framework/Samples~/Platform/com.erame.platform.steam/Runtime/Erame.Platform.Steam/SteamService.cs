using System;
using System.Threading;
using System.Threading.Tasks;
using Erame.Core;

#if STEAMWORKS_AVAILABLE
using Steamworks;
#endif

namespace Erame.Platform.Steam{
#if STEAMWORKS_AVAILABLE
    /// <summary>
    /// Steam 平台身份与生命周期。
    /// 该程序集仅在 STEAMWORKS_AVAILABLE（安装了 Steamworks.NET）且 USE_STEAM（平台开关打开）时参与编译。
    /// 其余平台能力由同程序集内的 SteamPlayerFeatures / SteamLobbyService 等分别实现。
    ///
    /// 除了通用 <see cref="IPlatformService"/>，还实现 <see cref="ISteamIntegration"/> 暴露
    /// Overlay 状态、统计就绪、服务器连接、会话票据与客户端语言同步。
    /// </summary>
    public class SteamService : IPlatformService, IService, ISteamIntegration{
        private const int AuthTicketBufferSize = 1024;

        // Steamworks 的演示 AppID：该 App 只声明了 english，GetCurrentGameLanguage 恒返回 english，
        // 用 480 调试时必须以客户端 UI 语言为准，否则中文客户端会被判成英文。
        private const uint DevAppId = 480;

        private readonly ILogService _log;
        private readonly uint _appId;
        private readonly ILocalizationService _localization;
        private readonly bool _syncLanguage;

        private bool _initialized;
        private string _initializationError;
        private bool _statsReady;
        private bool _loggedOn;
        private bool _overlayActive;

        private HAuthTicket _authTicket;
        private bool _hasAuthTicket;

        private Callback<GameOverlayActivated_t> _overlayCallback;
        private Callback<UserStatsReceived_t> _statsCallback;
        private Callback<SteamServersConnected_t> _serversConnectedCallback;
        private Callback<SteamServersDisconnected_t> _serversDisconnectedCallback;

        /// <param name="log">日志服务。</param>
        /// <param name="appId">回退 AppID（steam_appid.txt 缺失时用于 RestartAppIfNecessary）。</param>
        /// <param name="localization">可选：用于按 Steam 客户端语言自动切换文案。</param>
        /// <param name="syncLanguage">是否在初始化成功后自动同步语言。</param>
        public SteamService(ILogService log, uint appId, ILocalizationService localization = null, bool syncLanguage = true){
            _log = log;
            _appId = appId;
            _localization = localization;
            _syncLanguage = syncLanguage;
        }

        public bool IsInitialized => _initialized;
        public string PlatformName => "Steam";
        public string PlayerId => _initialized ? SteamUser.GetSteamID().ToString() : "Unknown";
        public string PlayerName => _initialized ? SteamFriends.GetPersonaName() : "Unknown";

        public uint AppId => _initialized ? SteamUtils.GetAppID().m_AppId : _appId;
        public string InitializationError => _initializationError;
        public bool IsOverlayEnabled => _initialized && SteamUtils.IsOverlayEnabled();
        public bool IsOverlayActive => _overlayActive;
        public bool StatsReady => _initialized && _statsReady;
        public bool IsLoggedOn => _initialized && _loggedOn;

        public event Action<bool> OverlayActivated;
        public event Action<bool> ConnectionChanged;

        public void Initialize(){
            if (_initialized) return;

#if !UNITY_EDITOR
            if (SteamAPI.RestartAppIfNecessary(new AppId_t(_appId))){
                Fail("需要通过 Steam 客户端启动游戏（RestartAppIfNecessary 返回 true）。");
                return;
            }
#endif

            _initialized = SteamAPI.Init();
            if (!_initialized){
                Fail("SteamAPI.Init() 失败。请确认 Steam 客户端已登录、根目录有 steam_appid.txt，" +
                     "且 Plugins/x86_64/steam_api64.dll 已随 Steamworks.NET 导入。");
                return;
            }

            _initializationError = null;
            RegisterCallbacks();
            _loggedOn = SafeIsLoggedOn();

            var actualAppId = SteamUtils.GetAppID().m_AppId;
            if (actualAppId != _appId){
                _log.Warn($"[Steam] AppID 不一致：配置 {_appId}，Steam 客户端实际 {actualAppId}（以 steam_appid.txt 为准）。");
            }

            _log.Info($"[Steam] 初始化成功 (AppID: {actualAppId})！玩家: {PlayerName}");
            if (!IsOverlayEnabled){
                _log.Info("[Steam] Overlay 被禁用（Steam 客户端里可为该游戏开启）。");
            }

            RequestStats();

            if (_syncLanguage){
                SyncLanguageFromSteam();
            }
        }

        public void Update(){
            if (!_initialized) return;
            SteamAPI.RunCallbacks();
        }

        public void Shutdown(){
            if (!_initialized){
                DisposeCallbacks();
                return;
            }

            CancelAuthTicket();
            DisposeCallbacks();

            _initialized = false;
            _statsReady = false;
            _loggedOn = false;
            _overlayActive = false;

            SteamAPI.Shutdown();
            _log.Info("[Steam] SteamAPI 已关闭");
        }

        public void Dispose(){
            Shutdown();
        }

        public Task<bool> LoginAsync(){
            if (!_initialized){
                _log.Warn($"[Steam] 无法登录：{_initializationError ?? "SteamAPI 未初始化"}");
                return Task.FromResult(false);
            }

            return Task.FromResult(IsLoggedOn);
        }

        public bool RequestStats(){
            if (!_initialized) return false;

            try{
                // Steamworks SDK 1.57 起 RequestCurrentStats 已移除：统计数据在初始化后由 Steam 自动请求，
                // 需要刷新时按用户重新请求，结果仍走 UserStatsReceived_t 回调。
                SteamUserStats.RequestUserStats(SteamUser.GetSteamID());
                return true;
            }
            catch (Exception e){
                _log.Error($"[Steam] 请求统计数据失败: {e.Message}");
                return false;
            }
        }

        public string GetGameLanguage(){
            if (!_initialized) return null;

            try{
                return SteamApps.GetCurrentGameLanguage();
            }
            catch (Exception e){
                _log.Error($"[Steam] 读取客户端语言失败: {e.Message}");
                return null;
            }
        }

        public string GetClientLanguage(){
            if (!_initialized) return null;

            try{
                return SteamUtils.GetSteamUILanguage();
            }
            catch (Exception e){
                _log.Error($"[Steam] 读取客户端界面语言失败: {e.Message}");
                return null;
            }
        }

        public string SyncLanguageFromSteam(){
            var gameLanguage = GetGameLanguage();
            var clientLanguage = GetClientLanguage();
            var steamLanguage = SelectSteamLanguage(gameLanguage, clientLanguage);

            _log.Info($"[Steam] 语言探测：游戏语言={gameLanguage ?? "-"}，客户端语言={clientLanguage ?? "-"}，采用={steamLanguage ?? "-"}");
            if (string.IsNullOrEmpty(steamLanguage)) return null;

            var language = SteamLanguageMap.ToFrameworkLanguage(steamLanguage);
            if (language == null && !string.IsNullOrEmpty(clientLanguage) &&
                !string.Equals(clientLanguage, steamLanguage, StringComparison.OrdinalIgnoreCase)){
                // 游戏语言没有被我们支持（例如日语），退回客户端语言再试一次
                language = SteamLanguageMap.ToFrameworkLanguage(clientLanguage);
            }

            if (language == null){
                _log.Info($"[Steam] 语言 {steamLanguage} 没有对应的语言包，保持当前语言。");
                return null;
            }

            if (_localization == null){
                _log.Info("[Steam] 未注册本地化服务，跳过语言同步。");
                return null;
            }

            if (string.Equals(_localization.CurrentLanguage, language, StringComparison.OrdinalIgnoreCase)){
                return language;
            }

            _localization.SetLanguage(language);
            _log.Info($"[Steam] 已按客户端语言切换: {steamLanguage} -> {language}");
            return language;
        }

        /// <summary>
        /// 选一种"能代表玩家"的语言代码：
        /// 正式 App 以游戏语言为准（玩家可以在库里单独把某个游戏设成英文）；
        /// 演示 AppID 480 的游戏语言恒为 english，改用客户端界面语言。
        /// </summary>
        private string SelectSteamLanguage(string gameLanguage, string clientLanguage){
            if (_appId == DevAppId){
                return string.IsNullOrEmpty(clientLanguage) ? gameLanguage : clientLanguage;
            }

            return string.IsNullOrEmpty(gameLanguage) ? clientLanguage : gameLanguage;
        }

        public Task<byte[]> GetAuthSessionTicketAsync(CancellationToken cancellationToken = default){
            if (!_initialized) return Task.FromResult<byte[]>(null);

            if (cancellationToken.IsCancellationRequested){
                return Task.FromCanceled<byte[]>(cancellationToken);
            }

            try{
                var buffer = new byte[AuthTicketBufferSize];
                // 新版 Steamworks 要求显式指定票据用途（P2P / 服务器鉴权），默认空身份即可
                var identity = new SteamNetworkingIdentity();
                _authTicket = SteamUser.GetAuthSessionTicket(buffer, buffer.Length, out var size, ref identity);
                if (size == 0){
                    _log.Error("[Steam] 获取会话票据失败（返回长度为 0）。");
                    return Task.FromResult<byte[]>(null);
                }

                _hasAuthTicket = true;
                var ticket = new byte[size];
                Array.Copy(buffer, ticket, (int)size);
                return Task.FromResult(ticket);
            }
            catch (Exception e){
                _log.Error($"[Steam] 获取会话票据异常: {e.Message}");
                return Task.FromResult<byte[]>(null);
            }
        }

        public void CancelAuthTicket(){
            if (!_hasAuthTicket) return;

            _hasAuthTicket = false;
            if (!_initialized) return;

            try{
                SteamUser.CancelAuthTicket(_authTicket);
            }
            catch (Exception e){
                _log.Warn($"[Steam] 作废会话票据失败: {e.Message}");
            }
        }

        private void RegisterCallbacks(){
            _overlayCallback = new Callback<GameOverlayActivated_t>(OnOverlayActivated);
            _statsCallback = new Callback<UserStatsReceived_t>(OnUserStatsReceived);
            _serversConnectedCallback = new Callback<SteamServersConnected_t>(OnServersConnected);
            _serversDisconnectedCallback = new Callback<SteamServersDisconnected_t>(OnServersDisconnected);
        }

        private void DisposeCallbacks(){
            _overlayCallback?.Dispose();
            _statsCallback?.Dispose();
            _serversConnectedCallback?.Dispose();
            _serversDisconnectedCallback?.Dispose();

            _overlayCallback = null;
            _statsCallback = null;
            _serversConnectedCallback = null;
            _serversDisconnectedCallback = null;
        }

        private void OnOverlayActivated(GameOverlayActivated_t evt){
            _overlayActive = evt.m_bActive != 0;
            Raise(OverlayActivated, _overlayActive, nameof(OverlayActivated));
        }

        private void OnUserStatsReceived(UserStatsReceived_t evt){
            if (evt.m_eResult != EResult.k_EResultOK){
                _log.Error($"[Steam] 统计数据接收失败: {evt.m_eResult}");
                return;
            }

            if (!_initialized || evt.m_steamIDUser.m_SteamID != SteamUser.GetSteamID().m_SteamID){
                return;
            }

            if (_statsReady) return;

            _statsReady = true;
            _log.Info("[Steam] 成就/统计数据已就绪，可以安全读写。");
        }

        private void OnServersConnected(SteamServersConnected_t evt){
            _loggedOn = true;
            RequestStats();
            Raise(ConnectionChanged, true, nameof(ConnectionChanged));
        }

        private void OnServersDisconnected(SteamServersDisconnected_t evt){
            _loggedOn = false;
            _statsReady = false;
            Raise(ConnectionChanged, false, nameof(ConnectionChanged));
        }

        private bool SafeIsLoggedOn(){
            try{
                return SteamUser.BLoggedOn();
            }
            catch (Exception e){
                _log.Warn($"[Steam] 读取登录状态失败: {e.Message}");
                return false;
            }
        }

        private void Fail(string reason){
            _initializationError = reason;
            _log.Error($"[Steam] {reason}");
        }

        private void Raise<T>(Action<T> handler, T value, string name){
            if (handler == null) return;

            // 逐个订阅者隔离：单个业务回调抛异常不能拖垮 Steam 回调线程
            foreach (var d in handler.GetInvocationList()){
                try{
                    ((Action<T>)d).Invoke(value);
                }
                catch (Exception e){
                    _log.Error($"[Steam] {name} 订阅者抛出异常: {e.Message}");
                }
            }
        }
    }
#endif
}
