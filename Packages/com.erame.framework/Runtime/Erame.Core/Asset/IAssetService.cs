using System;
using System.Threading;
using System.Threading.Tasks;

namespace Erame.Core{
    public interface IAssetService{
        // 增加进度和取消
        Task<AssetHandle<T>> LoadAsync<T>(
            string key, 
            IProgress<float> progress = null, 
            CancellationToken cancellationToken = default
        ) where T : class;

        // 新增：创建一个资产管理作用域
        IAssetScope CreateScope(string scopeName);
    }
}