using Erame.Core;
using Erame.Runtime.Config;
using UnityEngine;

namespace Erame.Runtime.Bootstrap{
    /// <summary>
    /// 推荐入口：继承本类、在场景中挂载即可启动整个项目。
    /// 负责单例去重、DontDestroyOnLoad、生命周期驱动；业务只需重写 Configure，
    /// 可选重写 OnStart / OnUpdate / OnQuit。
    /// </summary>
    public abstract class ErameMonoEntry : MonoBehaviour, IErameEntry{
        private static ErameMonoEntry _instance;
        private bool _ownsRuntime;

        [Header("Erame 框架")]
        [Tooltip("框架设置资产（存档密钥 / 日志级别）。留空将使用运行时默认配置：存档密钥退化为开发期密钥。")]
        [SerializeField] private ErameSettings settings;

        /// <summary>入口引用的框架设置资产（未引用时为 null）。</summary>
        public ErameSettings Settings => settings;

        protected virtual void Awake(){
            if (_instance != null && _instance != this){
                Debug.LogWarning("[Erame] 场景中存在多个入口组件，已销毁重复实例。");
                enabled = false;
                Destroy(gameObject);
                return;
            }

            _instance = this;
            _ownsRuntime = true;
            DontDestroyOnLoad(gameObject);

            // 启动前把场景引用的设置资产设为运行时默认：ErameRuntime、业务模块与平台包
            // 都在启动流程中从 ErameSettings.Default 读配置，不接线则资产配置永远不生效
            if (settings != null){
                ErameSettings.OverrideDefault(settings);
            }
            else{
                Debug.LogWarning("[Erame] 入口未引用 ErameSettings 资产：存档密钥将退化为开发期密钥（存于 PlayerPrefs）。请在 Inspector 中指定设置资产。");
            }

            ErameRuntime.Start(this);
        }

        protected virtual void Update(){
            if (_ownsRuntime && ErameRuntime.IsStarted) ErameRuntime.Update(Time.deltaTime);
        }

        protected virtual void OnApplicationQuit(){
            if (_ownsRuntime) ErameRuntime.Quit();
        }

        protected virtual void OnDestroy(){
            if (_instance == this) _instance = null;
            if (_ownsRuntime) ErameRuntime.Quit();
        }

        /// <inheritdoc />
        public abstract void Configure(ErameBuilder builder);

        /// <inheritdoc />
        public virtual void OnStart(){
        }

        /// <inheritdoc />
        public virtual void OnUpdate(float deltaTime){
        }

        /// <inheritdoc />
        public virtual void OnQuit(){
        }
    }
}
