using Game.Runtime;
using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using Erame.Core;

namespace Game.Runtime.Services{
    public sealed class LocalizationService : ILocalizationService, IService{
        private readonly ILogService _log;
        private readonly IEventBus _eventBus;
        
        // 数据结构：语言代码 -> (Key -> 翻译文本)
        private readonly Dictionary<string, Dictionary<string, string>> _dictionaries = new();
        
        public string CurrentLanguage { get; private set; } = "Chinese";

        public LocalizationService(ILogService log, IEventBus eventBus){
            _log = log;
            _eventBus = eventBus;
        }

        public void Initialize(){
            _log.Info("[Localization] 本地化服务初始化");

            // 先加载内置默认文案（保证任何环境都有可用的兜底文本）
            LoadBuiltInDefaults();

            // 再尝试从 StreamingAssets/Localization/{lang}.csv 覆盖
            TryLoadCsvFromStreamingAssets("Chinese");
            TryLoadCsvFromStreamingAssets("English");
            
            _log.Info($"[Localization] 当前语言: {CurrentLanguage}");
        }

        public void Dispose() => _log.Info("[Localization] 本地化服务已销毁");

        /// <summary>
        /// 从 CSV 文本加载语言包。格式为两列：key,value（第一行是表头）。
        /// 也可供外部（如 Addressables 下载后）直接调用。
        /// </summary>
        public void LoadFromCsv(string languageCode, string csvText){
            if (string.IsNullOrWhiteSpace(csvText)) return;

            var dict = new Dictionary<string, string>();
            var lines = csvText.Replace("\r\n", "\n").Split('\n');

            for (int i = 1; i < lines.Length; i++){
                var line = lines[i];
                if (string.IsNullOrWhiteSpace(line)) continue;

                int comma = line.IndexOf(',');
                if (comma <= 0) continue;

                string key = line.Substring(0, comma).Trim();
                string value = line.Substring(comma + 1).Trim();
                if (key.Length > 0) dict[key] = value;
            }

            _dictionaries[languageCode] = dict;
        }

        private void TryLoadCsvFromStreamingAssets(string languageCode){
            string path = Path.Combine(Application.streamingAssetsPath, "Localization", languageCode + ".csv");
            try{
                // 注意：Android 的 StreamingAssets 位于 apk 内，File.Exists 会返回 false，
                // 此时会自动使用内置默认文案；如需支持 Android，请改用 UnityWebRequest 读取。
                if (!File.Exists(path)) return;

                LoadFromCsv(languageCode, File.ReadAllText(path));
                _log.Info($"[Localization] 已从 CSV 加载语言包: {languageCode}");
            }
            catch (Exception ex){
                _log.Warn($"[Localization] 读取语言包失败 {languageCode}: {ex.Message}");
            }
        }

        private void LoadBuiltInDefaults(){
            LoadData("Chinese", new Dictionary<string, string>{
                { "UI_MainMenu_Start", "开始游戏" },
                { "UI_MainMenu_Quit", "退出游戏" },
                { "UI_Player_Gold", "金币" }
            });
            LoadData("English", new Dictionary<string, string>{
                { "UI_MainMenu_Start", "Start Game" },
                { "UI_MainMenu_Quit", "Quit Game" },
                { "UI_Player_Gold", "Gold" }
            });
        }

        private void LoadData(string lang, Dictionary<string, string> dict){
            _dictionaries[lang] = dict;
        }

        public void SetLanguage(string languageCode){
            if (CurrentLanguage == languageCode) return;
            
            if (!_dictionaries.ContainsKey(languageCode)){
                _log.Error($"[Localization] 找不到语言包: {languageCode}");
                return;
            }

            CurrentLanguage = languageCode;
            _log.Info($"[Localization] 切换语言至: {languageCode}");
            
            _eventBus.Publish(new LanguageChangedEvent(languageCode));
        }

        public string GetString(string key){
            if (_dictionaries.TryGetValue(CurrentLanguage, out var dict)){
                if (dict.TryGetValue(key, out var text)){
                    return text;
                }
            }
            
            _log.Warn($"[Localization] 找不到翻译键: {key}");
            return $"#{key}#";
        }
    }
}
