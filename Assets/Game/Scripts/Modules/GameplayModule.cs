using Erame.Core;
using Erame.Runtime.Config;
using Erame.Runtime.Services;

namespace Game.Scripts.Modules{
    /// <summary>游戏运行时服务：输入、UI、池、音频、本地化、流程、Tick、命令、存档、数据、会话、平台。</summary>
    public sealed class GameplayModule : IModule{
        public void Configure(IServiceRegistrar registrar){
            var settings = ErameSettings.Default;

            registrar.RegisterSingleton<IInputService>(p => new InputService(p.Get<ILogService>()));
            registrar.RegisterSingleton<IPoolService>(p => new UnityPoolService(p.Get<ILogService>()));
            registrar.RegisterSingleton<IUIService>(p => new UIService(p.Get<ILogService>(), p.Get<IInputService>()));
            registrar.RegisterSingleton<IAudioService>(p => new UnityAudioService(
                p.Get<ILogService>(),
                p.Get<IPoolService>(),
                p.Get<IAssetService>(),
                p.Get<ITimeService>()
            ));
            registrar.RegisterSingleton<ILocalizationService>(p => new LocalizationService(p.Get<ILogService>(), p.Get<IEventBus>()));
            registrar.RegisterSingleton<IGameStateMachine>(p => new GameStateMachine(p.Get<ILogService>()));
            registrar.RegisterSingleton<ITickService>(p => new TickService(p.Get<ILogService>()));
            registrar.RegisterSingleton<ICommandService>(p => new CommandService(p.Get<ILogService>()));
            registrar.RegisterSingleton<ISaveCrypto>(_ => new AesGcmSaveCrypto(settings.GetSaveKey()));
            registrar.RegisterSingleton<ISaveService>(p => new FileSaveService(
                p.Get<ILogService>(),
                p.Get<IEventBus>(),
                p.Get<ISaveCrypto>()
            ));
            registrar.RegisterSingleton<IDataRegistry>(p => new DataRegistryService(p.Get<ILogService>()));
            registrar.RegisterSingleton<ISessionService>(p => new SessionService(p.Get<ILogService>(), p.Get<IEventBus>()));
            registrar.RegisterSingleton<IPlatformService>(p => new NullPlatformService(p.Get<ILogService>()));
        }
    }
}
