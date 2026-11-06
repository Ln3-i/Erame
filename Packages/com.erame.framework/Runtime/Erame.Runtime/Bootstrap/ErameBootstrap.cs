using UnityEngine;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 场景启动组件：序列化引用一个 <see cref="ErameEntryProvider"/>，
    /// 由它创建入口并驱动整个生命周期。全程直接类型引用，不使用反射。
    /// </summary>
    public sealed class ErameBootstrap : MonoBehaviour{
        [Tooltip("入口提供者资产（由菜单 Erame/生成最小入口 创建）")]
        [SerializeField] private ErameEntryProvider provider;

        [Tooltip("跨场景常驻")]
        [SerializeField] private bool dontDestroyOnLoad = true;

        private void Awake(){
            if (dontDestroyOnLoad) DontDestroyOnLoad(gameObject);
        }

        private void Start(){
            if (provider == null){
                Debug.LogError("[Erame] ErameBootstrap 未指定 ErameEntryProvider，无法启动。");
                enabled = false;
                return;
            }

            var entry = provider.Create();
            if (entry == null){
                Debug.LogError("[Erame] ErameEntryProvider.Create() 返回 null。");
                enabled = false;
                return;
            }

            var builder = new ErameBuilder();
            provider.Configure(builder);
            ErameRuntime.Start(entry, builder);
        }

        private void Update(){
            if (ErameRuntime.IsStarted) ErameRuntime.Update();
        }

        private void OnDestroy(){
            ErameRuntime.Quit();
        }
    }
}
