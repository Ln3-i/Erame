using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Erame.Platform.Steam{
    /// <summary>
    /// Steam 云存档的独有能力：列出文件名、剩余配额、文件时间戳。
    /// 读写删除仍在 <c>IPlatformCloudService</c> 上。
    /// </summary>
    public interface ISteamCloudExtras{
        /// <summary>云端的全部文件名（含目录分隔的相对路径）。</summary>
        IReadOnlyList<string> GetFileNames();

        /// <summary>配额：总量与剩余可用字节；Steam 未启用云时返回 false。</summary>
        bool TryGetQuota(out ulong totalBytes, out ulong availableBytes);

        /// <summary>文件最后写入时间（UTC）；文件不存在返回 null。</summary>
        DateTime? GetFileTimestamp(string fileName);

        /// <summary>重命名云端文件（用于“存档槽位改名”这类操作）。</summary>
        Task<bool> RenameFileAsync(string oldName, string newName, CancellationToken cancellationToken = default);
    }
}
