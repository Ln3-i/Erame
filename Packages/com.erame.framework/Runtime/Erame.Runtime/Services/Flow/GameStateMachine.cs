using System;
using System.Collections.Generic;
using Erame.Core;

namespace Erame.Runtime.Services{
    public class GameStateMachine : IGameStateMachine, IService{
        private readonly ILogService _log;
        private readonly Dictionary<Type, IGameState> _states = new();
        private IGameState _currentState;

        public GameStateMachine(ILogService log){
            _log = log;
        }

        public void Initialize() => _log.Info("[StateMachine] 状态机初始化完成");
        public void Dispose() => _log.Info("[StateMachine] 状态机已销毁");

        public void RegisterState(IGameState state){
            _states[state.GetType()] = state;
        }

        public void ChangeState<T>() where T : class, IGameState{
            if (!_states.TryGetValue(typeof(T), out var newState)){
                _log.Error($"[StateMachine] 未注册状态: {typeof(T).Name}");
                return;
            }

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