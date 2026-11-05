using Erame.Core;
using Erame.Runtime.Bootstrap;
using Erame.Runtime.Config;
using Game.Scripts.Input;

namespace Game.Scripts.Modules{
    /// <summary>框架基础能力：日志、事件、时间、资源、场景。</summary>
    public sealed class FrameworkModule : IModule{
        private readonly ErameSettings _settings;

        public FrameworkModule(ErameSettings settings){ _settings = settings; }

        public void Configure(IServiceRegistrar registrar){
            ErameInstaller.InstallFramework(registrar, _settings);
        }
    }

    /// <summary>游戏运行时服务，并绑定本作独有的输入适配器。</summary>
    public sealed class GameplayModule : IModule{
        private readonly ErameSettings _settings;

        public GameplayModule(ErameSettings settings){ _settings = settings; }

        public void Configure(IServiceRegistrar registrar){
            ErameInstaller.InstallGameplay(registrar, _settings, _ => new GameInputAdapter());
        }
    }

    /// <summary>平台能力：Steam / 原生空实现。</summary>
    public sealed class PlatformModule : IModule{
        private readonly ErameSettings _settings;

        public PlatformModule(ErameSettings settings){ _settings = settings; }

        public void Configure(IServiceRegistrar registrar){
            ErameInstaller.InstallPlatform(registrar, _settings);
        }
    }
}
