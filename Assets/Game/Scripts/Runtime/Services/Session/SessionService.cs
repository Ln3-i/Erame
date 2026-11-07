using Game.Runtime;
using UnityEngine;
using Erame.Core;

namespace Game.Runtime.Services{
    // 会话服务占位实现：目前只输出日志，真正的单机开局 / 联机房间尚未实现
    public sealed class SessionService : ISessionService, IService{
        private readonly ILogService _log;
        private readonly IEventBus _eventBus;

        // 会话级服务：依赖通过容器注入
        public SessionService(ILogService log, IEventBus eventBus){
            _log = log;
            _eventBus = eventBus;
        }

        public void Initialize(){
            _log.Info("[Session] 会话服务初始化");
        }

        public void Dispose(){
            _log.Info("[Session] 会话服务销毁");
        }
    }
}