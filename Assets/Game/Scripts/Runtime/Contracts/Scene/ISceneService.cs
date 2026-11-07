using System;
using System.Threading.Tasks;

namespace Game.Runtime{
    public interface ISceneService{
        // 切换场景（异步，单场景：会卸载当前场景）
        Task LoadSceneAsync(string sceneName, IProgress<float> progress = null);

        // 叠加载场景（异步，additive：保留当前场景，用于"常驻主场景 + 关卡/战斗场景"）
        Task LoadSceneAdditiveAsync(string sceneName, IProgress<float> progress = null, bool setActive = false);

        // 卸载叠加加载的场景
        Task UnloadSceneAsync(string sceneName);

        // 获取当前场景的资源作用域
        IAssetScope CurrentScope { get; }
    }
}
