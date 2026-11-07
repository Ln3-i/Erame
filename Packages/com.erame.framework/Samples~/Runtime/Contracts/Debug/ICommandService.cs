namespace __NS__.Runtime{
    public interface ICommandService{
        // 注册一条指令
        void RegisterCommand(IDebugCommand command);
        
        // 执行玩家输入的一整串字符串，比如 "add_gold 100"
        void ExecuteCommand(string input);
    }
}