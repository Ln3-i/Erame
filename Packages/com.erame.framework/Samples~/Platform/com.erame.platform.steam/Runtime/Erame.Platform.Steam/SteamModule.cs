using Erame.Core;
using Erame.Runtime.Bootstrap;
using Erame.Runtime.Config;
using UnityEngine;

namespace Erame.Platform.Steam{
    /// <summary>
    /// Steam 平台模块：注册身份/生命周期，以及成就、统计、富状态、Overlay、好友、DLC、
    /// 大厅、云存档、创意工坊等能力。本程序集只在 USE_STEAM + STEAMWORKS_AVAILABLE 时编译。
    ///
    /// 注册方式统一为“先注册具体实现，再以接口视图暴露”，这样同一个实例可以被多个接口解析
    /// （此前 SteamPlayerFeatures 被 new 了 6 次，白白浪费且状态不一致）。
    /// </summary>
    internal sealed class SteamModule : IModule{
        public void Configure(IServiceRegistrar registrar){
            var settings = ErameSettings.Default;

            registrar.RegisterSingleton<SteamService>(p => {
                // 本地化服务是可选的：没注册时跳过语言同步，而不是让 Steam 初始化失败
                p.TryGet<ILocalizationService>(out var localization);
                return new SteamService(p.Get<ILogService>(), settings.SteamAppId, localization, settings.SteamSyncLanguage);
            });

            registrar.RegisterSingleton<IPlatformService>(p => p.Get<SteamService>());
            registrar.RegisterSingleton<ISteamIntegration>(p => p.Get<SteamService>());

            // 六个玩家向接口共享同一实例
            registrar.RegisterSingleton<SteamPlayerFeatures>(p =>
                new SteamPlayerFeatures(p.Get<IPlatformService>(), p.Get<ISteamIntegration>()));

            registrar.RegisterSingleton<IPlatformAchievementService>(p => p.Get<SteamPlayerFeatures>());
            registrar.RegisterSingleton<IPlatformStatsService>(p => p.Get<SteamPlayerFeatures>());
            registrar.RegisterSingleton<IPlatformPresenceService>(p => p.Get<SteamPlayerFeatures>());
            registrar.RegisterSingleton<IPlatformOverlayService>(p => p.Get<SteamPlayerFeatures>());
            registrar.RegisterSingleton<IPlatformFriendService>(p => p.Get<SteamPlayerFeatures>());
            registrar.RegisterSingleton<IPlatformDlcService>(p => p.Get<SteamPlayerFeatures>());

            registrar.RegisterSingleton<SteamLobbyService>(p =>
                new SteamLobbyService(p.Get<ILogService>(), p.Get<IPlatformService>()));
            registrar.RegisterSingleton<IPlatformLobbyService>(p => p.Get<SteamLobbyService>());
            registrar.RegisterSingleton<ISteamLobbyExtras>(p => p.Get<SteamLobbyService>());

            registrar.RegisterSingleton<SteamCloudService>(p => new SteamCloudService(p.Get<IPlatformService>()));
            registrar.RegisterSingleton<IPlatformCloudService>(p => p.Get<SteamCloudService>());
            registrar.RegisterSingleton<ISteamCloudExtras>(p => p.Get<SteamCloudService>());

            registrar.RegisterSingleton<SteamWorkshopService>(p =>
                new SteamWorkshopService(p.Get<ILogService>(), p.Get<IPlatformService>()));
            registrar.RegisterSingleton<IPlatformWorkshopService>(p => p.Get<SteamWorkshopService>());
            registrar.RegisterSingleton<ISteamWorkshopExtras>(p => p.Get<SteamWorkshopService>());
        }
    }

    /// <summary>场景加载前把 Steam 模块“提供”给 ErameRuntime；由入口 builder.AddPlatformModule() 显式开启。静态调用、无反射。</summary>
    internal static class SteamModuleRegistration{
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register(){
            ErameRuntime.ProvidePlatformModule(new SteamModule());
        }
    }
}
