using System.IO;
using Erame.Runtime.Config;
using UnityEditor;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>平台配置相关的编辑器工具。</summary>
    public static class EramePlatformMenu{
        [MenuItem("Erame/平台/同步 AppID 到 steam_appid.txt", priority = 30)]
        public static void SyncSteamAppId(){
            var settings = ErameEditorUtility.FindSettings();
            if (settings == null){
                Debug.LogWarning("[Erame] 未找到 ErameSettings 资产，无法同步 Steam AppID。");
                return;
            }

            var path = Path.Combine(ErameEditorUtility.GetProjectRoot(), "steam_appid.txt");
            File.WriteAllText(path, settings.SteamAppId.ToString());
            AssetDatabase.Refresh();

            if (settings.IsDevSteamAppId){
                Debug.LogWarning($"[Erame] 已写入 steam_appid.txt（{settings.SteamAppId}，仍是演示 AppID，发布前请替换）。");
            }
            else{
                Debug.Log($"[Erame] 已写入 steam_appid.txt: {settings.SteamAppId}");
            }
        }
    }
}
