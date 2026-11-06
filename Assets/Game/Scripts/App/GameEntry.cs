using Erame.Core;

namespace Game.Scripts.App{
    /// <summary>
    /// 标准入口：整个项目只需重写这里即可启动。
    /// Configure 负责业务注册（框架默认服务已由 ErameRuntime 注册），
    /// OnStart / OnUpdate / OnQuit 负责生命周期收尾。
    /// </summary>
    public sealed class GameEntry : ErameEntry{
        public override void Configure(ServiceContainer container){
            // 业务自定义服务在此注册：
            // container.RegisterSingleton<IMyService>(p => new MyService(...));
        }

        public override void OnStart(){
            // 启动完成后的业务初始化：
        }
    }
}
