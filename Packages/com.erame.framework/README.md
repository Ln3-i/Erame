# Erame 框架

一个**纯 C# 核心 + Unity 实现 + 可选平台包**的轻量游戏框架。核心目标是让玩法逻辑不依赖引擎，让引擎相关实现可以整体替换，并让其他开发者可以直接以 UPM 包的形式接入。

## 包组成

```
Packages/
├── com.erame.framework/           ← 必装
│   ├── Runtime/
│   │   ├── Erame.Core/            ← Erame.Core：纯 C#，noEngineReferences = true
│   │   │   ├── Container/         ServiceContainer、IModule、IService
│   │   │   ├── Event/             EventBus（struct 事件 + 订阅凭证）
│   │   │   ├── Asset/             IAssetService、IAssetScope、AssetHandle
│   │   │   ├── Audio/ Input/ UI/ Pool/ Save/ Data/ Debug/ Time/ Scene/ Session/ Platform/
│   │   │   └── Algorithms/        AStar、UnionFind、拓扑排序、优先队列
│   │   ├── Erame.Runtime/         ← Erame.Runtime：Unity 实现层
│   │   │   ├── Bootstrap/         ErameInstaller（服务装配入口）
│   │   │   ├── Config/            ErameSettings（存档密钥 / SteamAppId / 日志级别）
│   │   │   └── Services/          按领域分组：Asset/ Audio/ Data/ Debug/ Flow/
│   │   │                          Input/ Localization/ Log/ Platform/ Pool/ Save/
│   │   │                          Scene/ Session/ Time/ UI/
│   │   └── ...
│   └── Editor/                    ← Erame.Editor：PlatformSwitcher（平台宏切换）
│
└── com.erame.platform.steam/      ← 可选
    └── Runtime/                   ← Erame.Platform.Steam
                                     仅当安装了 Steamworks.NET 且开启 USE_STEAM 时编译
```

宿主工程中建议保留自己的业务程序集（例如 `Assets/Game`），它只依赖 `Erame.Runtime` / `Erame.Core`，不需要复制框架源码。

## 安装

### 方式一：嵌入工程（推荐用于本仓库自身）

把 `Packages/com.erame.framework`（以及可选的 `com.erame.platform.steam`）放进工程的 `Packages/` 目录，Unity 会自动识别为 Embedded Package，无需修改 `manifest.json`。

### 方式二：Git 依赖

在宿主工程的 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.erame.framework": "https://github.com/<your-account>/<your-repo>.git?path=/Packages/com.erame.framework#v0.2.0"
  }
}
```

需要 Steam 支持时再额外添加：

```json
"com.erame.platform.steam": "https://github.com/<your-account>/<your-repo>.git?path=/Packages/com.erame.platform.steam#v0.2.0"
```

也可以把包目录打成 `.tgz` 后通过 `Packages/manifest.json` 的本地路径 / `Add package from tarball` 安装。

## 快速接入

1. 创建配置资产：`Assets → Create → Erame → 框架设置`，得到 `ErameSettings`。
2. 填写 `SteamAppId`、日志级别与 32 字节存档密钥（留空会生成开发期密钥，仅适合本地调试）。
3. 在启动场景的 `GameBootstrap` 上把该资产拖入 `Settings` 字段。
4. 在业务程序集中注册服务：

```csharp
Container = new ServiceContainer();
Container.ErrorReporter = message => Debug.LogError(message);

ErameInstaller.InstallFramework(Container, settings);
ErameInstaller.InstallGameplay(Container, settings, _ => new MyInputAdapter());
ErameInstaller.InstallPlatform(Container, settings);

Container.Build();
Container.InitializeAll();
```

或者沿用 `IModule` 写法，把三个模块的实现委托给 `ErameInstaller`（参见仓库中的 `GameModules.cs`）。

## 程序集依赖

```
MyGame  ──►  Erame.Runtime  ──►  Erame.Core

Erame.Platform.Steam ──► Erame.Core ＋ Steamworks（仅 USE_STEAM 且 STEAMWORKS_AVAILABLE 时编译）
```

`Erame.Core` 开启了 `noEngineReferences`，可以脱离 Unity 做单元测试。

## 启动流程

```
GameBootstrap.Start()
└── GameApp.Start(settings)
    ├── new ServiceContainer()
    ├── 各 IModule.Configure(container)   ← Framework / Gameplay / Platform
    ├── Build() + InitializeAll()          ← 容器按依赖拓扑序初始化，失败自动回滚
    └── FlowRegistry.RegisterAll(flow)

GameBootstrap.Update()
├── GameApp.Flow.Update()
├── GameApp.Tick.Update(deltaTime)        ← ITickService 由主循环驱动
└── GameApp.Platform.Update()

