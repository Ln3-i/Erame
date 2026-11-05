using System;
using System.Collections.Generic;
using Erame.Core;

namespace Erame.Runtime.Services{
    public class TickService : ITickService, IService{
        private readonly ILogService _log;
        private float _accumulator = 0f;
        private readonly List<Action> _listeners = new();
        
        // 防止卡顿导致“死亡螺旋”（比如切出后台，回来时瞬间算 1000 次）
        private const int MaxTicksPerFrame = 5; 

        public float TickInterval { get; set; } = 0.1f; // 默认 1秒 10个Tick
        public long CurrentTick { get; private set; } = 0;

        public TickService(ILogService log){
            _log = log;
        }

        public void Initialize() => _log.Info($"[Tick] Tick调度器初始化，间隔: {TickInterval}s");
        public void Dispose() => _listeners.Clear();

        public void AddTickListener(Action onTick){
            if (!_listeners.Contains(onTick)) _listeners.Add(onTick);
        }

        public void RemoveTickListener(Action onTick){
            _listeners.Remove(onTick);
        }

        public void Update(float deltaTime){
            _accumulator += deltaTime;
            int ticksProcessed = 0;

            while (_accumulator >= TickInterval && ticksProcessed < MaxTicksPerFrame){
                _accumulator -= TickInterval;
                ticksProcessed++;
                CurrentTick++;

                // 触发所有监听者的 Tick 逻辑
                // 注意：使用 for 而不是 foreach，防止在 Tick 过程中有人增删监听器导致报错
                for (int i = _listeners.Count - 1; i >= 0; i--){
                    try {
                        _listeners[i]?.Invoke();
                    } catch (Exception ex) {
                        _log.Error($"[Tick] 执行错误: {ex.Message}");
                    }
                }
            }
            
            // 如果帧率太低导致溢出了，丢弃多余的时间，防止死机
            if (_accumulator > TickInterval * MaxTicksPerFrame){
                _accumulator = 0f; 
            }
        }
    }
}