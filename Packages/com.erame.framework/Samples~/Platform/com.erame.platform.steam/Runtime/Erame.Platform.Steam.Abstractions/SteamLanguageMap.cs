using System;
using System.Collections.Generic;

namespace Erame.Platform.Steam{
    /// <summary>
    /// Steam 客户端语言代码（<c>SteamApps.GetCurrentGameLanguage()</c>）与框架语言名的映射。
    /// 框架内置只有 <c>Chinese</c> / <c>English</c> 两套文案，其余语言返回 null，由业务自行扩展。
    /// 纯映射、无 Unity 依赖，便于单测。
    /// </summary>
    public static class SteamLanguageMap{
        private static readonly Dictionary<string, string> ToFramework = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){
            { "english", "English" },
            { "schinese", "Chinese" },
            { "tchinese", "Chinese" },
        };

        private static readonly Dictionary<string, string> ToSteam = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase){
            { "English", "english" },
            { "Chinese", "schinese" },
        };

        /// <summary>Steam 语言代码 → 框架语言名；不认识返回 null。</summary>
        public static string ToFrameworkLanguage(string steamLanguage){
            if (string.IsNullOrEmpty(steamLanguage)) return null;
            return ToFramework.TryGetValue(steamLanguage, out var language) ? language : null;
        }

        /// <summary>框架语言名 → Steam 语言代码；不认识返回 null（用于回写客户端语言）。</summary>
        public static string ToSteamLanguage(string frameworkLanguage){
            if (string.IsNullOrEmpty(frameworkLanguage)) return null;
            return ToSteam.TryGetValue(frameworkLanguage, out var language) ? language : null;
        }

        /// <summary>当前映射表支持的 Steam 语言代码。</summary>
        public static IReadOnlyCollection<string> SupportedSteamLanguages => ToFramework.Keys;
    }
}
