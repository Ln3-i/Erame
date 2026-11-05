using System;
using System.Reflection;
using Erame.Core;
using Erame.Runtime.Config;
using Erame.Runtime.Services;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 框架服务装配入口。游戏层只需要传入 <see cref="ErameSettings"/> 和可选的输入源工厂，
    /// 不必逐条复制注册代码。
    /// </summary>
    public static class ErameInstaller{
        /// <summary>基础能力：日志、事件、时间、资源、场景。</summary>
        public static void InstallFramework(IServiceRegistrar registrar, ErameSettings settings){
            if (registrar == null) throw new ArgumentNullException(nameof(registrar));
            var config = settings != null ? settings : ErameSettings.Default;

            registrar.RegisterSingleton<ILogService>(_ => new UnityLogService { MinLevel = config.LogLevel });
            registrar.RegisterSingleton<IEventBus>(p => new EventBus(p.Get<ILogService>()));
            registrar.RegisterSingleton<ITimeService>(p => new UnityTimeService(p.Get<ILogService>()));
            registrar.RegisterSingleton<IAssetService>(p => new AddressableAssetService(p.Get<ILogService>()));
            registrar.RegisterSingleton<ISceneService>(p => new UnitySceneService(p.Get<ILogService>(), p.Get<IAssetService>()));
        }

        /// <summary>
        /// 运行时服务：输入、UI、池、音频、本地化、流程、Tick、命令、存档、数据、会话。
        /// 输入源由调用方提供，框架本身不绑定任何具体输入配置。
        /// </summary>
        public static void InstallGameplay(
            IServiceRegistrar registrar,
            ErameSettings settings,
            Func<Erame.Core.IServiceProvider, IInputSource> inputSourceFactory){
            if (registrar == null) throw new ArgumentNullException(nameof(registrar));
            var config = settings != null ? settings : ErameSettings.Default;

            registrar.RegisterSingleton<IInputService>(p => {
                var service = new InputService(p.Get<ILogService>());
                var source = inputSourceFactory?.Invoke(p);
                if (source != null) service.SetSource(source);
                return service;
            });
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
            registrar.RegisterSingleton<ISaveCrypto>(_ => new AesGcmSaveCrypto(config.GetSaveKey()));
            registrar.RegisterSingleton<ISaveService>(p => new FileSaveService(
                p.Get<ILogService>(),
                p.Get<IEventBus>(),
                p.Get<ISaveCrypto>()
            ));
            registrar.RegisterSingleton<IDataRegistry>(p => new DataRegistryService(p.Get<ILogService>()));
            registrar.RegisterSingleton<ISessionService>(p => new SessionService(p.Get<ILogService>(), p.Get<IEventBus>()));
        }

        /// <summary>平台能力：Steam / 原生空实现。</summary>
        public static void InstallPlatform(IServiceRegistrar registrar, ErameSettings settings){
            if (registrar == null) throw new ArgumentNullException(nameof(registrar));
            var config = settings != null ? settings : ErameSettings.Default;

            registrar.RegisterSingleton<IPlatformService>(p => CreatePlatformService(p.Get<ILogService>(), config.SteamAppId));
        }

        /// <summary>
        /// 平台服务工厂。
        /// Steam 实现位于独立程序集 Erame.Platform.Steam，核心运行时与游戏层都不直接引用 Steamworks，
        /// 因此这里用反射获取类型：即使工程没装 Steamworks 包，也能正常编译并降级到单机模式。
        /// </summary>
        internal static IPlatformService CreatePlatformService(ILogService log, uint steamAppId){
#if USE_STEAM && STEAMWORKS_AVAILABLE
            var steamType = FindSteamServiceType();
            if (steamType != null){
                return (IPlatformService)Activator.CreateInstance(steamType, log, steamAppId);
            }
            log.Warn("[Platform] 已开启 Steam 模式，但找不到 SteamService 程序集，降级为原生模式。");
#endif
            return new NullPlatformService(log);
        }

#if USE_STEAM && STEAMWORKS_AVAILABLE
        private const string SteamServiceTypeName = "Erame.Platform.Steam.SteamService";

        /// <summary>
        /// 在当前 AppDomain 的所有已加载程序集中查找 SteamService。
        /// 注意：Type.GetType("命名空间.类名") 只搜索调用方程序集与 mscorlib，
        /// 而 Erame.Runtime 并不引用 Erame.Platform.Steam，因此必须显式遍历。
        /// </summary>
        private static Type FindSteamServiceType(){
            foreach (var assembly in AppDomain.CurrentDomain.GetAssemblies()){
                var type = assembly.GetType(SteamServiceTypeName, false);
                if (type != null) return type;
            }

            // 兜底：该程序集尚未被加载时，按名称尝试加载
            try{
                return Assembly.Load("Erame.Platform.Steam").GetType(SteamServiceTypeName, false);
            }
            catch (Exception){
                return null;
            }
        }
#endif
    }
}
