using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Erame.Platform{
    /// <summary>联机大厅能力。事件均在主线程回调。</summary>
    public interface IPlatformLobbyService{
        bool InLobby { get; }
        string CurrentLobbyId { get; }
        bool IsOwner { get; }
        string OwnerId { get; }
        int MemberCount { get; }

        IReadOnlyList<PlatformLobbyMember> GetMembers();

        Task<PlatformLobbyResult> CreateAsync(int maxMembers, bool isPrivate = false);
        Task<PlatformLobbyResult> JoinAsync(string lobbyId);
        void Leave();

        bool TryGetData(string key, out string value);
        bool SetData(string key, string value);

        bool SendChatMessage(string message);

        event Action LobbyEntered;
        event Action LobbyLeft;
        event Action<PlatformLobbyMember> MemberJoined;
        event Action<PlatformLobbyMember> MemberLeft;
        event Action<string, string> LobbyDataChanged;
    }
}
