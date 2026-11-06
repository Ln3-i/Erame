using Erame.Core;
using Erame.Runtime.Bootstrap;
using Erame.Runtime.Config;
using UnityEngine;

namespace Erame.Samples.Minimal{
    /// <summary>
    /// 最小入口提供者：持有配置并创建入口实例。
    /// 把本资产拖到场景中 ErameBootstrap 的 Provider 字段上即可启动。
    /// </summary>
    [CreateAssetMenu(fileName = "MinimalEntryProvider", menuName = "Erame/Minimal Entry Provider")]
    public sealed class MinimalEntryProvider : ErameEntryProvider{
        [Tooltip("可留空，留空时使用框架默认配置")]
        [SerializeField] private ErameSettings settings;

        public override ErameEntry Create() => new MinimalEntry();

        public override void Configure(ErameBuilder builder){
            builder.WithSettings(settings);
        }
    }
}
