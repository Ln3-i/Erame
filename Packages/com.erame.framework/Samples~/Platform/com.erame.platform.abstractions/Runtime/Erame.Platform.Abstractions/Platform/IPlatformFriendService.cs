using System.Collections.Generic;

namespace Erame.Platform{
    /// <summary>好友列表能力。</summary>
    public interface IPlatformFriendService{
        IReadOnlyList<PlatformFriend> GetFriends();
    }
}
