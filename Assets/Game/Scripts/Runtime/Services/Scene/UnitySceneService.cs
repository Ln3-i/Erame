using Game.Runtime;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Erame.Core;

namespace Game.Runtime.Services{
    public sealed class UnitySceneService : ISceneService, IService{
        private readonly ILogService _log;
        private readonly IAssetService _assetService;

        // additive 场景各自持有一个资源作用域，卸载场景时一起回收
        private readonly Dictionary<string, IAssetScope> _additiveScopes = new();

        public IAssetScope CurrentScope { get; private set; }

        public UnitySceneService(ILogService log, IAssetService assetService){
            _log = log;
            _assetService = assetService;
        }

        public void Initialize() => _log.Info("[Scene] 场景服务初始化");

        public void Dispose(){
            CurrentScope?.Dispose();
            CurrentScope = null;

            foreach (var scope in _additiveScopes.Values) scope.Dispose();
            _additiveScopes.Clear();

            _log.Info("[Scene] 场景服务销毁");
        }

        public async Task LoadSceneAsync(string sceneName, IProgress<float> progress = null){
            _log.Info($"[Scene] 开始切换场景: {sceneName}");

            // 1. 销毁旧场景的资源作用域（自动回收上一关的图片、模型等）
            if (CurrentScope != null){
                CurrentScope.Dispose();
                CurrentScope = null;
            }

            // 2. 异步加载新场景
            var asyncOp = SceneManager.LoadSceneAsync(sceneName);
            if (asyncOp == null){
                _log.Error($"[Scene] 场景不存在: {sceneName}");
                return;
            }

            await TrackAsync(asyncOp, progress);

            // 3. 创建新场景的资源作用域
            CurrentScope = _assetService.CreateScope($"Scene_{sceneName}");

            _log.Info($"[Scene] 场景 {sceneName} 加载完成，并创建新作用域");
        }

        public async Task LoadSceneAdditiveAsync(string sceneName, IProgress<float> progress = null, bool setActive = false){
            if (string.IsNullOrEmpty(sceneName)) throw new ArgumentException("sceneName 不能为空", nameof(sceneName));

            var loaded = SceneManager.GetSceneByName(sceneName);
            if (loaded.IsValid() && loaded.isLoaded){
                _log.Warn($"[Scene] 叠加场景已加载: {sceneName}");
                return;
            }

            _log.Info($"[Scene] 叠加加载场景: {sceneName}");

            var asyncOp = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            if (asyncOp == null){
                _log.Error($"[Scene] 场景不存在: {sceneName}");
                return;
            }

            await TrackAsync(asyncOp, progress);

            if (_additiveScopes.ContainsKey(sceneName)) return; // 并发重复加载兜底
            _additiveScopes[sceneName] = _assetService.CreateScope($"Additive_{sceneName}");

            if (setActive){
                var scene = SceneManager.GetSceneByName(sceneName);
                if (scene.IsValid() && scene.isLoaded) SceneManager.SetActiveScene(scene);
            }

            _log.Info($"[Scene] 叠加场景 {sceneName} 加载完成");
        }

        public async Task UnloadSceneAsync(string sceneName){
            if (string.IsNullOrEmpty(sceneName)) throw new ArgumentException("sceneName 不能为空", nameof(sceneName));

            if (_additiveScopes.TryGetValue(sceneName, out var scope)){
                scope.Dispose();
                _additiveScopes.Remove(sceneName);
            }

            var loaded = SceneManager.GetSceneByName(sceneName);
            if (!loaded.IsValid() || !loaded.isLoaded){
                _log.Warn($"[Scene] 场景未加载，忽略卸载: {sceneName}");
                return;
            }

            var asyncOp = SceneManager.UnloadSceneAsync(sceneName);
            if (asyncOp == null){
                _log.Error($"[Scene] 卸载场景失败: {sceneName}");
                return;
            }

            await TrackAsync(asyncOp, null);
            _log.Info($"[Scene] 场景已卸载: {sceneName}");
        }

        // Unity 的 progress 到 0.9 基本就算加载完了，这里做归一化并给调用方留出渲染帧
        private static async Task TrackAsync(AsyncOperation asyncOp, IProgress<float> progress){
            while (!asyncOp.isDone){
                progress?.Report(Mathf.Clamp01(asyncOp.progress / 0.9f));
                await Task.Yield();
            }
            progress?.Report(1f);
        }
    }
}
