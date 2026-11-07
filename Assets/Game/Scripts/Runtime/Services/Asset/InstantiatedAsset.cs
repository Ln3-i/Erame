using Game.Runtime;
using System;
using UnityEngine;

namespace Game.Runtime.Services{
    /// <summary>
    /// 一个已实例化的 Addressables 对象。Dispose 即回收（销毁场景对象并释放句柄），
    /// 重复 Dispose 无副作用。界面/战斗代码应当 <c>using</c> 它，或把它交给对象池复用。
    /// </summary>
    public sealed class InstantiatedAsset : IDisposable{
        private Action<int> _release;
        private int _instanceId;
        private bool _disposed;

        internal InstantiatedAsset(string key, GameObject gameObject, int instanceId, Action<int> release){
            Key = key;
            GameObject = gameObject;
            _instanceId = instanceId;
            _release = release;
        }

        /// <summary>创建它时使用的 Addressables key。</summary>
        public string Key { get; }

        /// <summary>实例本体；回收后为 null。</summary>
        public GameObject GameObject { get; private set; }

        public bool IsDisposed => _disposed;

        public T GetComponent<T>() where T : Component{
            return GameObject == null ? null : GameObject.GetComponent<T>();
        }

        public void Dispose(){
            if (_disposed) return;
            _disposed = true;

            var release = _release;
            var id = _instanceId;
            _release = null;
            GameObject = null;

            try{
                release?.Invoke(id);
            }
            catch (Exception e){
                Debug.LogError($"[Asset] 回收实例失败({Key}): {e}");
            }
        }
    }
}
