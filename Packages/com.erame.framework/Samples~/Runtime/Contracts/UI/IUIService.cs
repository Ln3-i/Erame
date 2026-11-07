using System.Threading.Tasks;

namespace __NS__.Runtime{
    public interface IUIService{
        void RegisterPanel(IUIPanel panel);

        Task OpenPanelAsync(string panelKey);

        /// <summary>
        /// 带参数打开面板。参数在 <see cref="IUIPanel.OnShowAsync"/> 之前交给面板
        /// （面板实现 <see cref="IPanelArguments"/> 即可收到；未实现则忽略，保持旧行为）。
        /// </summary>
        Task OpenPanelAsync(string panelKey, object args);

        Task CloseTopPanelAsync();

        /// <summary>关闭指定面板；它不在栈顶时同样会正确移除，并恢复下层显示与输入状态。</summary>
        Task ClosePanelAsync(string panelKey);

        Task CloseAllAsync();

        /// <summary>该面板当前是否打开。</summary>
        bool IsOpen(string panelKey);
    }
}
