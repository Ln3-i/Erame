using __NS__.Runtime;
using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace __NS__.Runtime.Services{
    /// <summary>
    /// 资源实例化：按 Addressables key 直接实例化预制体，并按实例回收。
    /// <para>
    /// 与 <see cref="Erame.Core.IAssetService"/> 的分工：加载资源得到的是"资产句柄"（共享、引用计数），
    /// 实例化得到的是"场景里的一个对象"（每次都是独立实例）。界面列表项、单位、建筑、
    /// 掉落物这类高频对象都应该走这里，配合 <c>IPoolService</c> 降低 Instantiate/Destroy 开销。
    /// </para>
    /// </summary>
    public interface IInstantiateService{
        /// <summary>当前仍存活的实例数量（诊断用）。</summary>
        int LiveInstanceCount { get; }

        /// <summary>
        /// 实例化一个 Addressables 预制体。失败返回 null（不抛异常，由调用方决定回退逻辑），
        /// 取消则抛 <see cref="OperationCanceledException"/> 且不泄漏句柄。
        /// </summary>
        Task<InstantiatedAsset> InstantiateAsync(
            string key,
            Transform parent = null,
            bool worldPositionStays = false,
            IProgress<float> progress = null,
            CancellationToken cancellationToken = default);

        /// <summary>回收实例（等价于 <c>InstantiatedAsset.Dispose()</c>）；实例不属于本服务时返回 false。</summary>
        bool Release(GameObject instance);
    }
}
