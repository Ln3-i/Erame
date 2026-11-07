using System;
using System.Collections.Generic;
using System.Linq;

namespace Erame.Editor{
    /// <summary>
    /// 一项可勾选的能力：声明它需要哪些契约/实现文件、写进哪个模块的注册语句、以及依赖的其他能力。
    /// 生成器只生成被勾选的能力 —— 不需要的能力不必先生成再删除。
    /// </summary>
    internal sealed class ErameCapability{
        public string Id;
        public string Title;
        public string Description;

        /// <summary>写进哪个模块（见 <see cref="ErameCapabilityGroup.Id"/>）。一个模块可以容纳多项能力。</summary>
        public string Group;

        /// <summary>依赖的能力 Id；勾选本项时会自动带上。</summary>
        public string[] DependsOn = Array.Empty<string>();

        /// <summary>Samples~/Runtime 下的相对路径，目录以 '/' 结尾，文件写全名。</summary>
        public string[] Paths = Array.Empty<string>();

        /// <summary>写进模块 Configure 的注册语句（一行或多行）。</summary>
        public string[] Registrations = Array.Empty<string>();

        /// <summary>基础设施：始终生成，不允许取消。</summary>
        public bool AlwaysOn;
    }

    /// <summary>
    /// 一个能力组 = 一个模块文件。<see cref="ErameCapability.Group"/> 指向这里。
    /// 拆组的目的是"删能力 = 删文件"：不再需要的领域直接删掉对应的 *Module.cs 和入口里的一行 AddModule。
    /// </summary>
    internal sealed class ErameCapabilityGroup{
        public string Id;
        public string ClassName;
        public string Summary;
    }

    /// <summary>能力目录。新增服务时在这里加一项即可，生成器与窗口会自动列出。</summary>
    internal static class ErameCapabilities{
        private const string Logging = "logging";
        private const string Event = "event";
        private const string Time = "time";
        private const string Tick = "tick";
        private const string Gate = "gate";
        private const string Input = "input";
        private const string Ui = "ui";
        private const string Pool = "pool";
        private const string Audio = "audio";
        private const string Localization = "localization";
        private const string Save = "save";
        private const string Data = "data";
        private const string Flow = "flow";
        private const string Command = "command";
        private const string Session = "session";
        private const string Asset = "asset";
        private const string Scene = "scene";

        /// <summary>
        /// 模块清单，顺序就是入口里 AddModule 的顺序（也是注册顺序）。
        /// 一个组的模块文件里只写本组被勾选能力的注册行。
        /// </summary>
        public static readonly ErameCapabilityGroup[] Groups = {
            new ErameCapabilityGroup{ Id = "log", ClassName = "LogModule", Summary = "日志：ILogService 与控制台输出。" },
            new ErameCapabilityGroup{ Id = "events", ClassName = "EventsModule", Summary = "事件总线：IEventBus。" },
            new ErameCapabilityGroup{ Id = "time", ClassName = "TimeModule", Summary = "时间与逻辑 Tick。" },
            new ErameCapabilityGroup{ Id = "assets", ClassName = "AssetsModule", Summary = "资源加载/实例化与场景。" },
            new ErameCapabilityGroup{ Id = "input", ClassName = "InputModule", Summary = "输入门闸与输入服务。" },
            new ErameCapabilityGroup{ Id = "ui", ClassName = "UIModule", Summary = "UI 面板栈。" },
            new ErameCapabilityGroup{ Id = "pool", ClassName = "PoolModule", Summary = "对象池。" },
            new ErameCapabilityGroup{ Id = "audio", ClassName = "AudioModule", Summary = "音频。" },
            new ErameCapabilityGroup{ Id = "localization", ClassName = "LocalizationModule", Summary = "本地化。" },
            new ErameCapabilityGroup{ Id = "save", ClassName = "SaveModule", Summary = "存档与存档槽位。" },
            new ErameCapabilityGroup{ Id = "data", ClassName = "DataModule", Summary = "数据表。" },
            new ErameCapabilityGroup{ Id = "flow", ClassName = "FlowModule", Summary = "流程状态机。" },
            new ErameCapabilityGroup{ Id = "command", ClassName = "CommandModule", Summary = "控制台命令。" },
            new ErameCapabilityGroup{ Id = "session", ClassName = "SessionModule", Summary = "会话。" }
        };