GameBootstrap.OnDestroy()
└── GameApp.Quit() → Container.Dispose()  ← 逆序 Dispose
```

## 服务一览

| 接口 | 实现 | 说明 |
|---|---|---|
| `ILogService` | `UnityLogService` | 分级日志，级别来自 `ErameSettings` |
| `IEventBus` | `EventBus` | 结构体事件，订阅者异常互相隔离 |
| `ITimeService` | `UnityTimeService` | 缩放/非缩放时间 |
| `IAssetService` | `AddressableAssetService` | 同 key 共享句柄 + 引用计数，`await handle.Task` |
| `ISceneService` | `UnitySceneService` | 场景加载与资源作用域 |
| `IInputService` | `InputService` | 输入上下文栈；输入源由宿主注入 |
| `IUIService` | `UIService` | 面板栈，操作串行化，`CloseAllAsync` |
| `IPoolService` | `UnityPoolService` | 池化 + 重复回池保护 + 销毁时清理 |
| `IAudioService` | `UnityAudioService` | BGM 双 Source 交叉淡入淡出，SFX 句柄随播放结束释放 |
| `ILocalizationService` | `LocalizationService` | 内置兜底文案 + `StreamingAssets/Localization/*.csv` 覆盖 |
| `IGameStateMachine` | `GameStateMachine` | 游戏流程状态机 |
| `ITickService` | `TickService` | 固定步长逻辑 Tick |
| `ICommandService` | `CommandService` | GM/调试指令 |
| `ISaveService` | `FileSaveService` | 随机 IV 加密、原子写入、`.bak` 回退、版本迁移钩子 |
| `IDataRegistry` | `DataRegistryService` | 配置表注册表 |
| `ISessionService` | `SessionService` | 局内/房间级服务示例 |
| `IPlatformService` | `SteamService` / `NullPlatformService` | 平台能力，自动降级 |

## 平台切换

- 菜单：`Erame/平台切换/Steam` 与 `Erame/平台切换/原生模式`（写入 `USE_STEAM` 宏）
- 当工程未安装 `com.rlabrecque.steamworks.net` 时，`STEAMWORKS_AVAILABLE` 不会被定义：
  - `Erame.Platform.Steam` 程序集整体不参与编译
  - `ErameInstaller` 自动降级到 `NullPlatformService`
  - `Erame.Runtime` 不含任何 Steamworks 引用，可独立分发

## 存档

- 存档密钥来自 `ErameSettings`，由 `ErameInstaller` 注册 `ISaveCrypto`（`AesGcmSaveCrypto`）注入，密钥必须是 16/24/32 字节
- 加密算法为 AES-GCM，输出格式 `[12 字节 Nonce][密文][16 字节 Tag]`，任何篡改都会导致解密失败
- 写入流程：主线程快照 → 后台线程加密 → 临时文件 → 原子替换，并保留 `.bak`
- 保存/读档支持 `CancellationToken`，取消时不会破坏主存档
- 旧版本存档可通过 `FileSaveService.RawDataMigrator` 注册迁移逻辑

## 编辑器菜单

| 菜单 | 作用 |
|---|---|
| `Erame/配置/创建框架设置` | 在选中目录创建 `ErameSettings`，并生成 32 字节密钥 |
| `Erame/配置/校验框架设置` | 检查密钥长度、AppID、日志级别 |
| `Erame/配置/生成开发期密钥` | 为当前设置重新生成密钥 |
| `Erame/配置/定位当前设置` | 在 Project/Inspector 中选中当前设置 |
| `Erame/诊断/框架自检` | 检查 Addressables、Input System、TMP、Steamworks、存档目录与包版本 |
| `Erame/诊断/服务容器快照` | Play 模式下导出服务注册与初始化顺序 |
| `Erame/诊断/事件订阅统计` | 列出各事件类型的订阅者数量 |
| `Erame/诊断/检查存档` | 列出存档文件、大小与修改时间 |
| `Erame/平台/同步 AppID 到 steam_appid.txt` | 把设置中的 AppID 写入项目根目录 |
| `Erame/构建/发布前检查` | 校验密钥、AppID、日志级别与 Steam 配置 |
| `Erame/帮助/打开框架文档` | 打开包内 README / API 文档 |
| `Erame/帮助/检查包更新` | 对比本地版本与 Git 远端标签 |
| `Erame/平台切换/Steam`、`Erame/平台切换/原生模式` | 切换 `USE_STEAM` 宏 |

## 测试

宿主工程的 EditMode 测试程序集只需引用 `Erame.Core`，运行方式：

`Window → General → Test Runner → EditMode → Run All`

## 命名空间约定

- `Erame.Core`：接口与纯逻辑
- `Erame.Runtime.Services`：Unity 实现
- `Erame.Runtime.Config`：运行配置
- `Erame.Runtime.Bootstrap`：服务装配
- `Erame.Platform.*`：平台 SDK 封装
- `Game.Scripts.*`：业务代码（不属于框架包）
