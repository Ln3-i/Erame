using Game.Runtime;
using System;
using System.Collections.Generic;
using Erame.Core;

namespace Game.Runtime.Services{
    /// <summary>
    /// 与输入帧形状无关的输入栈：稳定优先级插入的上下文 + 一个默认输入源。
    /// <typeparamref name="TFrame"/> 完全由业务定义，框架不再假定 Move/Attack 这类字段。
    /// 模态屏蔽交给 <see cref="IInputGate"/>，UI 不再需要伪造一个“空输入帧”。
    /// </summary>
    public class InputService<TFrame> : IInputService<TFrame>, IService{
        private readonly ILogService _log;
        private readonly IInputGate _gate;
        private readonly List<IInputContext<TFrame>> _contexts = new();
        private IInputSource<TFrame> _defaultSource;

        public InputService(ILogService log, IInputGate gate = null){
            _log = log;
            _gate = gate;
        }

        public void Initialize() => _log.Info($"[Input] 输入服务初始化（帧类型 {typeof(TFrame).Name}）");

        public virtual void Dispose(){
            _defaultSource?.Dispose();
            _defaultSource = null;
            _contexts.Clear();
            _log.Info("[Input] 输入服务已销毁");
        }

        public void SetSource(IInputSource<TFrame> source){
            if (ReferenceEquals(source, _defaultSource)) return;
            _defaultSource?.Dispose();
            _defaultSource = source;
        }

        public void PushContext(IInputContext<TFrame> context){
            if (context == null) return;

            // 稳定插入：按 Priority 降序排列，同优先级保持先压入者在前。
            // 不能用 List.Sort——它是不稳定排序，会打乱同优先级上下文的压入顺序
            int index = _contexts.Count;
            while (index > 0 && _contexts[index - 1].Priority < context.Priority){
                index--;
            }
            _contexts.Insert(index, context);
            _log.Info($"[Input] 压入输入上下文: {context.GetType().Name}");
        }

        public void PopContext(IInputContext<TFrame> context){
            if (context != null && _contexts.Remove(context)){
                _log.Info($"[Input] 移除输入上下文: {context.GetType().Name}");
            }
        }

        public virtual TFrame GetCurrentFrame(){
            // 门闸优先：UI/对话/过场拦截时不产生任何游戏输入
            if (_gate != null && _gate.IsBlocked) return default;

            foreach (var ctx in _contexts){
                if (ctx.IsBlocking)
                    return ctx.GetInputFrame();
            }
            return _defaultSource != null ? _defaultSource.GetInputFrame() : default;
        }
    }

    /// <summary>
    /// 兼容层：固定 <see cref="InputFrame"/> 的旧接口实现。
    /// 新项目请注册 <c>IInputService&lt;TFrame&gt;</c> 与 <c>InputService&lt;TFrame&gt;</c>。
    /// </summary>
    public sealed class InputService : InputService<InputFrame>, IInputService{
        public InputService(ILogService log, IInputGate gate = null) : base(log, gate){ }

        // 旧接口签名接收的是基接口，转交给泛型实现（IInputSource : IInputSource<InputFrame>）
        public void SetSource(IInputSource source) => SetSource((IInputSource<InputFrame>)source);

        public void PushContext(IInputContext context) => PushContext((IInputContext<InputFrame>)context);

        public void PopContext(IInputContext context) => PopContext((IInputContext<InputFrame>)context);
    }
}
