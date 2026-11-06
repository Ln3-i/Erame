using Erame.Core;
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
