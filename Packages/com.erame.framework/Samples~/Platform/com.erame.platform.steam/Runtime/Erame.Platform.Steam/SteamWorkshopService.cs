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
    /// Steam 创意工坊（UGC）：创建条目、上传并提交、订阅与安装信息查询。
    /// 另实现 <see cref="ISteamWorkshopExtras"/>：下载、删除、下载/上传进度。
    /// </summary>
    public sealed class SteamWorkshopService : IPlatformWorkshopService, ISteamWorkshopExtras, IService{
        private const int FolderBufferSize = 512;

        private readonly ILogService _log;
        private readonly IPlatformService _platform;

        // CallResult 必须保持引用直到回调完成，否则会被 GC 回收
        private readonly List<object> _pendingCalls = new List<object>();

        private UGCUpdateHandle_t _updateHandle;
        private bool _submitting;

        public SteamWorkshopService(ILogService log, IPlatformService platform){
            _log = log;
            _platform = platform;
        }

        private bool Available => _platform != null && _platform.IsInitialized;

        public void Initialize(){
            // 无自有状态需要预热；保留 IService 是为了和其它服务一起被容器管理
        }

        public void Dispose(){
            _pendingCalls.Clear();
            _submitting = false;
            _updateHandle = default;
        }

        public Task<ulong> CreateItemAsync(){
            if (!Available) return Task.FromResult(0UL);

            var tcs = new TaskCompletionSource<ulong>();
            CallResult<CreateItemResult_t> result = null;
            result = new CallResult<CreateItemResult_t>((created, ioFailure) => {
                _pendingCalls.Remove(result);
                if (ioFailure || created.m_eResult != EResult.k_EResultOK){
                    _log?.Warn($"[Steam] 创建创意工坊条目失败: {(ioFailure ? "IO" : created.m_eResult.ToString())}");
                    tcs.TrySetResult(0UL);
                    return;
                }
                tcs.TrySetResult(created.m_nPublishedFileId.m_PublishedFileId);
            });
            _pendingCalls.Add(result);

            var call = SteamUGC.CreateItem(SteamUtils.GetAppID(), EWorkshopFileType.k_EWorkshopFileTypeCommunity);
            result.Set(call);
            return tcs.Task;
        }

        public Task<bool> SubmitAsync(ulong itemId, PlatformWorkshopUpdate update){
            if (!Available || update == null) return Task.FromResult(false);
            if (_submitting) return Task.FromResult(false);

            var fileId = new PublishedFileId_t(itemId);
            var handle = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), fileId);

            SteamUGC.SetItemTitle(handle, update.Title ?? string.Empty);
            SteamUGC.SetItemDescription(handle, update.Description ?? string.Empty);
            if (!string.IsNullOrEmpty(update.ContentFolder)){
                SteamUGC.SetItemContent(handle, update.ContentFolder);
            }
            SteamUGC.SetItemVisibility(handle,
                update.IsPublic
                    ? ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic
                    : ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate);

            var tcs = new TaskCompletionSource<bool>();
            CallResult<SubmitItemUpdateResult_t> result = null;
            result = new CallResult<SubmitItemUpdateResult_t>((submitted, ioFailure) => {
                _pendingCalls.Remove(result);
                _submitting = false;
                _updateHandle = default;

                var ok = !ioFailure && submitted.m_eResult == EResult.k_EResultOK;
                if (!ok){
                    _log?.Warn($"[Steam] 提交创意工坊条目失败: {(ioFailure ? "IO" : submitted.m_eResult.ToString())}");
                }
                else if (submitted.m_bUserNeedsToAcceptWorkshopLegalAgreement){
                    // 没接受协议时内容不会公开，对调用方来说等同失败
                    _log?.Warn("[Steam] 需要先在 Overlay 中接受创意工坊协议，本次提交不会对外可见");
                    ok = false;
                }

                tcs.TrySetResult(ok);
            });
            _pendingCalls.Add(result);

            _updateHandle = handle;
            _submitting = true;
            result.Set(SteamUGC.SubmitItemUpdate(handle, update.ChangeNote ?? string.Empty));
            return tcs.Task;
        }

        public int SubscribedCount => Available ? (int)SteamUGC.GetNumSubscribedItems() : 0;

        public IReadOnlyList<ulong> GetSubscribedItems(){
            var list = new List<ulong>();
            if (!Available) return list;

            uint count = SteamUGC.GetNumSubscribedItems();
            if (count == 0) return list;

            var ids = new PublishedFileId_t[count];
            uint got = SteamUGC.GetSubscribedItems(ids, count);
            for (uint i = 0; i < got && i < ids.Length; i++){
                list.Add(ids[i].m_PublishedFileId);
            }
            return list;
        }

        public bool IsSubscribed(ulong itemId){
            return Available && HasState(itemId, EItemState.k_EItemStateSubscribed);
        }

        public Task<bool> SubscribeAsync(ulong itemId){
            if (!Available) return Task.FromResult(false);

            var tcs = new TaskCompletionSource<bool>();
            CallResult<RemoteStorageSubscribePublishedFileResult_t> result = null;
            result = new CallResult<RemoteStorageSubscribePublishedFileResult_t>((r, ioFailure) => {
                _pendingCalls.Remove(result);
                if (!ioFailure && r.m_eResult == EResult.k_EResultOK) tcs.TrySetResult(true);
                else tcs.TrySetResult(false);
            });
            _pendingCalls.Add(result);

            result.Set(SteamUGC.SubscribeItem(new PublishedFileId_t(itemId)));
            return tcs.Task;
        }

        public Task<bool> UnsubscribeAsync(ulong itemId){
            if (!Available) return Task.FromResult(false);

            var tcs = new TaskCompletionSource<bool>();
            CallResult<RemoteStorageUnsubscribePublishedFileResult_t> result = null;
            result = new CallResult<RemoteStorageUnsubscribePublishedFileResult_t>((r, ioFailure) => {
                _pendingCalls.Remove(result);
                tcs.TrySetResult(!ioFailure && r.m_eResult == EResult.k_EResultOK);
            });
            _pendingCalls.Add(result);

            result.Set(SteamUGC.UnsubscribeItem(new PublishedFileId_t(itemId)));
            return tcs.Task;
        }

        public bool IsInstalled(ulong itemId){
            return Available && HasState(itemId, EItemState.k_EItemStateInstalled);
        }

        public bool TryGetInstallInfo(ulong itemId, out string folder, out ulong sizeOnDisk){
            folder = null;
            sizeOnDisk = 0;
            if (!Available) return false;

            return SteamUGC.GetItemInstallInfo(new PublishedFileId_t(itemId), out sizeOnDisk, out folder, FolderBufferSize, out _);
        }

        // ---------------- 扩展能力 ----------------

        public Task<bool> DownloadItemAsync(ulong itemId, bool highPriority = false, CancellationToken cancellationToken = default){
            if (!Available) return Task.FromResult(false);
            if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

            // 下载本身是异步的，这里只表示“请求已提交”，进度用 TryGetDownloadProgress 查
            return Task.FromResult(SteamUGC.DownloadItem(new PublishedFileId_t(itemId), highPriority));
        }

        public bool TryGetDownloadProgress(ulong itemId, out ulong downloadedBytes, out ulong totalBytes){
            downloadedBytes = 0;
            totalBytes = 0;
            if (!Available) return false;

            return SteamUGC.GetItemDownloadInfo(new PublishedFileId_t(itemId), out downloadedBytes, out totalBytes);
        }

        public Task<bool> DeleteItemAsync(ulong itemId, CancellationToken cancellationToken = default){
            if (!Available) return Task.FromResult(false);
            if (cancellationToken.IsCancellationRequested) return Task.FromResult(false);

            var tcs = new TaskCompletionSource<bool>();
            CallResult<DeleteItemResult_t> result = null;
            result = new CallResult<DeleteItemResult_t>((r, ioFailure) => {
                _pendingCalls.Remove(result);
                var ok = !ioFailure && r.m_eResult == EResult.k_EResultOK;
                if (!ok) _log?.Warn($"[Steam] 删除创意工坊条目失败: {(ioFailure ? "IO" : r.m_eResult.ToString())}");
                tcs.TrySetResult(ok);
            });
            _pendingCalls.Add(result);

            result.Set(SteamUGC.DeleteItem(new PublishedFileId_t(itemId)));
            return tcs.Task;
        }

        public bool TryGetUpdateProgress(out ulong uploadedBytes, out ulong totalBytes){
            uploadedBytes = 0;
            totalBytes = 0;
            if (!Available || !_submitting) return false;

            SteamUGC.GetItemUpdateProgress(_updateHandle, out uploadedBytes, out totalBytes);
            return true;
        }

        private static bool HasState(ulong itemId, EItemState state){
            return (SteamUGC.GetItemState(new PublishedFileId_t(itemId)) & (uint)state) != 0;
        }
    }
#endif
}
