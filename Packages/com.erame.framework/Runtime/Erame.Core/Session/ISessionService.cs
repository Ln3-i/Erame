namespace Erame.Core{
    // 局内/房间级服务接口：接口属于 Core，实现属于 Runtime，避免业务层被迫依赖实现程序集
    public interface ISessionService{
        void DoSomethingInSession();
    }
}
