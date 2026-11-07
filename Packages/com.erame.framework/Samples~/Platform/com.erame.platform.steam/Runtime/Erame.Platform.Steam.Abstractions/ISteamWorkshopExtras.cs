using System;
using System.Threading;
using System.Threading.Tasks;

namespace Erame.Platform.Steam{
    /// <summary>
    /// 创意工坊的 Steam 独有能力：下载/删除条目、下载与上传进度。
    /// 订阅、提交、安装信息仍在 <c>IPlatformWorkshopService</c> 上。
    /// </summary>
    public interface ISteamWorkshopExtras{
        /// <summary>开始（或继续）下载一个已订阅条目；已在下载时直接返回 true。</summary>
        Task<bool> DownloadItemAsync(ulong itemId, bool highPriority = false, CancellationToken cancellationToken = default);

        /// <summary>条目下载进度；返回 false 表示该条目不在下载队列里。</summary>
        bool TryGetDownloadProgress(ulong itemId, out ulong downloadedBytes, out ulong totalBytes);

        /// <summary>删除自己发布的条目（不可恢复）。</summary>
        Task<bool> DeleteItemAsync(ulong itemId, CancellationToken cancellationToken = default);

        /// <summary>最近一次 <c>SubmitAsync</c> 的上传进度；没有进行中的提交时返回 false。</summary>
        bool TryGetUpdateProgress(out ulong uploadedBytes, out ulong totalBytes);
    }
}
