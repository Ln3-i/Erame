using UnityEngine;
using Erame.Core;

namespace Erame.Runtime.Services{
    // 模拟一个“局内/房间内”的服务
    public sealed class SessionService : ISessionService, IService{
        private readonly ILogService _log;
        private readonly IEventBus _eventBus;

        // 会话级服务：依赖通过容器注入
        public SessionService(ILogService log, IEventBus eventBus){
            _log = log;
            _eventBus = eventBus;
        }

        public void Initialize(){
            _log.Info("[Session] 会话服务初始化：进入房间/开始新游戏");
        }

        public void Dispose(){
            _log.Info("[Session] 会话服务销毁：退出房间/结束游戏");
        }

        public void DoSomethingInSession(){
            _log.Info("[Session] 当前正在房间内执行逻辑...");
        }
    }
}