namespace Erame.Platform{
    /// <summary>统计数值能力。</summary>
    public interface IPlatformStatsService{
        bool SetInt(string name, int value);
        bool SetFloat(string name, float value);

        bool GetInt(string name, out int value);
        bool GetFloat(string name, out float value);

        /// <summary>把本地改动提交到平台服务端。</summary>
        void Store();
    }
}
