using Erame.Core;
using Erame.Runtime.Bootstrap;
using Game.Scripts.Modules;

namespace Game{
    /// <summary>项目入口：挂到场景中即可启动整个项目。</summary>
    public sealed class GameEntry : ErameMonoEntry{
        public override void Configure(ErameBuilder builder){
            builder.AddModule<FrameworkModule>().AddModule<GameplayModule>();
        }

        public override void OnStart(){
            // 切换到初始游戏状态（待业务状态机补全后填写）
        }

        public override void OnQuit(){
            // 退出前的收尾
        }
    }
}
