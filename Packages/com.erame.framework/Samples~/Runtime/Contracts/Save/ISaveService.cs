using System.Threading;
using System.Threading.Tasks;

namespace __NS__.Runtime{
    public interface ISaveService{
        void Register(ISaveable saveable);
        void Unregister(ISaveable saveable);

        /// <summary>
        /// 异步保存。取消时不会破坏主存档（写入流程需保证原子性）。
        /// </summary>
        Task<bool> SaveToFileAsync(string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步读档。取消时不会破坏主存档。
        /// </summary>
        Task<bool> LoadFromFileAsync(string filePath, CancellationToken cancellationToken = default);
    }
}
