namespace Game.Runtime{
    // 局内/房间级服务接口：接口属于 Core，实现属于 Runtime，避免业务层被迫依赖实现程序集
    // 注意：开局/联机房间等真实能力尚未设计，实现类目前只是占位（只输出初始化日志）
    public interface ISessionService{
    }
}
