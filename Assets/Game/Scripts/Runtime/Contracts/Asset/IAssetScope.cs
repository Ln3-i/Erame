using System;
using System.Threading;
using System.Threading.Tasks;

namespace Game.Runtime{
    public interface IAssetScope : IDisposable{
        string ScopeName { get; }
        
        Task<AssetHandle<T>> LoadAsync<T>(
            string key, 
            IProgress<float> progress = null, 
            CancellationToken cancellationToken = default
        ) where T : class;
        
        void Track(IDisposable handle);
    }
}