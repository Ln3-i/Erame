using __NS__.Runtime;
using System;
using System.Collections.Generic;
using Erame.Core;

namespace __NS__.Runtime.Services{
    /// <summary>
    /// 固定步长逻辑时钟。
    /// 与渲染帧率解耦：每 <see cref="TickInterval"/> 秒推进一次 CurrentTick，供模拟/生产/需求等系统使用。
    /// 暂停与变速只影响逻辑时间，不碰 Time.timeScale。
    /// </summary>
    public class TickService : ITickService, IService, IFrameUpdate{
        private readonly ILogService _log;
        private float _accumulator = 0f;
        private readonly List<Action> _listeners = new();

        public TickService(ILogService log){
            _log = log;
        }

        /// <summary>单帧最多补跑的 Tick 数，防止卡顿/切后台回来时“死亡螺旋”。</summary>
        public int MaxTicksPerFrame { get; set; } = 5;

        public float TickInterval { get; set; } = 0.1f; // 默认 1秒 10个Tick

        public long CurrentTick { get; private set; } = 0;

        public bool IsPaused { get; private set; }

        private float _timeScale = 1f;

        public float TimeScale{
            get => _timeScale;
            set => _timeScale = value < 0f ? 0f : value; // 负值没有物理意义，钳到 0（等价暂停推进）
        }

        public void Initialize() => _log.Info($"[Tick] Tick调度器初始化，间隔: {TickInterval}s");
        public void Dispose() => _listeners.Clear();

        public void Pause(){
            if (IsPaused) return;
            IsPaused = true;
            _log.Info("[Tick] 逻辑时间已暂停");
        }

        public void Resume(){
            if (!IsPaused) return;
            IsPaused = false;
            _accumulator = 0f; // 丢弃暂停期间残留的零头，避免恢复瞬间补跑一格
            _log.Info("[Tick] 逻辑时间已恢复");
        }

        public void AddTickListener(Action onTick){
            if (onTick != null && !_listeners.Contains(onTick)) _listeners.Add(onTick);
        }

        public void RemoveTickListener(Action onTick){
            if (onTick != null) _listeners.Remove(onTick);
        }

        public void Update(float deltaTime){
            if (IsPaused || _timeScale <= 0f) return;
            if (TickInterval <= 0f) return; // 间隔非法时不推进，避免死循环

            _accumulator += deltaTime * _timeScale;
            int ticksProcessed = 0;

            while (_accumulator >= TickInterval && ticksProcessed < MaxTicksPerFrame){
                _accumulator -= TickInterval;
                ticksProcessed++;
                CurrentTick++;

                DispatchTick();
            }

            // 帧率过低导致积压时丢弃多余时间，防止“补帧越补越卡”
            if (_accumulator > TickInterval * MaxTicksPerFrame){
                _accumulator = 0f;
            }
        }

        private void DispatchTick(){
            if (_listeners.Count == 0) return;

            // 每次派发前取快照：回调中增删监听器不影响本次遍历。
            // 正序执行 = 注册顺序，便于业务推理先后关系（旧实现是逆序，语义不直观）。
            var snapshot = _listeners.ToArray();
            for (int i = 0; i < snapshot.Length; i++){
                try{
                    snapshot[i]?.Invoke();
                }
                catch (Exception ex){
                    _log.Error($"[Tick] 执行错误: {ex.Message}");
                }
            }
        }
    }
}
