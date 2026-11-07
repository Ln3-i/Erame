using Game.Runtime;
using System;
using System.Collections.Generic;
using UnityEngine;
using Erame.Core;

namespace Game.Runtime.Services{
    public class UnityPoolService : IPoolService, IService{
        private readonly ILogService _log;
        // 用 string 作为池子名字
        private readonly Dictionary<string, Queue<object>> _pool = new();
        // 记录"已在池中"的实例，防止同一个对象被重复入池（会导致同一实例被多处同时使用）
        private readonly Dictionary<string, HashSet<object>> _pooled = new();

        public UnityPoolService(ILogService log) { _log = log; }

        public void Initialize() => _log.Info("[Pool] 对象池服务初始化完成");

        public void Dispose(){
            foreach (var key in new List<string>(_pool.Keys)){
                ClearPool(key);
            }
            _pool.Clear();
            _pooled.Clear();
        }

        public void Prewarm<T>(string poolKey, Func<T> factory, int count) where T : class{
            if (count <= 0) return;
            var queue = GetOrCreateQueue(poolKey);
            var set = GetOrCreateSet(poolKey);

            for (int i = 0; i < count; i++){
                var obj = factory();
                if (obj == null) continue;
                HandleSpawnLogic(obj, false); // 预热隐藏
                queue.Enqueue(obj);
                set.Add(obj);
            }
            _log.Info($"[Pool] 预热池子 {poolKey} x {count}");
        }

        public T Spawn<T>(string poolKey, Func<T> factory) where T : class{
            if (_pool.TryGetValue(poolKey, out var queue) && queue.Count > 0){
                var obj = queue.Dequeue() as T;
                GetOrCreateSet(poolKey).Remove(obj);
                HandleSpawnLogic(obj, true); // 激活
                return obj;
            }
            // 没存货，现场造
            return factory();
        }

        public void Despawn<T>(string poolKey, T instance) where T : class{
            if (instance == null) return;

            var set = GetOrCreateSet(poolKey);
            if (set.Contains(instance)){
                _log.Warn($"[Pool] 实例被重复回池，已忽略: {poolKey}");
                return;
            }

            HandleSpawnLogic(instance, false); // 隐藏

            GetOrCreateQueue(poolKey).Enqueue(instance);
            set.Add(instance);
        }

        public void ClearPool(string poolKey){
            if (!_pool.TryGetValue(poolKey, out var queue)) return;

            foreach (var obj in queue){
                DestroyInstance(obj);
            }
            queue.Clear();
            _pool.Remove(poolKey);
            _pooled.Remove(poolKey);
            _log.Info($"[Pool] 已销毁池子: {poolKey}");
        }

        private Queue<object> GetOrCreateQueue(string poolKey){
            if (!_pool.TryGetValue(poolKey, out var queue)){
                queue = new Queue<object>();
                _pool[poolKey] = queue;
            }
            return queue;
        }

        private HashSet<object> GetOrCreateSet(string poolKey){
            if (!_pooled.TryGetValue(poolKey, out var set)){
                set = new HashSet<object>();
                _pooled[poolKey] = set;
            }
            return set;
        }

        private static void DestroyInstance(object obj){
            if (obj is GameObject go){
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            else if (obj is Component component && component != null){
                UnityEngine.Object.Destroy(component.gameObject);
            }
            else if (obj is IDisposable disposable){
                disposable.Dispose();
            }
        }

        private void HandleSpawnLogic(object obj, bool isSpawned){
            if (obj is GameObject go) go.SetActive(isSpawned);
        }
    }
}
