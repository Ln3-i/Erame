namespace Erame.Core{
    /// <summary>
    /// 每帧更新协议：服务实现它即可被 ErameRuntime 每帧统一驱动。
    /// 框架只认识这个底座协议，不认识 Tick / 平台等具体服务类型。
    /// </summary>
    public interface IFrameUpdate{
        void Update(float deltaTime);
    }
}
