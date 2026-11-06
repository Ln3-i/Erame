#if ERAME_AUTO_START
using UnityEngine;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 自动启动（可选）：默认不启用。
    /// 需要在 Player Settings 中定义 ERAME_AUTO_START 宏，并把入口提供者资产
    /// 放到 Resources/ErameEntryProvider.asset（名称即本类常量）。
    /// 启用后无需场景中放置 ErameBootstrap 即可在进入第一帧前完成启动。
    /// </summary>
    public static class ErameAutoStart{
        /// <summary>Resources 下提供者资产的固定路径。</summary>
        public const string ProviderResourcesPath = "ErameEntryProvider";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Start(){
            var provider = Resources.Load<ErameEntryProvider>(ProviderResourcesPath);
            if (provider == null){
                Debug.LogWarning($"[Erame] 已启用 ERAME_AUTO_START，但 Resources 中未找到 {ProviderResourcesPath}。");
                return;
            }

            if (ErameRuntime.IsStarted) return;

            var entry = provider.Create();
            if (entry == null){
                Debug.LogError("[Erame] ErameEntryProvider.Create() 返回 null，自动启动取消。");
                return;
            }

            var builder = new ErameBuilder();
            provider.Configure(builder);
            ErameRuntime.Start(entry, builder);

            // 自动启动没有场景组件负责主循环与退出，这里挂一个隐藏驱动器
            var driver = new GameObject("[Erame AutoStart]") { hideFlags = HideFlags.HideInHierarchy };
            Object.DontDestroyOnLoad(driver);
            driver.AddComponent<ErameAutoStartDriver>();
        }
    }

    /// <summary>自动启动模式下的主循环与退出驱动（不暴露给业务）。</summary>
    internal sealed class ErameAutoStartDriver : MonoBehaviour{
        private void Update(){
            if (ErameRuntime.IsStarted) ErameRuntime.Update();
        }

        private void OnDestroy(){
            ErameRuntime.Quit();
        }
    }
}
#endif
