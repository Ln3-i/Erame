using System;

namespace Erame.Core{
    /// <summary>
    /// 对象池服务。
    /// 约定：实现层在 <see cref="Spawn{T}"/> / <see cref="Despawn{T}"/> 时，
    /// 如果对象实现了 <see cref="IPoolable"/>，应自动调用 <see cref="IPoolable.OnSpawn"/> / <see cref="IPoolable.OnDespawn"/>，
    /// 调用方无需手动重置对象状态。
    /// </summary>
    public interface IPoolService{
        /// <summary>预热对象池：预先创建 count 个实例。</summary>
        void Prewarm<T>(string poolKey, Func<T> factory, int count) where T : class;

        /// <summary>从池中取出对象；实现层应对 <see cref="IPoolable"/> 自动调用 OnSpawn。</summary>
        T Spawn<T>(string poolKey, Func<T> factory) where T : class;

        /// <summary>把对象放回池中；实现层应对 <see cref="IPoolable"/> 自动调用 OnDespawn。</summary>
        void Despawn<T>(string poolKey, T instance) where T : class;

        void ClearPool(string poolKey);
    }
}
