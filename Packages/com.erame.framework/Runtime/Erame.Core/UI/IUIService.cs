using System.Threading.Tasks;

namespace Erame.Core{
    public interface IUIService{
        void RegisterPanel(IUIPanel panel);
        
        // 统一改成异步
        Task OpenPanelAsync(string panelKey);
        Task CloseTopPanelAsync(); 
        Task CloseAllAsync();
    }
}