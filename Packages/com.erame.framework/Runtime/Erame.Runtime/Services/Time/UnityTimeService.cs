using UnityEngine;
using Erame.Core;

namespace Erame.Runtime.Services{
    public sealed class UnityTimeService : ITimeService, IService{
        private readonly ILogService _log;

        public UnityTimeService(ILogService log){
            _log = log;
        }

        public float DeltaTime => UnityEngine.Time.deltaTime;
        public float FixedDeltaTime => UnityEngine.Time.fixedDeltaTime;
        public float UnscaledDeltaTime => UnityEngine.Time.unscaledDeltaTime;

        public float TimeScale{
            get => UnityEngine.Time.timeScale;
            set{
                UnityEngine.Time.timeScale = value;
                _log.Info($"[Time] 游戏流速变更为: {value}x");
            }
        }

        public bool IsPaused => TimeScale == 0f;

        public void Initialize(){
            // 启动时确保是正常速度，防止上次没退干净
            UnityEngine.Time.timeScale = 1f;
            _log.Info("[Time] 时间服务初始化完成");
        }

        public void Dispose(){
            UnityEngine.Time.timeScale = 1f; // 退出时重置
            _log.Info("[Time] 时间服务销毁");
        }
    }
}