using Erame.Core;
using Erame.Runtime.Bootstrap;
using UnityEngine;

namespace Erame.Samples.Minimal{
    /// <summary>
    /// 最小入口示例：继承 ErameMonoEntry，挂到场景即可启动。
    /// 业务只需重写 Configure；OnStart / OnUpdate / OnQuit 可选。
    /// </summary>
    public sealed class MinimalEntry : ErameMonoEntry{
        public override void Configure(ErameBuilder builder){
            // builder.RegisterSingleton<IMyService>(p => new MyService());
            // builder.AddModule<MyModule>();
        }

        public override void OnStart(){
            Debug.Log("[Minimal] 入口启动成功，可以开始写游戏逻辑了。");
        }
    }
}
