namespace Erame.Core{
    public interface ILocalizationService{
        // 当前语言代码（如 "Chinese", "English"）
        string CurrentLanguage { get; }
        
        // 切换语言
        void SetLanguage(string languageCode);
        
        // 获取翻译后的文本
        string GetString(string key);
    }
}