using System;
using System.Threading.Tasks;

namespace Erame.Core{
    public interface IService : IDisposable, IAsyncDisposable{
        void Initialize();
        new void Dispose();

        /// <summary>
        /// 异步清理。默认实现直接委派给同步的 <see cref="Dispose"/>；
        /// 需要真正的异步清理（如释放 Addressables 句柄）的服务可以覆盖此方法。
        /// </summary>
        ValueTask IAsyncDisposable.DisposeAsync(){
            Dispose();
            return default;
        }
    }
}
