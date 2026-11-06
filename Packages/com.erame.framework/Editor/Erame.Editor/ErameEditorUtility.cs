using System;
using System.Linq;
using System.IO;
using Erame.Core;
using Erame.Runtime.Config;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>Erame 编辑器菜单共用的查找与检查工具。</summary>
    public static class ErameEditorUtility{
        public const string FrameworkPackageName = "com.erame.framework";
        public const string SteamPackageName = "com.erame.platform.steam";
        public const string SteamworksPackageName = "com.rlabrecque.steamworks.net";
        public const string FrameworkPackagePath = "Packages/com.erame.framework";

        /// <summary>优先返回当前选中对象，其次返回工程内第一个 ErameSettings。</summary>
        public static ErameSettings FindSettings(){
            if (Selection.activeObject is ErameSettings selected) return selected;

            foreach (var guid in AssetDatabase.FindAssets("t:ErameSettings")){
                var asset = AssetDatabase.LoadAssetAtPath<ErameSettings>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset != null) return asset;
            }

            return null;
        }

        /// <summary>生成 32 字节（32 个 ASCII 字符）的开发用密钥。</summary>
        public static string GenerateSaveKey(){
            return Guid.NewGuid().ToString("N");
        }

        public static string GetProjectRoot(){
            return Path.GetFullPath(Path.Combine(Application.dataPath, ".."));
        }

        public static string GetPackageVersion(string packageName){
            var info = UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages()
                .FirstOrDefault(p => p.name == packageName);
            return info != null ? info.version : null;
        }

        public static bool IsPackageInstalled(string packageName){
            return UnityEditor.PackageManager.PackageInfo.GetAllRegisteredPackages().Any(p => p.name == packageName);
        }

        public static bool HasDefineSymbol(string symbol){
            var group = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var defines = PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(group));
            return defines.Split(';').Any(d => d.Trim() == symbol);
        }

        /// <summary>当前运行中的服务容器（未运行时为 null）。直接读 ErameRuntime，无反射。</summary>
        public static ServiceContainer GetRuntimeServices(){
            return Erame.Runtime.Bootstrap.ErameRuntime.Services;
        }

        public static string FormatSize(long bytes){
            if (bytes < 1024) return bytes + " B";
            if (bytes < 1024 * 1024) return (bytes / 1024f).ToString("N1") + " KB";
            return (bytes / (1024f * 1024f)).ToString("N1") + " MB";
        }
    }
}
