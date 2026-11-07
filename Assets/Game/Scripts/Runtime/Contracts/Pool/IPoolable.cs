namespace Game.Runtime{
    /// <summary>
    /// 池化对象生命周期回调。实现该接口的对象在从池中取出/放回池中时，
    /// 由对象池实现自动调用，用于重置或清理状态（事件订阅、计时器、临时引用等）。
    /// </summary>
    public interface IPoolable{
        /// <summary>从池中取出时调用，应在此重置对象状态。</summary>
        void OnSpawn();

        /// <summary>放回池中时调用，应在此释放外部引用、取消订阅。</summary>
        void OnDespawn();
    }
}