        public static readonly ErameCapability[] All = {
            new ErameCapability{
                Id = Logging, Title = "日志", Group = "log", AlwaysOn = true,
                Description = "ILogService + UnityLogService，所有服务都依赖它。",
                Paths = new[]{ "Contracts/Log/", "Services/Log/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<ILogService>(_ => new UnityLogService{ MinLevel = settings.LogLevel });"
                }
            },
            new ErameCapability{
                Id = Event, Title = "事件总线", Group = "events",
                Description = "IEventBus + EventBus；UI、本地化、存档、会话都依赖它。",
                DependsOn = new[]{ Logging },
                Paths = new[]{ "Contracts/Event/", "Services/Event/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<IEventBus>(p => new EventBus(p.Get<ILogService>()));"
                }
            },
            new ErameCapability{
                Id = Time, Title = "时间", Group = "time",
                Description = "ITimeService + UnityTimeService（真实时间/缩放时间）。",
                DependsOn = new[]{ Logging },
                Paths = new[]{ "Contracts/Time/ITimeService.cs", "Services/Time/UnityTimeService.cs" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<ITimeService>(p => new UnityTimeService(p.Get<ILogService>()));"
                }
            },
            new ErameCapability{
                Id = Tick, Title = "逻辑 Tick", Group = "time",
                Description = "ITickService + TickService，固定步长驱动模拟，支持暂停与变速。",
                DependsOn = new[]{ Logging },
                Paths = new[]{ "Contracts/Time/ITickService.cs", "Services/Time/TickService.cs" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<ITickService>(p => new TickService(p.Get<ILogService>()));"
                }
            },
            new ErameCapability{
                Id = Gate, Title = "输入门闸", Group = "input",
                Description = "IInputGate + InputGate；与具体输入方案无关，UI/对话/过场共享。",
                DependsOn = new[]{ Logging },
                Paths = new[]{ "Contracts/Input/IInputGate.cs", "Services/Input/InputGate.cs" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<IInputGate>(p => new InputGate(p.Get<ILogService>()));"
                }
            },
            new ErameCapability{
                Id = Input, Title = "输入", Group = "input",
                Description = "IInputService<TFrame> + InputService；默认帧结构是 InputFrame，可换成自己的结构。",
                DependsOn = new[]{ Logging, Gate },
                Paths = new[]{ "Contracts/Input/", "Services/Input/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<IInputService>(p => new InputService(p.Get<ILogService>(), p.Get<IInputGate>()));"
                }
            },
            new ErameCapability{
                Id = Ui, Title = "UI 面板栈", Group = "ui",
                Description = "IUIService + UIService；带参打开、面板栈、打开时自动屏蔽输入。",
                // UIService 的旧构造重载要 IInputService / UIInputContext，所以把输入一并带上
                DependsOn = new[]{ Logging, Gate, Input },
                Paths = new[]{ "Contracts/UI/", "Services/UI/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<IUIService>(p => new UIService(p.Get<ILogService>(), p.Get<IInputGate>()));"
                }
            },
            new ErameCapability{
                Id = Pool, Title = "对象池", Group = "pool",
                Description = "IPoolService + UnityPoolService。",
                DependsOn = new[]{ Logging },
                Paths = new[]{ "Contracts/Pool/", "Services/Pool/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<IPoolService>(p => new UnityPoolService(p.Get<ILogService>()));"
                }
            },
            new ErameCapability{
                Id = Audio, Title = "音频", Group = "audio",
                Description = "IAudioService + UnityAudioService；依赖资源、对象池与时间。",
                DependsOn = new[]{ Logging, Pool, Asset, Time },
                Paths = new[]{ "Contracts/Audio/", "Services/Audio/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<IAudioService>(p => new UnityAudioService(\n" +
                    "                p.Get<ILogService>(),\n" +
                    "                p.Get<IPoolService>(),\n" +
                    "                p.Get<IAssetService>(),\n" +
                    "                p.Get<ITimeService>()\n" +
                    "            ));"
                }
            },
            new ErameCapability{
                Id = Localization, Title = "本地化", Group = "localization",
                Description = "ILocalizationService + LocalizationService；切语言发事件，缺失 key 返回 #key#。",
                DependsOn = new[]{ Logging, Event },
                Paths = new[]{ "Contracts/Localization/", "Services/Localization/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<ILocalizationService>(p => new LocalizationService(p.Get<ILogService>(), p.Get<IEventBus>()));"
                }
            },
            new ErameCapability{
                Id = Save, Title = "存档", Group = "save",
                Description = "存档 + 槽位 + AES-GCM 加密；一个实例同时暴露 ISaveService / ISaveSlotService。",
                DependsOn = new[]{ Logging, Event },
                Paths = new[]{ "Contracts/Save/", "Services/Save/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<ISaveCrypto>(_ => new AesGcmSaveCrypto(settings.GetSaveKey()));",
                    "registrar.RegisterSingleton<FileSaveService>(p => new FileSaveService(\n" +
                    "                p.Get<ILogService>(),\n" +
                    "                p.Get<IEventBus>(),\n" +
                    "                p.Get<ISaveCrypto>()\n" +
                    "            ));",
                    "registrar.RegisterSingleton<ISaveService>(p => p.Get<FileSaveService>());",
                    "registrar.RegisterSingleton<ISaveSlotService>(p => p.Get<FileSaveService>());"
                }
            },
            new ErameCapability{
                Id = Data, Title = "数据表", Group = "data",
                Description = "IDataRegistry + CsvParser + DataRegistryService。",
                DependsOn = new[]{ Logging },
                Paths = new[]{ "Contracts/Data/", "Services/Data/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<IDataRegistry>(p => new DataRegistryService(p.Get<ILogService>()));"
                }
            },
            new ErameCapability{
                Id = Flow, Title = "流程状态机", Group = "flow",
                Description = "IGameStateMachine + GameStateMachine。",
                DependsOn = new[]{ Logging },
                Paths = new[]{ "Contracts/StateMachine/", "Services/Flow/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<IGameStateMachine>(p => new GameStateMachine(p.Get<ILogService>()));"
                }
            },
            new ErameCapability{
                Id = Command, Title = "控制台命令", Group = "command",
                Description = "ICommandService + CommandService，调试命令注册与执行。",
                DependsOn = new[]{ Logging },
                Paths = new[]{ "Contracts/Debug/", "Services/Debug/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<ICommandService>(p => new CommandService(p.Get<ILogService>()));"
                }
            },
            new ErameCapability{
                Id = Session, Title = "会话", Group = "session",
                Description = "ISessionService + SessionService（开局/房间的占位实现，可按项目替换）。",
                DependsOn = new[]{ Logging, Event },
                Paths = new[]{ "Contracts/Session/", "Services/Session/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<ISessionService>(p => new SessionService(p.Get<ILogService>(), p.Get<IEventBus>()));"
                }
            },
            new ErameCapability{
                Id = Asset, Title = "资源（Addressables）", Group = "assets",
                Description = "IAssetService + IInstantiateService + AddressableAssetService；加载与实例化共用引用计数。",
                DependsOn = new[]{ Logging },
                Paths = new[]{ "Contracts/Asset/", "Services/Asset/" },
                Registrations = new[]{
                    "// 同一实例同时暴露「资源加载」与「资源实例化」两个能力",
                    "registrar.RegisterSingleton<AddressableAssetService>(p => new AddressableAssetService(p.Get<ILogService>()));",
                    "registrar.RegisterSingleton<IAssetService>(p => p.Get<AddressableAssetService>());",
                    "registrar.RegisterSingleton<IInstantiateService>(p => p.Get<AddressableAssetService>());"
                }
            },
            new ErameCapability{
                Id = Scene, Title = "场景", Group = "assets",
                Description = "ISceneService + UnitySceneService；单场景切换与叠加加载，依赖资源作用域。",
                DependsOn = new[]{ Logging, Asset },
                Paths = new[]{ "Contracts/Scene/", "Services/Scene/" },
                Registrations = new[]{
                    "registrar.RegisterSingleton<ISceneService>(p => new UnitySceneService(p.Get<ILogService>(), p.Get<IAssetService>()));"
                }
            }
        };

