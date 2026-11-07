using Game.Runtime;
using System;
using System.Collections.Generic;
using Erame.Core;

namespace Game.Runtime.Services{
    public sealed class DataRegistryService : IDataRegistry, IService{
        // 用 Dictionary 存储所有的表。因为表是泛型的，所以这里存 object，取出来时强转。
        private readonly Dictionary<string, object> _tables = new Dictionary<string, object>(32);
        private readonly ILogService _log;

        public DataRegistryService(ILogService log){
            _log = log;
        }

        public void Initialize(){
            _log.Info("[DataRegistry] 数据注册表服务初始化完成");
        }

        public void Dispose(){
            _tables.Clear();
            _log.Info("[DataRegistry] 数据注册表已清空");
        }

        public void Register<T>(string name, DataTable<T> table) where T : class, IData{
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("表名不能为空", nameof(name));

            if (_tables.ContainsKey(name))
                _log.Warn($"[DataRegistry] 表名冲突，覆盖旧表: {name}");

            _tables[name] = table;
        }

        public DataTable<T> Get<T>(string name) where T : class, IData{
            if (_tables.TryGetValue(name, out var tableObj))
                return tableObj as DataTable<T>;
                
            _log.Error($"[DataRegistry] 找不到数据表: {name}");
            return null;
        }
    }
}