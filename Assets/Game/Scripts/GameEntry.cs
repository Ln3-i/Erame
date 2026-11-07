using Erame.Core;
using Erame.Runtime.Bootstrap;
using Game.Scripts.Modules;

namespace Game{
    /// <summary>
    /// 项目入口：把本组件挂到场景对象上即可启动整个项目。
    /// 模块按能力域拆分，不需要的领域删掉对应 *Module.cs 和下面一行 AddModule 即可。
    /// </summary>
    public sealed class GameEntry : ErameMonoEntry{
        public override void Configure(ErameBuilder builder){
            builder
                .AddModule<LogModule>()
                .AddModule<EventsModule>()
                .AddModule<TimeModule>()
                .AddModule<AssetsModule>()
                .AddModule<InputModule>()
                .AddModule<UIModule>()
                .AddModule<PoolModule>()
                .AddModule<AudioModule>()
                .AddModule<LocalizationModule>()
                .AddModule<SaveModule>()
                .AddModule<DataModule>()
                .AddModule<FlowModule>()
                .AddModule<CommandModule>()
                .AddModule<SessionModule>();
        }
    }
}
