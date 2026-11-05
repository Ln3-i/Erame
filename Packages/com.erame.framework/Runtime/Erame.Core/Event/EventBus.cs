using System;
using System.Collections.Generic;

namespace Erame.Core{
    public sealed class EventBus : IEventBus, IService{
        // 事件类型 -> 订阅者数组。每次订阅关系变化时整体替换引用（写时复制），
        // Publish 只读取快照，因此后台线程可以安全发布事件。
        private volatile Dictionary<Type, Delegate[]> _snapshot = new Dictionary<Type, Delegate[]>();

        private readonly object _writeLock = new object();
        private readonly ILogService _log;

        public EventBus(ILogService log){
            _log = log;
        }

        public void Initialize(){}

        public void Dispose(){
            lock (_writeLock){
                _snapshot = new Dictionary<Type, Delegate[]>();
            }
        }

        // 订阅事件
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct{
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(T);

            lock (_writeLock){
                var next = CopySnapshot(_snapshot);
                var existing = next.TryGetValue(type, out var list) ? list : Array.Empty<Delegate>();

                var combined = new Delegate[existing.Length + 1];
                Array.Copy(existing, combined, existing.Length);
                combined[existing.Length] = handler;
                next[type] = combined;

                _snapshot = next;
            }

            // 返回凭证：调用 Dispose 时，就自动执行解绑
            return new EventSubscription(() => Unsubscribe(handler));
        }

        // 取消订阅事件
        public void Unsubscribe<T>(Action<T> handler) where T : struct{
            if (handler == null) return;
            var type = typeof(T);

            lock (_writeLock){
                var current = _snapshot;
                if (!current.TryGetValue(type, out var existing)) return;

                int index = Array.IndexOf(existing, handler);
                if (index < 0) return;

                var next = CopySnapshot(current);
                if (existing.Length == 1){
                    next.Remove(type); // 没有剩余订阅者时移除该事件的键
                }
                else{
                    var remaining = new Delegate[existing.Length - 1];
                    Array.Copy(existing, 0, remaining, 0, index);
                    Array.Copy(existing, index + 1, remaining, index, existing.Length - index - 1);
                    next[type] = remaining;
                }

                _snapshot = next;
            }
        }

        /// <summary>只读诊断：返回各事件类型的订阅者数量（按事件类型名排序）。</summary>
        public IReadOnlyList<KeyValuePair<Type, int>> GetSubscriptionCounts(){
            var snapshot = _snapshot;
            var counts = new List<KeyValuePair<Type, int>>(snapshot.Count);
            foreach (var kv in snapshot){
                counts.Add(new KeyValuePair<Type, int>(kv.Key, kv.Value.Length));
            }
            counts.Sort((a, b) => string.CompareOrdinal(a.Key.Name, b.Key.Name));
            return counts;
        }

        // 发布事件
        public void Publish<T>(in T evt) where T : struct{
            // 无锁读取快照：即使在订阅/取消订阅期间也能安全遍历
            var snapshot = _snapshot;
            if (!snapshot.TryGetValue(typeof(T), out var handlers)) return;

            // 单个订阅者抛异常不应该影响其他订阅者
            foreach (var handler in handlers){
                try{ ((Action<T>)handler).Invoke(evt); }
                catch (Exception ex){ _log?.Error($"[EventBus] 事件 {typeof(T).Name} 的订阅者抛出异常: {ex}"); }
            }
        }

        private static Dictionary<Type, Delegate[]> CopySnapshot(Dictionary<Type, Delegate[]> source){
            var copy = new Dictionary<Type, Delegate[]>(source.Count);
            foreach (var kv in source){
                copy[kv.Key] = kv.Value;
            }
            return copy;
        }
    }
}
