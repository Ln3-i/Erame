namespace Erame.Core{
    public interface IInputContext{
        int Priority { get; }
        
        bool IsBlocking { get; }
        
        InputFrame GetInputFrame();
    }
}