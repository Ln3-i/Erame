using Erame.Core;
using Erame.Runtime.Bootstrap;
using Erame.Runtime.Config;
using Game.Scripts.Gameplay.Flow;
using Game.Scripts.Modules;

namespace Game.Scripts{
    /// <summary>
    /// 标准入口：模块与服务在这里注册，生命周期由 <see cref="ErameEntry"/> 承载。
    /// 开发者只需重写 Configure / OnStarted 即可启动整个项目。
    /// </summary>
    public sealed class GameEntry : ErameEntry{
        protected override void Configure(ServiceContainer container, ErameSettings settings){
            // 模块即服务分组；注册顺序决定实例创建顺序，容器内部会自动完成依赖排序
            IModule[] modules = {
                new FrameworkModule(settings),
                new GameplayModule(settings),
                new PlatformModule(settings)
            };
            foreach (var module in modules){
                module.Configure(container);
            }

            // 业务自定义服务可在此追加注册：
            // container.RegisterSingleton<IMyService>(p => new MyService(...));
        }

        protected override void OnStarted(){
            // 注册游戏流程
            FlowRegistry.RegisterAll(Get<IGameStateMachine>());
        }
    }
}
