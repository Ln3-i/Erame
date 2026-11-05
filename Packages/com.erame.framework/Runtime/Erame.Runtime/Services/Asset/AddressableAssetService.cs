using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Erame.Core;

namespace Erame.Runtime.Services{
    public sealed class AddressableAssetService : IAssetService, IService{
        private sealed class CacheEntry{
            public string Key;
            public AsyncOperationHandle Handle;
            public int RefCount;
        }

        private readonly ILogService _log;

        // 同一 key 共享同一个底层句柄，并按引用计数释放
        private readonly Dictionary<string, CacheEntry> _cache = new();
        // 同一 key 的并发加载合并为一个任务
        private readonly Dictionary<string, Task<CacheEntry>> _pending = new();

        private bool _disposed;

        public AddressableAssetService(ILogService log){ _log = log; }

        public void Initialize(){
            _log.Info("[Asset] 资源服务初始化");
            // 提前触发 Addressables 初始化，避免首次加载时才付出初始化开销
            Addressables.InitializeAsync();
        }

        public void Dispose(){
            if (_disposed) return;
            _disposed = true;

            foreach (var entry in _cache.Values){
                if (entry.Handle.IsValid()) Addressables.Release(entry.Handle);
            }
            _cache.Clear();
            _pending.Clear();
            _log.Info("[Asset] 资源服务销毁");
        }

        public IAssetScope CreateScope(string scopeName){
            _log.Info($"[Asset] 创建资产作用域: {scopeName}");
            return new AssetScope(this, scopeName);
        }

        public async Task<AssetHandle<T>> LoadAsync<T>(
            string key,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default
        ) where T : class{
            if (_disposed) throw new InvalidOperationException("资源服务已销毁");

            var entry = await AcquireEntryAsync<T>(key, progress, cancellationToken);
            if (entry == null) return null; // 加载失败：返回 null，由调用方处理
            return new AssetHandle<T>((T)entry.Handle.Result, () => ReleaseEntry(key));
        }

        private async Task<CacheEntry> AcquireEntryAsync<T>(string key, IProgress<float> progress, CancellationToken cancellationToken) where T : class{
            if (_cache.TryGetValue(key, out var cached) && cached.Handle.IsValid()){
                cached.RefCount++;
                return cached;
            }

            // 已有同 key 的加载在进行中：直接等待并复用，避免重复加载
            if (_pending.TryGetValue(key, out var pending)){
                var shared = await pending;
                if (shared != null) shared.RefCount++;
                return shared;
            }

            var task = LoadEntryAsync<T>(key, progress, cancellationToken);
            _pending[key] = task;
            try{
                var entry = await task;
                if (entry != null) entry.RefCount++;
                return entry;
            }
            finally{
                _pending.Remove(key);
            }
        }

        private async Task<CacheEntry> LoadEntryAsync<T>(string key, IProgress<float> progress, CancellationToken cancellationToken) where T : class{
            _log.Info($"[Asset] 开始加载资源: {key}");

            var handle = Addressables.LoadAssetAsync<T>(key);

            if (cancellationToken.IsCancellationRequested){
                if (handle.IsValid()) Addressables.Release(handle);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (progress != null){
                // 只有调用方真的需要进度时才做逐帧轮询；否则直接等待完成回调，零轮询开销
                while (!handle.IsDone){
                    if (cancellationToken.IsCancellationRequested){
                        _log.Warn($"[Asset] 加载被取消: {key}");
                        if (handle.IsValid()) Addressables.Release(handle);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    progress.Report(handle.PercentComplete);
                    await Task.Yield();
                }
            }
            else{
                await handle.Task;
            }

            if (cancellationToken.IsCancellationRequested){
                if (handle.IsValid()) Addressables.Release(handle);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (handle.Status == AsyncOperationStatus.Succeeded && !_disposed){
                var entry = new CacheEntry { Key = key, Handle = handle, RefCount = 0 };
                _cache[key] = entry;
                return entry;
            }

            _log.Error($"[Asset] 资源加载失败: {key}");
            if (handle.IsValid()) Addressables.Release(handle);
            return null;
        }

        private void ReleaseEntry(string key){
            if (!_cache.TryGetValue(key, out var entry)) return;

            entry.RefCount--;
            if (entry.RefCount > 0) return;

            _cache.Remove(key);
            _log.Info($"[Asset] 释放资源: {key}");
            if (entry.Handle.IsValid()) Addressables.Release(entry.Handle);
        }
    }
}
