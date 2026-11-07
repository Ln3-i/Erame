namespace Erame.Platform{
    /// <summary>DLC 能力。</summary>
    public interface IPlatformDlcService{
        int Count { get; }

        bool IsInstalled(uint dlcId);
        bool TryGetInfo(int index, out PlatformDlcInfo info);
    }
}
