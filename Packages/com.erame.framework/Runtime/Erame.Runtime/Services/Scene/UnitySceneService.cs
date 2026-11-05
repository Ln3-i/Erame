using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.SceneManagement;
using Erame.Core;

namespace Erame.Runtime.Services{
    public sealed class UnitySceneService : ISceneService, IService{
        private readonly ILogService _log;
        private readonly IAssetService _assetService;
        
        public IAssetScope CurrentScope { get; private set; }

        public UnitySceneService(ILogService log, IAssetService assetService){
            _log = log;
            _assetService = assetService;
        }

        public void Initialize() => _log.Info("[Scene] 场景服务初始化");
        public void Dispose(){
            CurrentScope?.Dispose();
            CurrentScope = null;
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

            // 3. 轮询进度
            while (!asyncOp.isDone){
                progress?.Report(asyncOp.progress / 0.9f); // Unity 进度到 0.9 就算加载完了，这里做个归一化
                await Task.Yield();
            }

            // 4. 创建新场景的资源作用域
            CurrentScope = _assetService.CreateScope($"Scene_{sceneName}");
            
            _log.Info($"[Scene] 场景 {sceneName} 加载完成，并创建新作用域");
        }
    }
}