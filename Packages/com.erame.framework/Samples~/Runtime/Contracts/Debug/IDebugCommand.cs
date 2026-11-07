namespace __NS__.Runtime{
    public interface IDebugCommand{
        string Name { get; }
        string Description { get; }
        void Execute(string[] args);
    }
}