namespace Erame.Platform{
    public enum PlatformPresenceState{
        Offline = 0,
        Online = 1,
        Busy = 2,
        Away = 3,
        Snooze = 4,
        LookingToTrade = 5,
        LookingToPlay = 6
    }

    public readonly struct PlatformFriend{
        public readonly string Id;
        public readonly string Name;
        public readonly PlatformPresenceState State;

        public PlatformFriend(string id, string name, PlatformPresenceState state){
            Id = id;
            Name = name;
            State = state;
        }
    }

    public readonly struct PlatformDlcInfo{
        public readonly uint Id;
        public readonly bool Available;
        public readonly string Name;

        public PlatformDlcInfo(uint id, bool available, string name){
            Id = id;
            Available = available;
            Name = name;
        }
    }

    public readonly struct PlatformLobbyMember{
        public readonly string Id;
        public readonly string Name;
        public readonly bool IsOwner;

        public PlatformLobbyMember(string id, string name, bool isOwner){
            Id = id;
            Name = name;
            IsOwner = isOwner;
        }
    }

    public readonly struct PlatformLobbyResult{
        public readonly bool Success;
        public readonly string LobbyId;
        public readonly string Error;

        private PlatformLobbyResult(bool success, string lobbyId, string error){
            Success = success;
            LobbyId = lobbyId;
            Error = error;
        }

        public static PlatformLobbyResult Ok(string lobbyId) => new PlatformLobbyResult(true, lobbyId, null);
        public static PlatformLobbyResult Fail(string error) => new PlatformLobbyResult(false, null, error);
    }

    /// <summary>创意工坊提交参数。</summary>
    public sealed class PlatformWorkshopUpdate{
        public string Title;
        public string Description;
        public string ContentFolder;
        public bool IsPublic = true;
        public string ChangeNote;
    }
}
