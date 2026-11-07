using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Erame.Core;

#if STEAMWORKS_AVAILABLE
using Steamworks;
#endif

namespace Erame.Platform.Steam{
#if STEAMWORKS_AVAILABLE
    /// <summary>
    /// Steam 云存档（RemoteStorage）。同步 API 包装为 Task，保持与其它平台接口一致。
    /// 另实现 <see cref="ISteamCloudExtras"/>：文件列表、配额、时间戳、重命名。
    /// </summary>
    public sealed class SteamCloudService : IPlatformCloudService, ISteamCloudExtras, IService{
        private readonly IPlatformService _platform;

        public SteamCloudService(IPlatformService platform){
            _platform = platform;
        }

        private bool Available => _platform != null && _platform.IsInitialized;

        public bool IsEnabled => Available && SteamRemoteStorage.IsCloudEnabledForApp() &&
                                 SteamRemoteStorage.IsCloudEnabledForAccount();

        public void Initialize(){
            // 无自有状态；保留 IService 以便与其它服务统一管理
        }

        public void Dispose(){
        }

        public bool Exists(string fileName){
            return Available && !string.IsNullOrEmpty(fileName) && SteamRemoteStorage.FileExists(fileName);
        }

        public Task<bool> WriteAsync(string fileName, byte[] data){
            if (!Available || string.IsNullOrEmpty(fileName) || data == null) return Task.FromResult(false);
            return Task.FromResult(SteamRemoteStorage.FileWrite(fileName, data, data.Length));
        }

        public Task<byte[]> ReadAsync(string fileName){
            if (!Available || string.IsNullOrEmpty(fileName)) return Task.FromResult<byte[]>(null);

            int size = SteamRemoteStorage.GetFileSize(fileName);
            if (size <= 0) return Task.FromResult<byte[]>(null);

            var buffer = new byte[size];
            int read = SteamRemoteStorage.FileRead(fileName, buffer, size);
            if (read <= 0) return Task.FromResult<byte[]>(null);
            if (read < size) Array.Resize(ref buffer, read);

            return Task.FromResult(buffer);
        }

        public Task<bool> DeleteAsync(string fileName){
            if (!Available || string.IsNullOrEmpty(fileName)) return Task.FromResult(false);
            return Task.FromResult(SteamRemoteStorage.FileDelete(fileName));
        }

        // ---------------- 扩展能力 ----------------

        public IReadOnlyList<string> GetFileNames(){
            var list = new List<string>();
            if (!Available) return list;

            int count = SteamRemoteStorage.GetFileCount();
            for (int i = 0; i < count; i++){
                var name = SteamRemoteStorage.GetFileNameAndSize(i, out _);
                if (!string.IsNullOrEmpty(name)) list.Add(name);
            }
            return list;
        }

        public bool TryGetQuota(out ulong totalBytes, out ulong availableBytes){
            totalBytes = 0;
            availableBytes = 0;
            if (!Available) return false;
            return SteamRemoteStorage.GetQuota(out totalBytes, out availableBytes);
        }

        public DateTime? GetFileTimestamp(string fileName){
            if (!Available || string.IsNullOrEmpty(fileName)) return null;

            long seconds = SteamRemoteStorage.GetFileTimestamp(fileName);
            if (seconds <= 0) return null;

            return DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
        }

        public async Task<bool> RenameFileAsync(string oldName, string newName, CancellationToken cancellationToken = default){
            // Steam 没有提供重命名 API，用 读-写-删 模拟；任一步失败都保持旧文件不变
            if (!Available || string.IsNullOrEmpty(oldName) || string.IsNullOrEmpty(newName)) return false;
            if (cancellationToken.IsCancellationRequested) return false;

            var data = await ReadAsync(oldName);
            if (data == null) return false;

            var written = await WriteAsync(newName, data);
            if (!written) return false;

            return await DeleteAsync(oldName);
        }
    }
#endif
}
