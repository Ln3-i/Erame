using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Erame.Core;

namespace Erame.Runtime.Services{
    public sealed class UIService : IUIService, IService{
        private readonly ILogService _log;
        private readonly IInputService _inputService;
        private readonly UIInputContext _uiContext = new();
        private readonly Dictionary<string, IUIPanel> _panels = new();
        private readonly List<IUIPanel> _stack = new();

        // 串行化面板操作，避免快速连续点击导致面板栈与输入上下文错乱
        private readonly SemaphoreSlim _operationLock = new(1, 1);
        private bool _inputContextPushed;

        public UIService(ILogService log, IInputService inputService){
            _log = log;
            _inputService = inputService;
        }

        public void Initialize() => _log.Info("[UI] UI服务初始化完成");

        public void Dispose(){
            // Dispose 是同步接口，无法等待隐藏动画；这里主动发起关闭并清空栈
            _ = CloseAllAsync();
            _panels.Clear();
        }

        public void RegisterPanel(IUIPanel panel) => _panels[panel.PanelKey] = panel;

        public async Task OpenPanelAsync(string panelKey){
            await _operationLock.WaitAsync();
            try{
                if (!_panels.TryGetValue(panelKey, out var panel)) return;
                if (_stack.Contains(panel)) _stack.Remove(panel);

                // 第一个面板：拦截游戏输入
                if (!_inputContextPushed){
                    _inputService.PushContext(_uiContext);
                    _inputContextPushed = true;
                    _log.Info("[UI] 压入 UI 输入上下文（屏蔽游戏操作）");
                }

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
                _stack.RemoveAt(_stack.Count - 1);

                await top.OnHideAsync();

                if (_stack.Count == 0 && _inputContextPushed){
                    _inputService.PopContext(_uiContext);
                    _inputContextPushed = false;
                    _log.Info("[UI] 移除 UI 输入上下文（恢复游戏操作）");
                }

                // 重新显示底层面板
                if (_stack.Count > 0){
                    await _stack[_stack.Count - 1].OnShowAsync();
                }
                _log.Info($"[UI] 关闭面板: {top.PanelKey}");
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

                if (_inputContextPushed){
                    _inputService.PopContext(_uiContext);
                    _inputContextPushed = false;
                }
                _log.Info("[UI] 已关闭所有面板");
            }
            finally{
                _operationLock.Release();
            }
        }
    }
}
