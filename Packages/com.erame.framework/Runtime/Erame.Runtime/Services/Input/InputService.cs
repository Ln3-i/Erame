using System.Collections.Generic;
using Erame.Core;

namespace Erame.Runtime.Services{
    public sealed class InputService : IInputService, IService{
        private readonly ILogService _log;
        private readonly List<IInputContext> _contexts = new();
        private IInputSource _defaultSource;

        public InputService(ILogService log) { _log = log; }

        public void Initialize() => _log.Info("[Input] 输入服务初始化");

        public void Dispose(){
            _defaultSource?.Dispose();
            _defaultSource = null;
            _contexts.Clear();
            _log.Info("[Input] 输入服务已销毁");
        }

        public void SetSource(IInputSource source){
            if (source == _defaultSource) return;
            _defaultSource?.Dispose();
            _defaultSource = source;
        }

        public void PushContext(IInputContext context){
            _contexts.Add(context);
            _contexts.Sort((a, b) => b.Priority.CompareTo(a.Priority)); 
            _log.Info($"[Input] 压入输入上下文: {context.GetType().Name}");
        }

        public void PopContext(IInputContext context){
            if (_contexts.Remove(context)){
                _log.Info($"[Input] 移除输入上下文: {context.GetType().Name}");
            }
        }

        public InputFrame GetCurrentFrame(){
            foreach (var ctx in _contexts){
                if (ctx.IsBlocking)
                    return ctx.GetInputFrame();
            }
            return _defaultSource?.GetInputFrame() ?? InputFrame.Empty;
        }
    }
}