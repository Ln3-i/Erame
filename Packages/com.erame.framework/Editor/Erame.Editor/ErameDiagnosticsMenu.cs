using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Erame.Core;
using Erame.Runtime.Config;
using UnityEditor;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>框架自检、运行时诊断与存档检查。</summary>
    public static class ErameDiagnosticsMenu{
        private static readonly string[] SaveExtensions = { ".sav", ".save", ".dat", ".bak" };

        [MenuItem("Erame/诊断/框架自检", priority = 20)]
        public static void SelfCheck(){
            var lines = new List<string>();
            var failed = false;

            AddPackageLine(lines, "Addressables", "com.unity.addressables", true, ref failed);
            AddPackageLine(lines, "Input System", "com.unity.inputsystem", true, ref failed);
            AddPackageLine(lines, "TextMeshPro", "com.unity.textmeshpro", false, ref failed);
            AddPackageLine(lines, "Steamworks", ErameEditorUtility.SteamworksPackageName, false, ref failed);

            var frameworkVersion = ErameEditorUtility.GetPackageVersion(ErameEditorUtility.FrameworkPackageName);
            lines.Add(frameworkVersion != null
                ? $"[OK] 框架包: {frameworkVersion}"
                : "[FAIL] 未检测到 com.erame.framework");

            var steamPackage = ErameEditorUtility.GetPackageVersion(ErameEditorUtility.SteamPackageName);
            lines.Add(steamPackage != null
                ? $"[OK] Steam 平台包: {steamPackage}"
                : "[提示] 未安装 com.erame.platform.steam（可选）");

            if (File.Exists("Assets/AddressableAssetsData/AddressableAssetSettings.asset"))
                lines.Add("[OK] Addressables 配置文件存在");
            else
                lines.Add("[警告] 未找到 AddressableAssetsData，资源服务可能无法加载");

            var settings = ErameEditorUtility.FindSettings();
            if (settings == null){
                lines.Add("[警告] 未找到 ErameSettings 资产");
            }
            else{
                var keyOk = settings.HasConfiguredSaveKey
                    && (settings.ConfiguredSaveKeyByteCount == 16
                        || settings.ConfiguredSaveKeyByteCount == 24
                        || settings.ConfiguredSaveKeyByteCount == 32);
                lines.Add(keyOk
                    ? "[OK] 存档密钥已配置"
                    : "[警告] 存档密钥未配置或长度非法");
            }

            lines.Add(CheckSaveDirectory());

            var hasSteamDefine = ErameEditorUtility.HasDefineSymbol("USE_STEAM");
            lines.Add(hasSteamDefine ? "[OK] USE_STEAM 已开启" : "[提示] 当前为原生平台模式");

            var prefix = failed ? "[Erame] 框架自检发现问题" : "[Erame] 框架自检完成";
            if (failed) Debug.LogError(prefix + "\n" + string.Join("\n", lines));
            else Debug.Log(prefix + "\n" + string.Join("\n", lines));
        }

        [MenuItem("Erame/测试/容器快照", priority = 21)]
        public static void DumpContainer(){
            if (!EditorApplication.isPlaying){
                Debug.LogWarning("[Erame] 容器快照需要进入 Play 模式后使用。");
                return;
            }

            var container = ErameEditorUtility.GetRuntimeServices();
            if (container == null){
                Debug.LogWarning("[Erame] 未找到运行中的服务容器，请确认场景中已挂载 ErameMonoEntry 入口。");
                return;
            }

            var lines = new List<string>();
            lines.Add($"状态: Built={container.IsBuilt}, Disposed={container.IsDisposed}");

            var registered = container.GetRegisteredServiceTypes();
            lines.Add($"注册服务 ({registered.Count}): " + string.Join(", ", registered.Select(t => t.Name)));

            var creationOrder = container.GetCreationOrder();
            lines.Add($"初始化顺序 ({creationOrder.Count}): " + string.Join(" -> ", creationOrder.Select(t => t.Name)));

            Debug.Log("[Erame] 服务容器快照\n" + string.Join("\n", lines));
        }

        [MenuItem("Erame/测试/事件统计", priority = 22)]
        public static void DumpSubscriptions(){
            if (!EditorApplication.isPlaying){
                Debug.LogWarning("[Erame] 事件统计需要进入 Play 模式后使用。");
                return;
            }

            var container = ErameEditorUtility.GetRuntimeServices();
            if (container == null){
                Debug.LogWarning("[Erame] 未找到运行中的服务容器，请确认场景中已挂载 ErameMonoEntry 入口。");
                return;
            }

            if (!container.TryGet<IEventBus>(out var bus)){
                Debug.LogWarning("[Erame] 容器中没有注册 IEventBus。");
                return;
            }

            if (bus is not EventBus concrete){
                Debug.LogWarning("[Erame] 当前 IEventBus 实现不支持订阅统计。");
                return;
            }

            var counts = concrete.GetSubscriptionCounts();
            if (counts.Count == 0){
                Debug.Log("[Erame] 当前没有任何事件订阅。");
                return;
            }

            var lines = counts.Select(c => $"{c.Key.Name}: {c.Value} 个订阅者").ToList();
            Debug.Log($"[Erame] 事件订阅统计（{counts.Count} 种事件）\n" + string.Join("\n", lines));
        }

        [MenuItem("Erame/诊断/检查存档", priority = 23)]
        public static void CheckSaves(){
            var directory = Application.persistentDataPath;
            var lines = new List<string> { "目录: " + directory };

            if (!Directory.Exists(directory)){
                lines.Add("[警告] 存档目录不存在");
                Debug.LogWarning("[Erame] 存档检查\n" + string.Join("\n", lines));
                return;
            }

            var files = Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories)
                .Where(f => SaveExtensions.Contains(Path.GetExtension(f).ToLowerInvariant()))
                .ToList();

            if (files.Count == 0){
                lines.Add("未发现存档文件");
            }
            else{
                lines.Add($"发现 {files.Count} 个存档相关文件:");
                foreach (var file in files){
                    var info = new FileInfo(file);
                    lines.Add($"  {info.Name} ({ErameEditorUtility.FormatSize(info.Length)}, {info.LastWriteTime:yyyy-MM-dd HH:mm})");
                }
            }

            Debug.Log("[Erame] 存档检查\n" + string.Join("\n", lines));
        }

        private static void AddPackageLine(List<string> lines, string label, string packageName, bool required, ref bool failed){
            var version = ErameEditorUtility.GetPackageVersion(packageName);
            if (version != null){
                lines.Add($"[OK] {label}: {version}");
                return;
            }

            lines.Add(required ? $"[FAIL] 缺少 {label} 包" : $"[提示] 未安装 {label}（可选）");
            if (required) failed = true;
        }

        private static string CheckSaveDirectory(){
            var directory = Application.persistentDataPath;
            try{
                Directory.CreateDirectory(directory);
                var probe = Path.Combine(directory, ".erame_write_test");
                File.WriteAllText(probe, "ok");
                File.Delete(probe);
                return "[OK] 存档目录可写";
            }
            catch (Exception ex){
                return "[警告] 存档目录不可写: " + ex.Message;
            }
        }
    }
}
