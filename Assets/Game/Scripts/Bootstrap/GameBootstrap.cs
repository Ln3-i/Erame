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
            // Tick / 流程状态机 / 平台的推进由入口类 ErameEntry 的默认主循环处理
            GameApp.Update();
        }

        private void OnDestroy(){
            GameApp.Quit();
        }
    }
}