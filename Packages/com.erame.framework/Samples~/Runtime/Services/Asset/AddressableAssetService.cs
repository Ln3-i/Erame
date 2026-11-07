using __NS__.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using Erame.Core;

namespace __NS__.Runtime.Services{
    public sealed class AddressableAssetService : IAssetService, IInstantiateService, IService{
        private sealed class CacheEntry{
            public string Key;
            public AsyncOperationHandle Handle;
            public int RefCount;
        }

        // 存活的实例：按 InstanceID 索引，避免 Unity 对象被销毁后字典键的比较歧义
        private sealed class LiveInstance{
            public string Key;
            public GameObject GameObject;
            public AsyncOperationHandle<GameObject> Handle;
        }

        private readonly ILogService _log;

        private readonly Dictionary<int, LiveInstance> _instances = new();

        // 同一 key 共享同一个底层句柄，并按引用计数释放
        private readonly Dictionary<string, CacheEntry> _cache = new();

        // 同一 key 的并发加载合并为一个任务。
        // 取消源归"所有等待者"共有，与单个调用方的令牌解耦：
        // 若任务绑定第一个调用方的 CancellationToken，A 取消会连带让 B 的加载一起失败。
        // Cts 不显式 Dispose——它只被轮询、不注册回调，无非托管资源，随 PendingLoad 一起被 GC 回收。
        private sealed class PendingLoad{
            public readonly CancellationTokenSource Cts = new CancellationTokenSource();
            public Task<CacheEntry> Task;
            public int Waiters;
        }

        private readonly Dictionary<string, PendingLoad> _pending = new();

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

            // 中止仍在进行的合并加载：LoadEntryAsync 感知到取消后会自行释放句柄
            foreach (var pending in _pending.Values){
                pending.Cts.Cancel();
            }
            _pending.Clear();

            foreach (var entry in _cache.Values){
                if (entry.Handle.IsValid()) Addressables.Release(entry.Handle);
            }
            _cache.Clear();

            foreach (var instance in _instances.Values){
                if (instance.Handle.IsValid()) Addressables.ReleaseInstance(instance.Handle);
            }
            _instances.Clear();

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

            PendingLoad pending;
            if (_pending.TryGetValue(key, out var existing)){
                // 已有同 key 的加载在进行中：直接等待并复用，避免重复加载。
                // 进度回调归属于发起加载的第一个调用方；后续等待者只共享结果
                pending = existing;
            }
            else{
                pending = new PendingLoad();
                pending.Task = LoadEntryAsync<T>(key, progress, pending.Cts.Token);
                _pending[key] = pending;
            }

            pending.Waiters++;
            try{
                var entry = await WaitWithCancellation(pending.Task, cancellationToken);
                if (entry != null) entry.RefCount++;
                return entry;
            }
            finally{
                pending.Waiters--;
                if (pending.Task.IsCompleted){
                    if (_pending.TryGetValue(key, out var current) && ReferenceEquals(current, pending)){
                        _pending.Remove(key);
                    }
                }
                else if (pending.Waiters <= 0){
                    // 最后一个等待者离开：从注册表摘除并取消共享加载，
                    // 让 LoadEntryAsync 释放句柄——否则会留下引用计数为 0 却无人释放的缓存条目
                    if (_pending.TryGetValue(key, out var current) && ReferenceEquals(current, pending)){
                        _pending.Remove(key);
                    }
                    pending.Cts.Cancel();
                }
            }
        }

        /// <summary>
        /// "等待任务完成，但调用方自己的令牌可以随时取消"。
        /// 取消只让当前等待者离开，共享任务继续为其他等待者服务。
        /// （.NET Standard 2.1 没有 Task.WaitAsync，这里用 WhenAny + 令牌注册实现。）
        /// </summary>
        private static async Task<T> WaitWithCancellation<T>(Task<T> task, CancellationToken cancellationToken){
            if (!cancellationToken.CanBeCanceled) return await task;
            cancellationToken.ThrowIfCancellationRequested();

            var cancelled = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
            using (cancellationToken.Register(() => cancelled.TrySetResult(true))){
                var completed = await Task.WhenAny(task, cancelled.Task);
                if (completed != task) cancellationToken.ThrowIfCancellationRequested();
                return await task;
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

        public int LiveInstanceCount => _instances.Count;

        public async Task<InstantiatedAsset> InstantiateAsync(
            string key,
            Transform parent = null,
            bool worldPositionStays = false,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default
        ){
            if (_disposed) throw new InvalidOperationException("资源服务已销毁");
            if (string.IsNullOrEmpty(key)) throw new ArgumentException("key 不能为空", nameof(key));

            var handle = Addressables.InstantiateAsync(key, parent, worldPositionStays);

            if (cancellationToken.IsCancellationRequested){
                ReleaseInstantiateHandle(handle);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (progress != null){
                while (!handle.IsDone){
                    if (cancellationToken.IsCancellationRequested){
                        _log.Warn($"[Asset] 实例化被取消: {key}");
                        ReleaseInstantiateHandle(handle);
                        cancellationToken.ThrowIfCancellationRequested();
                    }
                    progress.Report(Mathf.Clamp01(handle.PercentComplete));
                    await Task.Yield();
                }
            }
            else if (handle.IsValid()){
                await handle.Task;
            }

            if (cancellationToken.IsCancellationRequested){
                ReleaseInstantiateHandle(handle);
                cancellationToken.ThrowIfCancellationRequested();
            }

            if (handle.Status != AsyncOperationStatus.Succeeded || handle.Result == null){
                _log.Error($"[Asset] 实例化失败: {key}");
                ReleaseInstantiateHandle(handle);
                return null;
            }

            var go = handle.Result;
            var instanceId = go.GetInstanceID();
            var live = new LiveInstance { Key = key, GameObject = go, Handle = handle };
            _instances[instanceId] = live;
            _log.Info($"[Asset] 实例化: {key}");

            return new InstantiatedAsset(key, go, instanceId, id => ReleaseInstanceById(id));
        }

        public bool Release(GameObject instance){
            if (instance == null) return false;
            return ReleaseInstanceById(instance.GetInstanceID());
        }

        private bool ReleaseInstanceById(int instanceId){
            if (!_instances.TryGetValue(instanceId, out var live)) return false;

            _instances.Remove(instanceId);
            if (live.Handle.IsValid()) Addressables.ReleaseInstance(live.Handle);
            _log.Info($"[Asset] 回收实例: {live.Key}");
            return true;
        }

        private static void ReleaseInstantiateHandle(AsyncOperationHandle<GameObject> handle){
            if (handle.IsValid()) Addressables.ReleaseInstance(handle);
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
