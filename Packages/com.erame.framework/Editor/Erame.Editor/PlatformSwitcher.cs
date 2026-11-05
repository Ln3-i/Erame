using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Build;
using UnityEngine;

namespace Erame.Editor{
    public static class PlatformSwitcher{

        [MenuItem("Erame/平台切换/Steam")]
        public static void EnableSteam(){
            SetDefineSymbol("USE_STEAM", true);
        }

        [MenuItem("Erame/平台切换/原生模式")]
        public static void DisableSteam(){
            SetDefineSymbol("USE_STEAM", false);
        }

        // 核心逻辑：动态添加或移除宏（按符号精确增删，避免误删包含相同子串的其他宏）
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
