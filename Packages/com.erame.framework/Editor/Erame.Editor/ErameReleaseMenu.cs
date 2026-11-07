using System.Collections.Generic;
using Erame.Core;
using Erame.Runtime.Bootstrap;
using UnityEditor;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>发布前的配置检查，避免把演示配置带进正式包。</summary>
    public static class ErameReleaseMenu{
        [MenuItem("Erame/构建/发布前检查", priority = 40)]
        public static void PreflightCheck(){
            var errors = new List<string>();
            var warnings = new List<string>();

            var settings = ErameEditorUtility.FindSettings();
            if (settings == null){
                errors.Add("未找到 ErameSettings 资产");
            }
            else{
                if (!settings.HasConfiguredSaveKey){
                    errors.Add("存档密钥未配置（发布版本不能依赖 PlayerPrefs 开发期密钥）");
                }
                else{
                    var length = settings.ConfiguredSaveKeyByteCount;
                    if (length != 16 && length != 24 && length != 32){
                        errors.Add($"存档密钥长度为 {length} 字节，必须是 16/24/32");
                    }
                }

                if (settings.LogLevel == LogLevel.Debug) warnings.Add("日志级别为 Debug，建议发布时使用 Info 或更高");

                // 上面对资产的校验只有在"入口引用了该资产"时才真正进包：
                // 入口没引用的话，运行时读到的是空实例（开发期密钥）
                var entry = Object.FindObjectOfType<ErameMonoEntry>();
                if (entry == null){
                    warnings.Add("当前场景未挂载 ErameMonoEntry 入口，无法确认设置资产被引用");
                }
                else if (entry.Settings == null){
                    errors.Add("入口组件未引用 ErameSettings 资产：包体将使用开发期密钥");
                }
                else if (entry.Settings != settings){
                    warnings.Add($"入口引用的是 {entry.Settings.name}，与校验的 {settings.name} 不是同一份资产");
                }
            }

            foreach (var error in errors) Debug.LogError("[Erame 发布检查] " + error);
            foreach (var warning in warnings) Debug.LogWarning("[Erame 发布检查] " + warning);

            var summary = errors.Count == 0
                ? (warnings.Count == 0
                    ? "发布前检查通过。"
                    : $"检查通过，但有 {warnings.Count} 条警告，详见 Console。")
                : $"发现 {errors.Count} 个错误、{warnings.Count} 条警告，详见 Console。";

            EditorUtility.DisplayDialog("Erame 发布前检查", summary, "知道了");
        }
    }
}
