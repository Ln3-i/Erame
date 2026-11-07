using System.Threading.Tasks;

namespace Erame.Platform{
    /// <summary>
    /// 平台身份与生命周期。其余能力（成就/统计/好友/大厅等）拆分为独立接口，
    /// 平台包只注册它真正实现的能力，业务按需 TryGet。
    /// </summary>
    public interface IPlatformService{
        bool IsInitialized { get; }

        /// <summary>平台名称，如 Steam / Epic / Wegame。</summary>
        string PlatformName { get; }

        string PlayerId { get; }
        string PlayerName { get; }

        void Initialize();
        void Update();
        void Shutdown();

        Task<bool> LoginAsync();
    }
}
