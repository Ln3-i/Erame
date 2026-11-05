using System;
using System.Threading.Tasks;

namespace Erame.Core{
    public interface ISceneService{
        // 切换场景（异步）
        Task LoadSceneAsync(string sceneName, IProgress<float> progress = null);
        
        // 获取当前场景的资源作用域
        IAssetScope CurrentScope { get; }
    }
}