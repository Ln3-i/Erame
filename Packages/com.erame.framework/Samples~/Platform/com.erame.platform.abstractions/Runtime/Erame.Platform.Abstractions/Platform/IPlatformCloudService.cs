using System.Threading.Tasks;

namespace Erame.Platform{
    /// <summary>平台云存档能力（与本地 ISaveService 独立，可按需选用）。</summary>
    public interface IPlatformCloudService{
        bool IsEnabled { get; }

        bool Exists(string fileName);
        Task<bool> WriteAsync(string fileName, byte[] data);
        Task<byte[]> ReadAsync(string fileName);
        Task<bool> DeleteAsync(string fileName);
    }
}
