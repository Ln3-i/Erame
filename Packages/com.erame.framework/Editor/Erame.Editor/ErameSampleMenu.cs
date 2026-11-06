using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace Erame.Editor{
    /// <summary>
    /// 生成业务层示例：与当前工程一致的文件架构 + 一个标准入口（GameEntry）。
    /// 供其他开发者接入框架后作为项目起点。
    /// </summary>
    public static class ErameSampleMenu{
        // 与 Assets/Game/Scripts 保持一致的业务层目录架构
        private static readonly string[] ScriptFolders = { "App", "Bootstrap", "Config", "Gameplay", "Input", "Modules", "Simulation" };

        private const string EntryTemplate =
@"using Erame.Core;
using Erame.Runtime.Bootstrap;
using Erame.Runtime.Config;

namespace __NAMESPACE__.Scripts.App{
    /// <summary>
    /// 标准入口：整个项目只需重写这里即可启动。
    /// Configure 负责注册模块与服务，OnStarted 负责启动完成后的收尾。
    /// </summary>
    public sealed class GameEntry : ErameEntry{
        protected override void Configure(ServiceContainer container, ErameSettings settings){
            // 1. 框架默认装配：日志/事件/时间/资源/场景 + 输入/UI/池/音频/本地化/流程/存档 + 平台
            ErameInstaller.InstallFramework(container, settings);
            ErameInstaller.InstallGameplay(container, settings, null);   // 如需输入源：_ => new MyInputAdapter()
            ErameInstaller.InstallPlatform(container, settings);

            // 2. 业务自定义服务在此追加：
            // container.RegisterSingleton<IMyService>(p => new MyService(...));

            // 3. 也可以改用模块分组注册：
            // new FrameworkModule(settings).Configure(container);
        }

        protected override void OnStarted(){
            // 启动完成后的业务初始化，例如注册游戏流程状态：
            // FlowRegistry.RegisterAll(Get<IGameStateMachine>());
        }
    }
}
";

        private const string BootstrapTemplate =
@"using __NAMESPACE__.Scripts.App;
using Erame.Runtime.Config;
using UnityEngine;

namespace __NAMESPACE__.Scripts.Bootstrap{
    /// <summary>
    /// 启动组件：挂在场景中的 GameObject 上即可运行整个项目。
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour{
        [Tooltip(""可留空，留空时使用框架默认配置"")]
        [SerializeField] private ErameSettings settings;

        private readonly GameEntry _entry = new GameEntry();

        private void Awake(){
            DontDestroyOnLoad(gameObject);
        }

        private void Start(){
            _entry.Start(settings);
        }

        private void Update(){
            _entry.Update();
        }

        private void OnDestroy(){
            _entry.Quit();
        }
    }
}
";

        [MenuItem("Erame/快速开始/创建业务层示例", priority = 15)]
        public static void CreateSample(){
            var parent = ResolveTargetFolder();
            var gameFolder = AssetDatabase.GenerateUniqueAssetPath(parent + "/Game");
            var gameName = Path.GetFileName(gameFolder);

            // 创建目录架构：Game/Scripts/{App,Bootstrap,Config,Gameplay,Input,Modules,Simulation}
            CreateFolder(parent, gameName);
            var scriptsPath = gameFolder + "/Scripts";
            CreateFolder(gameFolder, "Scripts");
            foreach (var folder in ScriptFolders){
                CreateFolder(scriptsPath, folder);
            }

            var ns = ToNamespace(gameName);
            var entryPath = scriptsPath + "/App/GameEntry.cs";
            var bootstrapPath = scriptsPath + "/Bootstrap/GameBootstrap.cs";

            File.WriteAllText(entryPath, EntryTemplate.Replace("__NAMESPACE__", ns));
            File.WriteAllText(bootstrapPath, BootstrapTemplate.Replace("__NAMESPACE__", ns));
            AssetDatabase.Refresh();

            var entry = AssetDatabase.LoadAssetAtPath<MonoScript>(entryPath);
            if (entry != null){
                Selection.activeObject = entry;
                EditorGUIUtility.PingObject(entry);
            }

            Debug.Log(
                "[Erame] 已创建业务层示例:\n" +
                $"  {gameFolder}/Scripts/...  （{ScriptFolders.Length} 个业务目录）\n" +
                $"  标准入口: {entryPath}\n" +
                "  下一步：把 GameBootstrap 挂到场景对象上，并通过 Erame/配置/创建框架设置 创建配置。");
        }

        private static string ResolveTargetFolder(){
            var path = AssetDatabase.GetAssetPath(Selection.activeObject);
            if (!string.IsNullOrEmpty(path)){
                if (!AssetDatabase.IsValidFolder(path)){
                    path = Path.GetDirectoryName(path)?.Replace('\\', '/');
                }

                if (!string.IsNullOrEmpty(path) && AssetDatabase.IsValidFolder(path)) return path;
            }

            return "Assets";
        }

        private static void CreateFolder(string parent, string name){
            if (AssetDatabase.IsValidFolder(parent + "/" + name)) return;
            AssetDatabase.CreateFolder(parent, name);
        }

        private static string ToNamespace(string folderName){
            var clean = Regex.Replace(folderName, "[^A-Za-z0-9_]", "");
            if (string.IsNullOrEmpty(clean)) return "MyGame";
            if (char.IsDigit(clean[0])) clean = "_" + clean;
            return clean;
        }
    }
}
