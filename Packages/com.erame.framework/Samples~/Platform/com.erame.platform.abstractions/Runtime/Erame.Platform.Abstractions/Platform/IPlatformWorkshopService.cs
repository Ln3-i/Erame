using System.Collections.Generic;
using System.Threading.Tasks;

namespace Erame.Platform{
    /// <summary>创意工坊 / UGC 能力。</summary>
    public interface IPlatformWorkshopService{
        /// <summary>创建一个空条目，返回条目 ID。</summary>
        Task<ulong> CreateItemAsync();

        /// <summary>上传内容、设置元信息并提交审核，返回是否成功。</summary>
        Task<bool> SubmitAsync(ulong itemId, PlatformWorkshopUpdate update);

        int SubscribedCount { get; }
        IReadOnlyList<ulong> GetSubscribedItems();

        bool IsSubscribed(ulong itemId);
        Task<bool> SubscribeAsync(ulong itemId);
        Task<bool> UnsubscribeAsync(ulong itemId);

        bool IsInstalled(ulong itemId);
        bool TryGetInstallInfo(ulong itemId, out string folder, out ulong sizeOnDisk);
    }
}
