using Game.Runtime;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Erame.Core;

namespace Game.Runtime.Services{
    /// <summary>
    /// 面板栈式 UI 服务。
    /// 输入屏蔽走 <see cref="IInputGate"/>（与输入帧形状无关）；未注册门闸时退回旧行为（压入 UIInputContext）。
    /// </summary>
    public sealed class UIService : IUIService, IService{
        private readonly ILogService _log;
        private readonly IInputGate _gate;
        private readonly IInputService _legacyInputService;
        private readonly UIInputContext _uiContext = new();

        private readonly Dictionary<string, IUIPanel> _panels = new();
        private readonly List<IUIPanel> _stack = new();

        // 串行化面板操作，避免快速连续点击导致面板栈与输入上下文错乱
        private readonly SemaphoreSlim _operationLock = new(1, 1);

        private bool _blockHeld;
        private IDisposable _blockHandle;

        /// <summary>推荐用法：输入屏蔽交给门闸。</summary>
        public UIService(ILogService log, IInputGate inputGate){
            _log = log;
            _gate = inputGate ?? throw new ArgumentNullException(nameof(inputGate));
        }

        /// <summary>兼容旧注册方式：内部压入 UIInputContext 屏蔽输入。</summary>
        public UIService(ILogService log, IInputService inputService){
            _log = log;
            _legacyInputService = inputService ?? throw new ArgumentNullException(nameof(inputService));
        }

        public void Initialize() => _log.Info("[UI] UI服务初始化完成");

        public void Dispose(){
            // Dispose 是同步接口，无法等待隐藏动画：同步触发各面板关闭并立刻清理。
            // 不能 fire-and-forget CloseAllAsync——它可能在容器销毁后才执行，
            // 届时面板回调会碰到已失效的依赖；输入屏蔽也必须在 Dispose 内归还，
            // 否则服务销毁后游戏操作输入仍被 UI 屏蔽
            for (int i = _stack.Count - 1; i >= 0; i--){
                var panel = _stack[i];
                try{
                    var task = panel.OnHideAsync();
                    if (task.IsFaulted){
                        _log.Error($"[UI] 关闭面板 {panel.PanelKey} 失败: {task.Exception?.GetBaseException().Message}");
                    }
                }
                catch (Exception ex){
                    _log.Error($"[UI] 关闭面板 {panel.PanelKey} 抛出异常: {ex.Message}");
                }
            }
            _stack.Clear();
            _panels.Clear();

            EndInputBlock();
            _operationLock.Dispose();
        }

        public void RegisterPanel(IUIPanel panel){
            if (panel == null) throw new ArgumentNullException(nameof(panel));
            if (string.IsNullOrEmpty(panel.PanelKey)) throw new ArgumentException("面板 PanelKey 不能为空", nameof(panel));

            // 场景重载后重新注册是正常流程，允许覆盖；但同名不同实例时给出提示，便于排查漏注销
            if (_panels.TryGetValue(panel.PanelKey, out var existing) && !ReferenceEquals(existing, panel)){
                _log.Warn($"[UI] 面板 {panel.PanelKey} 被重新注册（旧实例将被替换）");
            }
            _panels[panel.PanelKey] = panel;
        }

        public Task OpenPanelAsync(string panelKey) => OpenPanelAsync(panelKey, null);

        public async Task OpenPanelAsync(string panelKey, object args){
            await _operationLock.WaitAsync();
            try{
                if (!_panels.TryGetValue(panelKey, out var panel)){
                    _log.Warn($"[UI] 面板未注册，无法打开: {panelKey}");
                    return;
                }

                if (_stack.Contains(panel)) _stack.Remove(panel);

                // 参数必须先于 OnShowAsync：面板在这里准备数据，OnShow 只负责呈现
                if (panel is IPanelArguments receiver) receiver.SetArguments(args);

                // 第一个面板：拦截游戏输入
                BeginInputBlock();

                // 隐藏底层面板
                if (_stack.Count > 0){
                    await _stack[_stack.Count - 1].OnHideAsync();
                }

                _stack.Add(panel);
                await panel.OnShowAsync();
                _log.Info($"[UI] 打开面板: {panelKey}");
            }
            finally{
                _operationLock.Release();
            }
        }

        public async Task CloseTopPanelAsync(){
            await _operationLock.WaitAsync();
            try{
                if (_stack.Count == 0) return;

                var top = _stack[_stack.Count - 1];
                await PopAtAsync(_stack.Count - 1);

                // 重新显示底层面板（栈内不存重复面板，移除后栈顶必定是下一层）
                if (_stack.Count > 0){
                    await _stack[_stack.Count - 1].OnShowAsync();
                }
                _log.Info($"[UI] 关闭面板: {top.PanelKey}");
            }
            finally{
                _operationLock.Release();
            }
        }

        public async Task ClosePanelAsync(string panelKey){
            await _operationLock.WaitAsync();
            try{
                int index = _stack.FindIndex(p => p.PanelKey == panelKey);
                if (index < 0) return;

                bool wasTop = index == _stack.Count - 1;
                var panel = _stack[index];
                await PopAtAsync(index);

                if (wasTop && _stack.Count > 0){
                    await _stack[_stack.Count - 1].OnShowAsync();
                }
                _log.Info($"[UI] 关闭面板: {panel.PanelKey}");
            }
            finally{
                _operationLock.Release();
            }
        }

        public async Task CloseAllAsync(){
            await _operationLock.WaitAsync();
            try{
                for (int i = _stack.Count - 1; i >= 0; i--){
                    await _stack[i].OnHideAsync();
                }
                _stack.Clear();
                EndInputBlock();
                _log.Info("[UI] 已关闭所有面板");
            }
            finally{
                _operationLock.Release();
            }
        }

        public bool IsOpen(string panelKey){
            for (int i = 0; i < _stack.Count; i++){
                if (_stack[i].PanelKey == panelKey) return true;
            }
            return false;
        }

        // 隐藏并移除指定下标的面板；栈空时归还输入屏蔽
        private async Task PopAtAsync(int index){
            var panel = _stack[index];
            _stack.RemoveAt(index);

            await panel.OnHideAsync();

            if (_stack.Count == 0){
                EndInputBlock();
            }
        }

        private void BeginInputBlock(){
            if (_blockHeld) return;

            if (_gate != null){
                _blockHandle = _gate.Block(this);
                _log.Info("[UI] 门闸拦截游戏输入");
            }
            else if (_legacyInputService != null){
                _legacyInputService.PushContext(_uiContext);
                _log.Info("[UI] 压入 UI 输入上下文（屏蔽游戏操作）");
            }

            _blockHeld = true;
        }

        private void EndInputBlock(){
            if (!_blockHeld) return;
            _blockHeld = false;

            if (_gate != null){
                _blockHandle?.Dispose();
                _blockHandle = null;
                _log.Info("[UI] 门闸恢复游戏输入");
            }
            else if (_legacyInputService != null){
                _legacyInputService.PopContext(_uiContext);
                _log.Info("[UI] 移除 UI 输入上下文（恢复游戏操作）");
            }
        }
    }
}
