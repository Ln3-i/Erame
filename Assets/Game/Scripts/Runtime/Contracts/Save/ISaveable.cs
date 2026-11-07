namespace Game.Runtime{
    // 任何需要存档的系统，都要实现这个接口
    public interface ISaveable{
        // 存档的唯一标识，比如 "PlayerData", "Inventory"
        string SaveKey { get; }

        // 将自己的数据写出去
        void Save(SaveWriter writer);

        // 从读取器里恢复自己的数据
        void Load(SaveReader reader);
    }
}