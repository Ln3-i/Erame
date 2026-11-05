using UnityEngine;
using Erame.Core;
using Erame.Runtime.Config;
using Erame.Runtime.Services;
using Game.Scripts.Gameplay.Flow;
using Game.Scripts.Modules;

namespace Game.Scripts{
    public static class GameApp{
        public static ServiceContainer Container { get; private set; }
        public static bool IsStarted => Container != null;

        /// <summary>本次启动使用的框架配置；未显式传入时使用运行时默认配置。</summary>
        public static ErameSettings Settings { get; private set; }

        // ================= 框架工具 =================
        public static ILogService Log => Container.Get<ILogService>();
        public static ITimeService Time => Container.Get<ITimeService>();
        public static IPoolService Pool => Container.Get<IPoolService>();
        public static IInputService Input => Container.Get<IInputService>();
        public static IEventBus EventBus => Container.Get<IEventBus>();
        public static IAssetService Asset => Container.Get<IAssetService>();
        public static ISceneService Scene => Container.Get<ISceneService>();
        public static IUIService UI => Container.Get<IUIService>();
        public static ILocalizationService Localization => Container.Get<ILocalizationService>();
        public static ITickService Tick => Container.Get<ITickService>();
        public static ICommandService Command => Container.Get<ICommandService>();
        public static ISaveService Save => Container.Get<ISaveService>();
        public static IDataRegistry Data => Container.Get<IDataRegistry>();
        public static ISessionService Session => Container.Get<ISessionService>();
        public static IPlatformService Platform => Container.Get<IPlatformService>();
        public static IGameStateMachine Flow => Container.Get<IGameStateMachine>();
        public static IAudioService Audio => Container.Get<IAudioService>();
        
        public static void Start(ErameSettings settings = null){
            // 初始化
            if (Container != null) return; 
            Settings = settings != null ? settings : ErameSettings.Default;
            Container = new ServiceContainer();
            Container.ErrorReporter = message => UnityEngine.Debug.LogError(message);

            // 通过模块注册服务：注册顺序决定实例创建顺序，容器内部会自动完成依赖排序
            IModule[] modules = { new FrameworkModule(Settings), new GameplayModule(Settings), new PlatformModule(Settings) };
            foreach (var module in modules){
                module.Configure(Container);
            }

            // 构建并初始化服务
            Container.Build();
            Container.InitializeAll();

            // 注册游戏流程
            var flow = Container.Get<IGameStateMachine>();
            FlowRegistry.RegisterAll(flow);
        }

        public static void Quit(){
            if (Container == null) return;
            
            Container.Dispose();
            Container = null;
        }
    }
}