namespace Erame.Platform{
    /// <summary>富状态（好友列表中展示的当前状态）。</summary>
    public interface IPlatformPresenceService{
        bool SetRichPresence(string key, string value);
        void ClearRichPresence();
    }
}
