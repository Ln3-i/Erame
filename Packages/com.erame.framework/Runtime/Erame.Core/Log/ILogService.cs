namespace Erame.Core{
    public interface ILogService{
        LogLevel MinLevel { get; set; }

        void Debug(string message);
        void Info(string message);
        void Warn(string message);
        void Error(string message);
    }
}