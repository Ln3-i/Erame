namespace __NS__.Runtime{
    // 数据注册表，用于统一管理游戏里所有的 DataTable
    public interface IDataRegistry{
        // 注册一张表
        void Register<T>(string name, DataTable<T> table) where T : class, IData;
        
        // 获取一张表
        DataTable<T> Get<T>(string name) where T : class, IData;
    }
}