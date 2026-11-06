using Erame.Core;
using Erame.Runtime.Services;

namespace Game.Scripts.Modules{
    /// <summary>框架基础能力：资源与场景（日志/事件/时间已由 ErameRuntime 注册）。</summary>
    public sealed class FrameworkModule : IModule{
        public void Configure(IServiceRegistrar registrar){
            registrar.RegisterSingleton<IAssetService>(p => new AddressableAssetService(p.Get<ILogService>()));
            registrar.RegisterSingleton<ISceneService>(p => new UnitySceneService(p.Get<ILogService>(), p.Get<IAssetService>()));
        }
    }
}
