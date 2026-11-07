using __NS__.Runtime;
using System;
using System.Collections.Generic;
using Erame.Core;

namespace __NS__.Runtime.Services{
    public class GameStateMachine : IGameStateMachine, IService{
        private readonly ILogService _log;
        private readonly Dictionary<Type, IGameState> _states = new();
        private IGameState _currentState;

        public GameStateMachine(ILogService log){
            _log = log;
        }

        public void Initialize() => _log.Info("[StateMachine] 状态机初始化完成");

        public void Dispose(){
            // 销毁时通知当前状态退出，与 ChangeState 的 OnExit 配对，
            // 避免状态在 OnEnter 里建立的订阅/资源无人清理
            _currentState?.OnExit();
            _currentState = null;
            _states.Clear();
            _log.Info("[StateMachine] 状态机已销毁");
        }

        public void RegisterState(IGameState state){
            _states[state.GetType()] = state;
        }

        public void ChangeState<T>() where T : class, IGameState{
            if (!_states.TryGetValue(typeof(T), out var newState)){
                _log.Error($"[StateMachine] 未注册状态: {typeof(T).Name}");
                return;
            }

            // 切到同一状态：直接忽略，否则 OnExit + OnEnter 会把状态内部数据清空重来
            if (ReferenceEquals(_currentState, newState)) return;

            _currentState?.OnExit();
            _currentState = newState;
            _currentState.OnEnter();
            _log.Info($"[StateMachine] 切换到状态: {typeof(T).Name}");
        }

        public void Update(){
            _currentState?.OnUpdate();
        }
    }
}