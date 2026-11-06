# Erame 框架

一个**纯 C# 核心 + Unity 实现 + 可选平台包**的轻量游戏框架。开发者只需要写一个入口类并挂到场景，即可启动整个项目。

## 包组成

```
Packages/
├── com.erame.framework/           ← 必装
│   ├── Runtime/
│   │   ├── Erame.Core/            ← 纯 C#，noEngineReferences = true
│   │   │   ├── Entry/             IErameEntry、ErameBuilder、ErameEntry（高级/内部）
│   │   │   ├── Container/         ServiceContainer、IModule、IService
│   │   │   ├── Event/             EventBus（struct 事件 + 订阅凭证）
│   │   │   ├── Asset/             IAssetService、IAssetScope、AssetHandle
│   │   │   ├── Audio/ Input/ UI/ Pool/ Save/ Data/ Debug/ Time/ Scene/ Session/ Platform/
│   │   │   └── Algorithms/        AStar、UnionFind、拓扑排序、优先队列
│   │   ├── Erame.Runtime/         ← Unity 实现层
│   │   │   ├── Bootstrap/         ErameMonoEntry（推荐入口）、ErameRuntime（编排）
│   │   │   ├── Config/            ErameSettings（可选配置）
│   │   │   └── Services/          按领域分组：Asset/ Audio/ Data/ Debug/ Flow/
│   │   │                          Input/ Localization/ Log/ Platform/ Pool/ Save/
│   │   │                          Scene/ Session/ Time/ UI/
│   │   └── ...
│   └── Editor/                    ← Erame.Editor：生成器、诊断、平台切换
│   └── Samples~/Minimal/          ← 最小入口样例
│
└── com.erame.platform.steam/      ← 可选
    └── Runtime/                   ← Erame.Platform.Steam
                                     仅当安装了 Steamworks.NET 且开启 USE_STEAM 时编译
```

宿主工程保留自己的业务程序集即可，不需要复制框架源码。

## 安装

### 方式一：嵌入工程（推荐用于本仓库自身）

把 `Packages/com.erame.framework`（以及可选的 `com.erame.platform.steam`）放进工程的 `Packages/` 目录，Unity 会自动识别为 Embedded Package，无需修改 `manifest.json`。

### 方式二：Git 依赖

在宿主工程的 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.erame.framework": "https://github.com/<your-account>/<your-repo>.git?path=/Packages/com.erame.framework#v0.5.0"
  }
}
```

需要 Steam 支持时再额外添加：

```json
"com.erame.platform.steam": "https://github.com/<your-account>/<your-repo>.git?path=/Packages/com.erame.platform.steam#v0.5.0"
```

## 快速上手（三步）

1. 菜单 **`Erame/示例/生成项目示例`**：生成 `Assets/Game/GameEntry.cs` 与挂好组件的 `Assets/Game/Scenes/Game.unity`。
2. 打开 `Game.unity`，进入 Play。
3. Console 出现启动日志即成功；之后所有业务只需维护 `GameEntry`。

手写同样简单：

```csharp
using Erame.Core;
using Erame.Runtime.Bootstrap;

public sealed class GameEntry : ErameMonoEntry{
    public override void Configure(ErameBuilder builder){
        builder.RegisterSingleton<IMyService>(p => new MyService());
        builder.AddModule<MyModule>();
    }
}
```

把 `GameEntry` 组件挂到场景中任意对象上即可。入口自动 `DontDestroyOnLoad`，重复实例会被销毁。

`ErameBuilder` 只提供四种注册方式：

| 方法 | 说明 |
|---|---|
| `RegisterSingleton(instance)` | 注册现成单例 |
| `RegisterSingleton(factory)` | 注册单例工厂（依赖通过 `IServiceProvider` 解析） |
| `AddModule(IModule)` | 收集模块 |
| `AddModule<T>()` | 收集模块类型（编译期构造，无反射） |

模块只收集、不立即注册；`ErameRuntime` 会在业务注册完成后统一 Configure，所以模块之间不依赖注册顺序。框架**不提供** `RegisterService<TInterface,TImpl>()`（那需要反射）。

## 启动流程

```
场景 GameEntry（ErameMonoEntry）
└── Awake → ErameRuntime.Start(this)        ← 幂等
    ├── 注册默认服务（ILogService / IEventBus / ITimeService）
    ├── entry.Configure(builder)             ← 业务注册
    ├── 模块统一 Configure
    ├── Build + InitializeAll                ← 依赖拓扑序，失败自动回滚
    └── entry.OnStart()

