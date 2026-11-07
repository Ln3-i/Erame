using System.Collections.Generic;
using System.IO;
using Erame.Runtime.Config;
using UnityEditor;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>ErameSettings 的创建、校验与维护。</summary>
    public static class ErameSettingsMenu{
        [MenuItem("Erame/示例/生成框架设置", priority = 10)]
        public static void CreateSettings(){
            var instance = ScriptableObject.CreateInstance<ErameSettings>();
            var path = AssetDatabase.GenerateUniqueAssetPath(GetTargetFolder() + "/ErameSettings.asset");
            AssetDatabase.CreateAsset(instance, path);

            var serialized = new SerializedObject(instance);
            var keyProperty = serialized.FindProperty("saveKey");
            if (keyProperty != null){
                keyProperty.stringValue = ErameEditorUtility.GenerateSaveKey();
                serialized.ApplyModifiedPropertiesWithoutUndo();
            }

            EditorUtility.SetDirty(instance);
            AssetDatabase.SaveAssets();

            Selection.activeObject = instance;
            EditorGUIUtility.PingObject(instance);
            Debug.Log($"[Erame] 已创建框架设置: {path}");
        }

        [MenuItem("Erame/配置/校验框架设置", priority = 11)]
        public static void ValidateSettings(){
            var settings = ErameEditorUtility.FindSettings();
            if (settings == null){
                Debug.LogWarning("[Erame] 未找到 ErameSettings 资产，请先执行「Erame/配置/创建框架设置」。");
                return;
            }

            var lines = new List<string>();
            var failed = false;

            lines.Add("资产: " + AssetDatabase.GetAssetPath(settings));
            lines.Add("日志级别: " + settings.LogLevel);
            lines.Add("存档密钥: " + (settings.HasConfiguredSaveKey
                ? settings.ConfiguredSaveKeyByteCount + " 字节"
                : "未配置（将使用开发期密钥）"));

            if (settings.HasConfiguredSaveKey){
                var length = settings.ConfiguredSaveKeyByteCount;
                if (length != 16 && length != 24 && length != 32){
                    lines.Add("[错误] 密钥长度必须是 16/24/32 字节");
                    failed = true;
                }
            }
            else{
                lines.Add("[提示] 发布前请填写固定密钥，避免换机后旧存档无法解密");
            }

            if (settings.LogLevel == Erame.Core.LogLevel.Debug){
                lines.Add("[警告] 日志级别为 Debug，发布版本建议 Info 或更高");
            }

            var prefix = failed ? "[Erame] 配置校验未通过" : "[Erame] 配置校验完成";
            if (failed) Debug.LogError(prefix + "\n" + string.Join("\n", lines));
            else Debug.Log(prefix + "\n" + string.Join("\n", lines));
        }

        [MenuItem("Erame/配置/生成开发期密钥", priority = 12)]
        public static void GenerateDevKey(){
            var settings = ErameEditorUtility.FindSettings();
            if (settings == null){
                Debug.LogWarning("[Erame] 未找到 ErameSettings 资产，请先执行「Erame/配置/创建框架设置」。");
                return;
            }

            var serialized = new SerializedObject(settings);
            var keyProperty = serialized.FindProperty("saveKey");
            if (keyProperty == null){
                Debug.LogError("[Erame] 无法访问存档密钥字段。");
                return;
            }

            keyProperty.stringValue = ErameEditorUtility.GenerateSaveKey();
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(settings);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Erame] 已为 {AssetDatabase.GetAssetPath(settings)} 生成新的 32 字节存档密钥。");
        }

        [MenuItem("Erame/配置/定位当前设置", priority = 13)]
        public static void PingSettings(){
            var settings = ErameEditorUtility.FindSettings();
            if (settings == null){
                Debug.LogWarning("[Erame] 未找到 ErameSettings 资产，请先执行「Erame/配置/创建框架设置」。");
                return;
            }

            Selection.activeObject = settings;
            EditorGUIUtility.PingObject(settings);
        }

        private static string GetTargetFolder(){
            var selectedPath = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(selectedPath)){
                return AssetDatabase.IsValidFolder(selectedPath)
                    ? selectedPath
                    : Path.GetDirectoryName(selectedPath)?.Replace('\\', '/') ?? "Assets";
            }

            if (AssetDatabase.IsValidFolder("Assets/Game/Configs")) return "Assets/Game/Configs";
            if (AssetDatabase.IsValidFolder("Assets/Configs")) return "Assets/Configs";
            return "Assets";
        }
    }
}
