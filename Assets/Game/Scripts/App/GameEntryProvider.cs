using Erame.Core;
using Erame.Runtime.Bootstrap;
using Erame.Runtime.Config;
using UnityEngine;

namespace Game.Scripts.App{
    /// <summary>
    /// 入口提供者资产：持有配置并创建 GameEntry。
    /// 场景中的 ErameBootstrap 引用本资产来启动整个项目（不使用反射）。
    /// </summary>
    [CreateAssetMenu(fileName = "GameEntryProvider", menuName = "Erame/Entry Provider")]
    public sealed class GameEntryProvider : ErameEntryProvider{
        [Tooltip("可留空，留空时使用框架默认配置")]
        [SerializeField] private ErameSettings settings;

        public override ErameEntry Create() => new GameEntry();

        public override void Configure(ErameBuilder builder){
            builder.WithSettings(settings);
        }
    }
}
