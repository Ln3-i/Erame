using Erame.Core;
using Erame.Runtime.Config;

namespace Game.Scripts{
    /// <summary>
    /// 业务门面：聚合服务访问，供玩法代码使用。
    /// 生命周期与服务注册已由 <see cref="GameEntry"/>（入口程序）承载，
    /// 本类只做静态转发。
    /// </summary>
    public static class GameApp{
        private static GameEntry _entry;

        public static ServiceContainer Container => _entry?.Container;
        public static bool IsStarted => Container != null;

        /// <summary>本次启动使用的框架配置；未显式传入时使用运行时默认配置。</summary>
        public static ErameSettings Settings => _entry?.Settings;

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
            if (_entry != null) return;

            var entry = new GameEntry();
            entry.Start(settings);   // 启动失败时 entry 内部已回滚，这里不持有半成品
            _entry = entry;
        }

        /// <summary>主循环，由 GameBootstrap 驱动。</summary>
        public static void Update() => _entry?.Update();

        public static void Quit(){
            if (_entry == null) return;

            _entry.Quit();
            _entry = null;
        }
    }
}
