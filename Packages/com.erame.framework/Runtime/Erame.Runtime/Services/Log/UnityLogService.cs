using UnityEngine;
using Erame.Core;

namespace Erame.Runtime.Services{
    // 日志服务的 Unity 实现：最终写到 Unity 控制台。
    // 同时实现 IService，让容器管它的初始化和销毁。
    public sealed class UnityLogService : ILogService, IService{
        private LogLevel _minLevel = LogLevel.Debug;

        public LogLevel MinLevel{
            get => _minLevel;
            set => _minLevel = value;
        }

        public void Initialize(){
            UnityEngine.Debug.Log("[Log] 日志服务初始化完成");
        }

        public void Dispose(){
            UnityEngine.Debug.Log("[Log] 日志服务已销毁");
        }

        public void Debug(string message){
            if (LogLevel.Debug < _minLevel) return;
            UnityEngine.Debug.Log(Format(LogLevel.Debug, message));
        }

        public void Info(string message){
            if (LogLevel.Info < _minLevel) return;
            UnityEngine.Debug.Log(Format(LogLevel.Info, message));
        }

        public void Warn(string message){
            if (LogLevel.Warn < _minLevel) return;
            UnityEngine.Debug.LogWarning(Format(LogLevel.Warn, message));
        }

        public void Error(string message){
            if (LogLevel.Error < _minLevel) return;
            UnityEngine.Debug.LogError(Format(LogLevel.Error, message));
        }

        private static string Format(LogLevel level, string message){
            return $"[{level}] {message}";
        }
    }
}