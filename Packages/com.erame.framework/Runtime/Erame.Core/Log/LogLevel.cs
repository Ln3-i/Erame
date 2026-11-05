namespace Erame.Core{
    public enum LogLevel{
        Debug = 0,   // 开发期详细日志，发布版本关掉
        Info  = 1,   // 正常信息
        Warn  = 2,   // 警告，不致命但要注意
        Error = 3,   // 错误，功能可能已经坏了
        None  = 4    // 关掉所有日志
    }
}