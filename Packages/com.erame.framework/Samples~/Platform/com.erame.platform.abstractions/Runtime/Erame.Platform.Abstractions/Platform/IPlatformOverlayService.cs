namespace Erame.Platform{
    /// <summary>平台 Overlay（游戏内浮层）能力。</summary>
    public interface IPlatformOverlayService{
        /// <summary>打开内置对话，如 "friends"、"achievements"、"community"。</summary>
        void Open(string dialog);

        void OpenUrl(string url);
        void OpenStore(uint appId);

        /// <summary>打开邀请好友加入大厅的浮层。</summary>
        void OpenInviteDialog(string lobbyId);
    }
}
