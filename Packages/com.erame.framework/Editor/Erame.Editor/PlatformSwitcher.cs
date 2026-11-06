using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>平台选择：无平台 / Steam 可切换；Epic、Wegame 暂未实现，置灰。</summary>
    public static class PlatformSwitcher{
        private const string SteamDefine = "USE_STEAM";

        [MenuItem("Erame/平台/无平台", priority = 30)]
        public static void DisableSteam(){
            SetDefineSymbol(SteamDefine, false);
        }

        [MenuItem("Erame/平台/Steam", priority = 31)]
        public static void EnableSteam(){
            SetDefineSymbol(SteamDefine, true);
        }

        [MenuItem("Erame/平台/Epic", priority = 32)]
        public static void EnableEpic(){
            // 未实现：仅占位
        }

        [MenuItem("Erame/平台/Epic", true)]
        private static bool EnableEpicValidate(){
            return false;   // 置灰
        }

        [MenuItem("Erame/平台/Wegame", priority = 33)]
        public static void EnableWegame(){
            // 未实现：仅占位
        }

        [MenuItem("Erame/平台/Wegame", true)]
        private static bool EnableWegameValidate(){
            return false;   // 置灰
        }

        // 按符号精确增删，避免误删包含相同子串的其他宏
        private static void SetDefineSymbol(string symbol, bool enable){
            var buildTargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var namedTarget = NamedBuildTarget.FromBuildTargetGroup(buildTargetGroup);

            string defines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
            var symbols = new List<string>(defines.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

            bool hasSymbol = symbols.Contains(symbol);
            if (enable && !hasSymbol){
                symbols.Add(symbol);
            }
            else if (!enable && hasSymbol){
                symbols.Remove(symbol);
            }
            else{
                return;
            }

            PlayerSettings.SetScriptingDefineSymbols(namedTarget, string.Join(";", symbols));
            Debug.Log($"[Erame] 已{(enable ? "开启" : "关闭")} {symbol}，等待脚本重新编译。");
        }
    }
}
