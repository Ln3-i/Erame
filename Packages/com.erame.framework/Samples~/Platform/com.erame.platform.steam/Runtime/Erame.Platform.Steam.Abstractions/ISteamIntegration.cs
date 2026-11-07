using System;
using System.Threading;
using System.Threading.Tasks;

namespace Erame.Platform.Steam{
    /// <summary>
    /// Steam 独有的运行时能力：Overlay 状态、统计就绪、服务器连接、会话票据、客户端语言。
    /// 这些能力不属于通用平台接口，业务按需 <c>TryGet&lt;ISteamIntegration&gt;()</c>；
    /// 未接入 Steam 时自然取不到，不影响其它平台。
    /// </summary>
    public interface ISteamIntegration{
        /// <summary>Steam 实际使用的 AppID（来自 steam_appid.txt 或 Steam 客户端）。</summary>
        uint AppId { get; }

        /// <summary>初始化失败原因；成功时为 null。</summary>
        string InitializationError { get; }

        /// <summary>Overlay 是否可用（玩家可以在 Steam 客户端里关掉）。</summary>
        bool IsOverlayEnabled { get; }

        /// <summary>Overlay 当前是否展开。</summary>
        bool IsOverlayActive { get; }

        /// <summary>成就/统计数据是否已从云端取回；未就绪前读写成就都会被 Steam 拒绝。</summary>
        bool StatsReady { get; }

        /// <summary>是否已连接 Steam 服务器（离线模式下为 false）。</summary>
        bool IsLoggedOn { get; }

        /// <summary>Overlay 展开 / 收起（典型用法：展开时暂停游戏）。</summary>
        event Action<bool> OverlayActivated;

        /// <summary>与 Steam 服务器的连接状态变化。</summary>
        event Action<bool> ConnectionChanged;

        /// <summary>重新拉取统计数据（断线重连、或怀疑数据丢失时调用）。</summary>
        bool RequestStats();

        /// <summary>取得用于 P2P / 服务器鉴权的会话票据；失败返回 null。</summary>
        Task<byte[]> GetAuthSessionTicketAsync(CancellationToken cancellationToken = default);

        /// <summary>作废最近一次取得的会话票据。</summary>
        void CancelAuthTicket();

        /// <summary>Steam 客户端的游戏语言（如 schinese / english）。</summary>
        string GetGameLanguage();

        /// <summary>
        /// Steam 客户端界面语言（如 schinese / english）。
        /// 与 <see cref="GetGameLanguage"/> 的区别：前者是玩家的客户端设置，后者是"这个 App 声明当前以哪种语言运行"，
        /// 用演示 AppID 480 调试时后者恒为 english，不能代表玩家语言。
        /// </summary>
        string GetClientLanguage();

        /// <summary>把 Steam 客户端语言同步到本地化服务；返回切换到的语言，未切换返回 null。</summary>
        string SyncLanguageFromSteam();
    }
}
