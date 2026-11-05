using Erame.Runtime.Config;
using UnityEngine;

namespace Game.Scripts.Bootstrap{
    public sealed class GameBootstrap : MonoBehaviour{
        [SerializeField] private ErameSettings settings;

        private void Awake(){
            DontDestroyOnLoad(gameObject); 
        }

        private void Start(){
            GameApp.Start(settings);
        }

        private void Update(){
            if (!GameApp.IsStarted) return;

            // 这些访问器在服务缺失时会抛异常，因此不做 null 条件判断，让问题尽早暴露
            GameApp.Tick.Update(GameApp.Time.DeltaTime);
            GameApp.Flow.Update();
            GameApp.Platform.Update();
        }

        private void OnDestroy(){
            GameApp.Quit();
        }
    }
}