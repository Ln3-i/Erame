using Erame.Core;
using UnityEngine;

namespace Erame.Samples.Minimal{
    /// <summary>
    /// 最小入口示例：只需继承 ErameEntry，按需重写四个虚方法即可。
    /// Configure：业务注册（框架默认服务由 ErameRuntime 注册）
    /// OnStart：启动完成；OnUpdate：主循环；OnQuit：退出清理。
    /// </summary>
    public sealed class MinimalEntry : ErameEntry{
        public override void OnStart(){
            Debug.Log("[Minimal] 入口启动完成，可以开始编写游戏逻辑了。");
        }
    }
}
