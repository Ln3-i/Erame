using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Erame.Editor{
    /// <summary>框架文档入口与版本更新检查。</summary>
    public static class ErameHelpMenu{
        [MenuItem("Erame/帮助/打开开发手册", priority = 50)]
        public static void OpenHandbook(){
            OpenDoc("开发手册.md");
        }

        [MenuItem("Erame/帮助/打开框架文档", priority = 51)]
        public static void OpenReadme(){
            OpenDoc("README.md");
        }

        [MenuItem("Erame/帮助/打开 API 文档", priority = 52)]
        public static void OpenApi(){
            OpenDoc("API.md");
        }

        [MenuItem("Erame/检查更新", priority = 60)]
        public static void CheckUpdates(){
            var current = ErameEditorUtility.GetPackageVersion(ErameEditorUtility.FrameworkPackageName);
            if (current == null){
                Debug.LogWarning("[Erame] 未检测到 com.erame.framework，无法检查更新。");
                return;
            }

            var root = ErameEditorUtility.GetProjectRoot();
            if (!Directory.Exists(Path.Combine(root, ".git"))){
                Debug.LogWarning("[Erame] 当前工程不是 Git 仓库，无法检查更新。");
                return;
            }

            try{
                var startInfo = new ProcessStartInfo("git", "ls-remote --tags --refs origin"){
                    WorkingDirectory = root,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                using var process = Process.Start(startInfo);
                if (process == null){
                    Debug.LogWarning("[Erame] 无法启动 git，请确认已安装 Git。");
                    return;
                }

                if (!process.WaitForExit(15000)){
                    process.Kill();
                    Debug.LogWarning("[Erame] 读取远端标签超时，请检查网络或远端配置。");
                    return;
                }

                var output = process.StandardOutput.ReadToEnd();
                var error = process.StandardError.ReadToEnd();

                if (process.ExitCode != 0){
                    Debug.LogWarning("[Erame] 无法读取远端标签，请确认已配置 origin。\n" + error.Trim());
                    return;
                }

                var tags = output.Split('\n')
                    .Select(line => line.Trim())
                    .Where(line => line.Contains("refs/tags/"))
                    .Select(line => line.Substring(line.LastIndexOf("refs/tags/", StringComparison.Ordinal) + 10).Trim())
                    .Where(tag => !tag.EndsWith("^{}"))
                    .ToList();

                if (tags.Count == 0){
                    Debug.Log("[Erame] 远端没有版本标签。当前本地版本: " + current);
                    return;
                }

                var currentVersion = ParseVersion(current);
                var latestTag = tags
                    .Select(tag => new { Tag = tag, Version = ParseVersion(tag) })
                    .Where(item => item.Version != null)
                    .OrderByDescending(item => item.Version)
                    .FirstOrDefault();

                if (latestTag == null){
                    Debug.Log("[Erame] 远端标签无法解析为版本号。当前本地版本: " + current);
                    return;
                }

                if (currentVersion != null && latestTag.Version > currentVersion){
                    Debug.Log($"[Erame] 发现新版本: {latestTag.Tag}（本地 {current}），可在 manifest.json 中更新引用标签。");
                }
                else{
                    Debug.Log($"[Erame] 已是最新版本: 本地 {current}，远端 {latestTag.Tag}。");
                }
            }
            catch (Exception ex){
                Debug.LogWarning("[Erame] 检查更新失败: " + ex.Message);
            }
        }

        private static void OpenDoc(string fileName){
            var path = Path.GetFullPath(Path.Combine("Packages/com.erame.framework", fileName));
            if (!File.Exists(path)){
                Debug.LogError("[Erame] 找不到文档: " + path);
                return;
            }

            EditorUtility.OpenWithDefaultApp(path);
        }

        private static Version ParseVersion(string tag){
            return Version.TryParse(tag.TrimStart('v', 'V'), out var version) ? version : null;
        }
    }
}