        /// <summary>最小可运行集：日志 + 事件 + 时间 + Tick + 门闸。</summary>
        public static IReadOnlyCollection<string> Minimal => new[]{ Logging, Event, Time, Tick, Gate };

        public static IReadOnlyCollection<string> AllIds => All.Select(c => c.Id).ToArray();

        public static ErameCapability Find(string id) => All.FirstOrDefault(c => c.Id == id);

        public static ErameCapabilityGroup FindGroup(string id) => Groups.FirstOrDefault(g => g.Id == id);

        /// <summary>补齐依赖与基础设施，返回最终要生成的能力集合（保持目录顺序）。</summary>
        public static IReadOnlyCollection<string> Resolve(IEnumerable<string> selected){
            var wanted = new HashSet<string>(selected ?? Enumerable.Empty<string>());
            foreach (var cap in All){
                if (cap.AlwaysOn) wanted.Add(cap.Id);
            }

            var queue = new Queue<string>(wanted);
            while (queue.Count > 0){
                var id = queue.Dequeue();
                var cap = Find(id);
                if (cap == null) continue;
                foreach (var dependency in cap.DependsOn){
                    if (wanted.Add(dependency)) queue.Enqueue(dependency);
                }
            }

            return All.Where(c => wanted.Contains(c.Id)).Select(c => c.Id).ToArray();
        }

