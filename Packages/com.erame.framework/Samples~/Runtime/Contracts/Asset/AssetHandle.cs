using System;
using System.Threading;

namespace __NS__.Runtime{
    public sealed class AssetHandle<T> : IDisposable where T : class{
        public T Asset { get; }

        // 使用 Interlocked.Exchange 取走回调，保证并发 Dispose 时只释放一次
        private Action _releaseAction;

        public AssetHandle(T asset, Action releaseAction){
            Asset = asset ?? throw new ArgumentNullException(nameof(asset));
            _releaseAction = releaseAction;
        }

        public void Dispose(){
            var release = Interlocked.Exchange(ref _releaseAction, null);
            release?.Invoke();
        }
    }
}
