using Game.Runtime;
using Game.Runtime.Services;
using System.Collections.Generic;
using Erame.Core;

namespace Tests{
    /// <summary>测试用日志：记录警告与错误，供断言使用（线程安全，存档测试会在后台线程打日志）。</summary>
    public sealed class FakeLogService : ILogService{
        private readonly object _gate = new();
        private readonly List<string> _warnings = new();
        private readonly List<string> _errors = new();

        public LogLevel MinLevel { get; set; } = LogLevel.Debug;

        public IReadOnlyList<string> Warnings{ get{ lock (_gate) return _warnings.ToArray(); } }
        public IReadOnlyList<string> Errors{ get{ lock (_gate) return _errors.ToArray(); } }

        public void Debug(string message){ }
        public void Info(string message){ }

        public void Warn(string message){
            lock (_gate) _warnings.Add(message);
        }

        public void Error(string message){
            lock (_gate) _errors.Add(message);
        }
    }
}
