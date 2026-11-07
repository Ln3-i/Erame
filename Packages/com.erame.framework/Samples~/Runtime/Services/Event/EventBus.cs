using __NS__.Runtime;
using System;
using Erame.Core;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace __NS__.Runtime.Services{
    public sealed class EventBus : IEventBus, IEventBusDiagnostics, IService{
        // 类型 -> 该事件的委托数组。数组本身不可变，替换时整段换掉，
        // 因此 Publish 可以无锁读取（读到的是某个完整快照）。
        private readonly ConcurrentDictionary<Type, Delegate[]> _handlers = new();

        private readonly ILogService _log;

        public EventBus(ILogService log){
            _log = log;
        }

        public void Initialize(){}

        public void Dispose(){
            _handlers.Clear();
        }

        // 订阅事件
        public IDisposable Subscribe<T>(Action<T> handler) where T : struct{
            if (handler == null) throw new ArgumentNullException(nameof(handler));
            var type = typeof(T);

            // 只重建"这一个事件类型"的数组，不再复制整张类型表。
            // 旧实现每次订阅都浅拷贝整个字典，类型越多、订阅越频繁，扩容成本越高。
            _handlers.AddOrUpdate(
                type,
                _ => new Delegate[] { handler },
                (_, existing) => Append(existing, handler));

            // 返回凭证：调用 Dispose 时，就自动执行解绑
            return new EventSubscription(() => Unsubscribe(handler));
        }

        // 取消订阅事件
        public void Unsubscribe<T>(Action<T> handler) where T : struct{
            if (handler == null) return;
            var type = typeof(T);

            // 与并发订阅竞争时重试：只有"目标数组没被别人换掉"才提交删除，
            // 避免把别人刚订阅的处理器一起覆盖掉
            while (_handlers.TryGetValue(type, out var existing)){
                int index = Array.IndexOf(existing, handler);
                if (index < 0) return;

                if (existing.Length == 1){
                    if (RemoveIfUnchanged(type, existing)) return;
                    continue;
                }

                var remaining = new Delegate[existing.Length - 1];
                Array.Copy(existing, 0, remaining, 0, index);
                Array.Copy(existing, index + 1, remaining, index, existing.Length - index - 1);

                if (_handlers.TryUpdate(type, remaining, existing)) return;
            }
        }

        /// <summary>只读诊断：返回各事件类型的订阅者数量（按事件类型名排序）。</summary>
        public IReadOnlyList<KeyValuePair<Type, int>> GetSubscriptionCounts(){
            var counts = new List<KeyValuePair<Type, int>>(_handlers.Count);
            foreach (var kv in _handlers){
                counts.Add(new KeyValuePair<Type, int>(kv.Key, kv.Value.Length));
            }
            counts.Sort((a, b) => string.CompareOrdinal(a.Key.Name, b.Key.Name));
            return counts;
        }

        /// <summary>只读诊断：某事件类型当前是否有订阅者（避免为“没人听”的事件构造负载）。</summary>
        public bool HasSubscribers<T>() where T : struct{
            return _handlers.TryGetValue(typeof(T), out var handlers) && handlers.Length > 0;
        }

        // 发布事件
        public void Publish<T>(in T evt) where T : struct{
            if (!_handlers.TryGetValue(typeof(T), out var handlers) || handlers.Length == 0) return;

            // 订阅/退订只会替换数组，不会修改正在遍历的这份，所以这里不需要拷贝
            for (int i = 0; i < handlers.Length; i++){
                try{ ((Action<T>)handlers[i]).Invoke(evt); }
                catch (Exception ex){ _log?.Error($"[EventBus] 事件 {typeof(T).Name} 的订阅者抛出异常: {ex}"); }
            }
        }

        private static Delegate[] Append(Delegate[] existing, Delegate handler){
            var combined = new Delegate[existing.Length + 1];
            Array.Copy(existing, combined, existing.Length);
            combined[existing.Length] = handler;
            return combined;
        }

        private bool RemoveIfUnchanged(Type type, Delegate[] expected){
            // 以"键值对整体匹配"的方式删除：只删自己比较过的那一份快照
            return ((ICollection<KeyValuePair<Type, Delegate[]>>)_handlers)
                .Remove(new KeyValuePair<Type, Delegate[]>(type, expected));
        }
    }
}
