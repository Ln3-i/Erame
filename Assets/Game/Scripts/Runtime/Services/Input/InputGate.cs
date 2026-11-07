using Game.Runtime;
using System;
using Erame.Core;
using System.Collections.Generic;

namespace Game.Runtime.Services{
    /// <summary>
    /// <see cref="IInputGate"/> 的默认实现：一个可嵌套的计数门闸。
    /// 由 FrameworkModule 注册（与 ITimeService 同级），UI/对话/过场共享同一个实例。
    /// </summary>
    public sealed class InputGate : IInputGate, IService{
        private readonly List<object> _owners = new();
        private readonly ILogService _log;

        public InputGate(ILogService log = null){
            _log = log;
        }

        public int BlockCount => _owners.Count;

        public bool IsBlocked => _owners.Count > 0;

        public event Action<bool> BlockedChanged;

        public void Initialize(){}

        public void Dispose(){
            _owners.Clear();
            BlockedChanged = null;
        }

        public IDisposable Block(object owner = null){
            _owners.Add(owner);
            if (_owners.Count == 1) RaiseChanged(true);
            return new BlockHandle(this, owner);
        }

        private void Release(object owner){
            int index = _owners.LastIndexOf(owner);
            if (index < 0) return; // 已经归还过，忽略

            _owners.RemoveAt(index);
            if (_owners.Count == 0) RaiseChanged(false);
        }

        private void RaiseChanged(bool blocked){
            var handler = BlockedChanged;
            if (handler == null) return;

            // 逐个订阅者隔离：一个监听器抛异常不应让其他监听器（例如音效、光标）收不到状态变化
            var invocationList = handler.GetInvocationList();
            for (int i = 0; i < invocationList.Length; i++){
                try{ ((Action<bool>)invocationList[i]).Invoke(blocked); }
                catch (Exception ex){ _log?.Error($"[Input] 门闸状态回调抛出异常: {ex.Message}"); }
            }
        }

        private sealed class BlockHandle : IDisposable{
            private InputGate _gate;
            private readonly object _owner;

            public BlockHandle(InputGate gate, object owner){
                _gate = gate;
                _owner = owner;
            }

            public void Dispose(){
                var gate = _gate;
                _gate = null; // 防重复归还
                gate?.Release(_owner);
            }
        }
    }
}
