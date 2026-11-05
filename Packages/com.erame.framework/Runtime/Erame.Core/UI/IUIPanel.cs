using System.Threading.Tasks;

namespace Erame.Core{
    public interface IUIPanel{
        string PanelKey { get; }
        void OnCreate(ILogService log, IEventBus eventBus);
        
        // 改为异步方法
        Task OnShowAsync();
        Task OnHideAsync();
        
        void OnDestroy();
    }
}