        /// <summary>当前勾选会上哪几项能力（用于窗口提示"将自动包含"）。</summary>
        public static IEnumerable<string> AutoIncluded(IEnumerable<string> selected){
            var raw = new HashSet<string>(selected ?? Enumerable.Empty<string>());
            return Resolve(raw).Where(id => !raw.Contains(id));
        }

        /// <summary>本次会生成哪些模块（按 <see cref="Groups"/> 顺序，只包含至少有一项能力的组）。</summary>
        public static IReadOnlyList<ErameCapabilityGroup> SelectedGroups(IReadOnlyCollection<string> selected) =>
            Groups.Where(g => All.Any(c => c.Group == g.Id && selected.Contains(c.Id))).ToArray();

        /// <summary>
        /// 选中资源能力（场景/音频会自动带上资源）时，工程 manifest 里必须有 com.unity.addressables。
        /// 框架包本身不再声明这个依赖：不用资源的项目不该被迫安装它。
        /// </summary>
        public static bool RequiresAddressables(IReadOnlyCollection<string> selected) => selected.Contains(Asset);

        /// <summary>某个模板文件是否属于已选能力。</summary>
        public static bool Includes(IReadOnlyCollection<string> selected, string relativePath){
            if (string.IsNullOrEmpty(relativePath)) return false;
            var path = relativePath.Replace('\\', '/');

            foreach (var id in selected){
                var cap = Find(id);
                if (cap == null) continue;

                foreach (var pattern in cap.Paths){
                    if (pattern.EndsWith("/", StringComparison.Ordinal)){
                        if (path.StartsWith(pattern, StringComparison.Ordinal)) return true;
                    }
                    else if (string.Equals(path, pattern, StringComparison.Ordinal)){
                        return true;
                    }
                }
            }

            return false;
        }

        /// <summary>某个模块里是否有需要 ErameSettings 的注册（决定要不要写 settings 局部变量）。</summary>
        public static bool NeedsSettings(IReadOnlyCollection<string> selected, string group) =>
            All.Where(c => c.Group == group && selected.Contains(c.Id))
                .SelectMany(c => c.Registrations)
                .Any(r => r.Contains("settings."));
    }
}
