using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Erame.Core;
using Erame.Runtime.Bootstrap;
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

            var frameworkVersion = ErameEditorUtility.GetPackageVersion(ErameEditorUtility.FrameworkPackageName);
            lines.Add(frameworkVersion != null
                ? $"[OK] 框架包: {frameworkVersion}"
                : "[FAIL] 未检测到 com.erame.framework");

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

            // 运行时读的是入口引用的设置资产，不是工程里"存在"的资产：
            // 入口没引用时，上面校验的密钥/日志级别都不会真正生效
            // （本文件同时 using System 与 UnityEngine，Object 必须全限定避免 CS0104）
            var entry = UnityEngine.Object.FindObjectOfType<ErameMonoEntry>();
            if (entry == null){
                lines.Add("[提示] 当前场景未挂载 ErameMonoEntry 入口");
            }
            else if (entry.Settings == null){
                lines.Add("[警告] 入口未引用 ErameSettings 资产：运行时配置（密钥/日志级别）不会生效");
            }
            else{
                lines.Add($"[OK] 入口已引用设置资产: {entry.Settings.name}");
            }

            lines.Add(CheckSaveDirectory());

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
            lines.Add($"注册服务 ({registered.Count})：");
            foreach (var type in registered){
                var source = container.GetRegistrationSource(type) ?? "未标记";
                lines.Add($"  {type.Name} ← {source}");
            }

            var creationOrder = container.GetCreationOrder();
            lines.Add($"初始化顺序 ({creationOrder.Count}): " + string.Join(" -> ", creationOrder.Select(t => t.Name)));
            lines.Add("提示：同一实例以「具体类型 + 接口视图」注册时会各占一行（如 FileSaveService / ISaveService / ISaveSlotService），初始化与释放按实例去重，只执行一次。");

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

            // 底座不提供事件契约：这里用反射找「恰好提供 GetSubscriptionCounts()」的服务，
            // 因此业务用哪种事件总线（或者干脆没有事件总线）都能工作。
            var counts = new List<KeyValuePair<Type, int>>();
            var found = false;
            foreach (var service in container.GetAll<object>()){
                if (service == null) continue;

                var method = service.GetType().GetMethod("GetSubscriptionCounts", Type.EmptyTypes);
                if (method == null || method.GetParameters().Length != 0) continue;

                try{
                    if (method.Invoke(service, null) is IReadOnlyList<KeyValuePair<Type, int>> typed){
                        counts.AddRange(typed);
                        found = true;
                        break; // 同时存在多个实现时只看第一个
                    }
                }
                catch (Exception ex){
                    Debug.LogWarning($"[Erame] 读取事件统计失败（{service.GetType().Name}）: {ex.Message}");
                }
            }

            if (!found){
                Debug.LogWarning("[Erame] 容器中没有支持诊断的事件总线（找不到 GetSubscriptionCounts）。");
                return;
            }

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
