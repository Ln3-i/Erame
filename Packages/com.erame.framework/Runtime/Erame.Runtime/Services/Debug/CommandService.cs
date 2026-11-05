using System;
using System.Collections.Generic;
using Erame.Core;

namespace Erame.Runtime.Services{
    public class CommandService : ICommandService, IService{
        private readonly ILogService _log;
        private readonly Dictionary<string, IDebugCommand> _commands = new();

        public CommandService(ILogService log){ _log = log; }

        public void Initialize() => _log.Info("[Command] 调试指令服务初始化");
        public void Dispose() => _commands.Clear();

        public void RegisterCommand(IDebugCommand command){
            if (command == null || string.IsNullOrEmpty(command.Name)) return;
            
            // ★ 核心修复：注册时统一转小写，防止大小写不匹配
            string key = command.Name.ToLowerInvariant();
            
            if (_commands.ContainsKey(key)){
                _log.Warn($"[Command] 指令重复注册: {key}");
                return;
            }
            _commands[key] = command;
        }

        public void ExecuteCommand(string input){
            if (string.IsNullOrWhiteSpace(input)) return;

            // 用空格切分指令和参数
            var parts = input.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            string cmdName = parts[0].ToLowerInvariant();
            
            // 把剩下的部分作为参数
            string[] args = new string[parts.Length - 1];
            Array.Copy(parts, 1, args, 0, args.Length);

            // 查找并执行
            if (_commands.TryGetValue(cmdName, out var command)){
                try{
                    command.Execute(args);
                    _log.Info($"[Command] 执行成功: {input}");
                }
                catch (Exception ex){
                    _log.Error($"[Command] 执行出错: {ex.Message}");
                }
            }
            else if (cmdName == "help"){
                foreach (var cmd in _commands.Values){
                    _log.Info($"[Help] {cmd.Name} - {cmd.Description}");
                }
            }
            else{
                _log.Warn($"[Command] 未知指令: {cmdName}");
            }
        }
    }
}