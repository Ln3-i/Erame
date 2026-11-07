using System;

namespace Erame.Platform.Steam{
    /// <summary>
    /// 大厅相关的 Steam 独有能力：聊天接收、邀请、被踢、成员上限、数据刷新。
    /// 通用收发（创建/加入/离开/设置键值/发消息）仍在 <c>IPlatformLobbyService</c> 上。
    /// </summary>
    public interface ISteamLobbyExtras{
        /// <summary>房主设置的大厅人数上限（未进房时为 0）。</summary>
        int MaxMembers { get; }

        /// <summary>收到大厅聊天消息：发送者 SteamID + 文本。</summary>
        event Action<string, string> ChatMessageReceived;

        /// <summary>收到好友的大厅邀请（lobbyId）。</summary>
        event Action<string> LobbyInviteReceived;

        /// <summary>被移出大厅；参数 true 表示网络断开而非被房主踢出。</summary>
        event Action<bool> Kicked;

        /// <summary>强制重新拉取大厅全部键值并对差异发出 <c>LobbyDataChanged</c>。</summary>
        void RefreshLobbyData();

        /// <summary>删除一个大厅键（Steam 语义：设空值即删除）。</summary>
        bool RemoveData(string key);
    }
}
