using Erame.Core;
using Erame.Runtime.Bootstrap;
using UnityEngine;

namespace Erame.Platform.Wegame{
    /// <summary>Wegame 平台模块：注册占位实现，等待接入 WeGame SDK。</summary>
    internal sealed class WegameModule : IModule{
        public void Configure(IServiceRegistrar registrar){
            registrar.RegisterSingleton<IPlatformService>(p => new WegameService(p.Get<ILogService>()));
            registrar.RegisterSingleton<IPlatformAchievementService>(p => (IPlatformAchievementService)p.Get<IPlatformService>());
            registrar.RegisterSingleton<IPlatformStatsService>(p => (IPlatformStatsService)p.Get<IPlatformService>());
            registrar.RegisterSingleton<IPlatformPresenceService>(p => (IPlatformPresenceService)p.Get<IPlatformService>());
            registrar.RegisterSingleton<IPlatformOverlayService>(p => (IPlatformOverlayService)p.Get<IPlatformService>());
            registrar.RegisterSingleton<IPlatformFriendService>(p => (IPlatformFriendService)p.Get<IPlatformService>());
            registrar.RegisterSingleton<IPlatformDlcService>(p => (IPlatformDlcService)p.Get<IPlatformService>());
            registrar.RegisterSingleton<IPlatformCloudService>(p => (IPlatformCloudService)p.Get<IPlatformService>());
            registrar.RegisterSingleton<IPlatformLobbyService>(p => (IPlatformLobbyService)p.Get<IPlatformService>());
            registrar.RegisterSingleton<IPlatformWorkshopService>(p => (IPlatformWorkshopService)p.Get<IPlatformService>());
        }
    }

    internal static class WegameModuleRegistration{
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Register(){
            ErameRuntime.ProvidePlatformModule(new WegameModule());
        }
    }
}