每帧 Update → ErameRuntime.Update(deltaTime) → entry.OnUpdate + ITickService
退出 → ErameRuntime.Quit() → entry.OnQuit() → 容器逆序 Dispose
```

其余服务（资源、场景、输入、UI、池、音频、本地化、流程、存档、数据、会话等）由业务模块注册，参见仓库中的 `FrameworkModule` / `GameplayModule`。

> 高级/内部：无 UnityEngine 的宿主可继承 `ErameEntry`（纯 C#）并自行驱动 `ErameRuntime`，普通 Unity 项目无需使用。

## 编辑器菜单

| 菜单 | 作用 |
|---|---|
| `Erame/示例/生成项目示例` | 生成 `GameEntry` + 挂好组件的场景；已存在时提示覆盖或取消 |
| `Erame/示例/生成框架设置` | 在选中目录创建可选的 `ErameSettings`（含 32 字节密钥） |
| `Erame/配置/校验框架设置` | 检查密钥长度、AppID、日志级别 |
| `Erame/配置/生成开发期密钥` | 重新生成密钥 |
| `Erame/配置/定位当前设置` | 选中当前设置资产 |
| `Erame/测试/容器快照` | Play 模式下导出服务注册与初始化顺序 |
| `Erame/测试/事件统计` | 列出各事件类型的订阅者数量 |
| `Erame/诊断/框架自检` | 检查 Addressables、Input System、TMP、Steamworks、存档目录与包版本 |
| `Erame/诊断/检查存档` | 列出存档文件、大小与修改时间 |
| `Erame/平台/无平台`、`Erame/平台/Steam` | 切换 `USE_STEAM` 宏 |
| `Erame/平台/Epic`、`Erame/平台/Wegame` | 暂未实现，置灰 |
| `Erame/构建/发布前检查` | 校验密钥、AppID、日志级别与 Steam 配置 |
| `Erame/检查更新` | 对比本地版本与 Git 远端标签 |
| `Erame/帮助/打开框架文档`、`打开 API 文档` | 打开包内文档 |

## 服务一览

| 接口 | 实现 | 说明 |
|---|---|---|
| `ILogService` | `UnityLogService` | 分级日志（默认服务） |
| `IEventBus` | `EventBus` | 结构体事件，订阅者异常互相隔离（默认服务） |
| `ITimeService` | `UnityTimeService` | 缩放/非缩放时间（默认服务） |
| `IAssetService` | `AddressableAssetService` | 同 key 共享句柄 + 引用计数 |
| `ISceneService` | `UnitySceneService` | 场景加载与资源作用域 |
| `IInputService` | `InputService` | 输入上下文栈；输入源由业务注入 |
| `IUIService` | `UIService` | 面板栈，操作串行化 |
| `IPoolService` | `UnityPoolService` | 池化 + 重复回池保护 |
| `IAudioService` | `UnityAudioService` | BGM 交叉淡入淡出，SFX 句柄随播放结束释放 |
| `ILocalizationService` | `LocalizationService` | 内置兜底文案 + CSV 覆盖 |
| `IGameStateMachine` | `GameStateMachine` | 游戏流程状态机 |
| `ITickService` | `TickService` | 固定步长逻辑 Tick，由 `ErameRuntime.Update` 驱动 |
| `ICommandService` | `CommandService` | GM/调试指令 |
| `ISaveService` | `FileSaveService` | 随机 IV 加密、原子写入、`.bak` 回退 |
| `IDataRegistry` | `DataRegistryService` | 配置表注册表 |
| `ISessionService` | `SessionService` | 局内/房间级服务 |
| `IPlatformService` | `SteamService` / `NullPlatformService` | 平台能力，缺失时降级 |

## 存档

- 密钥来自 `ErameSettings`（可选资产）；未配置时使用 PlayerPrefs 中的开发期密钥
- AES-GCM，格式 `[12 字节 Nonce][密文][16 字节 Tag]`
- 主线程快照 → 后台加密 → 临时文件 → 原子替换，并保留 `.bak`
- 支持 `CancellationToken`；旧存档可通过 `FileSaveService.RawDataMigrator` 迁移

## 测试

宿主工程的 EditMode 测试程序集只需引用 `Erame.Core`：

`Window → General → Test Runner → EditMode → Run All`

## 命名空间约定

- `Erame.Core`：接口与纯逻辑
- `Erame.Runtime.Services`：Unity 实现
- `Erame.Runtime.Bootstrap`：入口与启动编排
- `Erame.Runtime.Config`：可选配置
- `Erame.Platform.*`：平台 SDK 封装
- `Game.*`：业务代码（不属于框架包）
