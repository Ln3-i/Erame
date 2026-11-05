using System.Threading.Tasks;
using Erame.Core;

namespace Erame.Runtime.Services{
    public class NullPlatformService : IPlatformService, IService{
        private readonly ILogService _log;

        public NullPlatformService(ILogService log){ _log = log; }

        private bool _initialized = false;

        public bool IsInitialized => _initialized;
        public string PlayerId => "OfflinePlayer";
        public string PlayerName => "OfflinePlayer";

        public void Initialize(){
            if (_initialized) return;
            _initialized = true;
            _log.Info("[Platform] 使用单机空平台服务");
        }

        public void Update(){ }
        public void Shutdown(){ }

        public Task<bool> LoginAsync(){
            return Task.FromResult(_initialized);
        }

        public void UnlockAchievement(string achievementId){
            _log.Info($"[Platform] (原生模式) 触发成就解锁: {achievementId}");
        }

        public void SetStat(string statName, int value){
            _log.Info($"[Platform] (原生模式) 更新数据统计: {statName} = {value}");
        }

        public bool IsDlcInstalled(uint dlcId){
            return false;
        }

        public void Dispose(){
            _log.Info("[Platform] 原生模式服务已销毁");
        }
    }
}