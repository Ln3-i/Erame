using System.Collections.Generic;
using Erame.Core;
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

                if (settings.IsDevSteamAppId) errors.Add("Steam AppID 仍为演示值 480");
                if (settings.LogLevel == LogLevel.Debug) warnings.Add("日志级别为 Debug，建议发布时使用 Info 或更高");
            }

            var hasSteamDefine = ErameEditorUtility.HasDefineSymbol("USE_STEAM");
            var hasSteamworks = ErameEditorUtility.IsPackageInstalled(ErameEditorUtility.SteamworksPackageName);

            if (hasSteamDefine && !hasSteamworks) errors.Add("已开启 USE_STEAM，但未安装 Steamworks.NET");
            if (!hasSteamDefine && hasSteamworks) warnings.Add("已安装 Steamworks.NET，但未开启 USE_STEAM");
            if (hasSteamDefine && !ErameEditorUtility.IsPackageInstalled(ErameEditorUtility.SteamPackageName)){
                errors.Add("已开启 USE_STEAM，但未安装 com.erame.platform.steam");
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
