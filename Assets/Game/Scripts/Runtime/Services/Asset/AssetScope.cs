using Game.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine; 
using Erame.Core;

namespace Game.Runtime.Services{
    public class AssetScope : IAssetScope{
        private readonly IAssetService _parentService;
        private readonly List<IDisposable> _trackedHandles = new();
        private readonly List<GameObject> _trackedGameObjects = new(); 
        private bool _isDisposed = false;

        public string ScopeName { get; }

        public AssetScope(IAssetService parentService, string scopeName){
            _parentService = parentService;
            ScopeName = scopeName;
        }

        public async Task<AssetHandle<T>> LoadAsync<T>(
            string key, 
            IProgress<float> progress = null, 
            CancellationToken cancellationToken = default
        ) where T : class{
            if (_isDisposed) throw new InvalidOperationException("作用域已被销毁，无法加载资源");
            var handle = await _parentService.LoadAsync<T>(key, progress, cancellationToken);
            if (handle == null) return null;
            _trackedHandles.Add(handle);
            return handle;
        }

        public void Track(IDisposable handle){
            if (!_isDisposed) _trackedHandles.Add(handle);
        }

        public void TrackGameObject(GameObject go){
            if (!_isDisposed && go != null) _trackedGameObjects.Add(go);
        }

        public void Dispose(){
            if (_isDisposed) return;
            _isDisposed = true;
            
            foreach (var go in _trackedGameObjects){
                if (go != null) UnityEngine.Object.Destroy(go);
            }
            _trackedGameObjects.Clear();

            for (int i = _trackedHandles.Count - 1; i >= 0; i--){
                _trackedHandles[i]?.Dispose();
            }
            _trackedHandles.Clear();
        }
    }
}