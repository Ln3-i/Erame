# Erame 框架

一个**纯 C# 底座 + 轻量启动壳 + 业务侧 Runtime 实现**的轻量游戏框架。开发者只需要写一个入口类并挂到场景，即可启动整个项目。

## 包组成

```
Packages/
└── com.erame.framework/           ← 必装
    ├── Runtime/
    │   ├── Erame.Core/            ← 纯 C# 底座，noEngineReferences = true
    │   │   ├── Entry/             IErameEntry、ErameBuilder、ErameEntry（高级/内部）
    │   │   ├── Container/         ServiceContainer、IModule、IService、IFrameUpdate
    │   │   ├── Log/               LogLevel（基础配置枚举）
    │   │   ├── Algorithms/        AStar、UnionFind、拓扑排序、优先队列
    │   │   └── Utils/             MathUtil、PCG32
    │   └── Erame.Runtime/         ← 启动壳（保持轻量）
    │       ├── Bootstrap/         ErameMonoEntry（推荐入口）、ErameRuntime（编排）
    │       └── Config/            ErameSettings（可选配置）
    ├── Editor/                    ← Erame.Editor：生成器、诊断
    └── Samples~/
        ├── Minimal/               ← 最小入口样例
        ├── Runtime/               ← Runtime 契约/实现模板；生成项目时按能力复制到业务工程
        └── Platform/              ← 历史平台实现归档（不参与编译，仅供参考）
```

`Erame.Core` 与启动壳留在包内；生成项目时，被勾选能力的 Runtime 契约与实现会写入 `Assets/Game/Scripts/Runtime`，归业务所有，可直接修改或删除。平台包不再由框架提供。

## 安装

### 方式一：嵌入工程（推荐用于本仓库自身）

把 `Packages/com.erame.framework` 放进工程的 `Packages/` 目录，Unity 会自动识别为 Embedded Package，无需修改 `manifest.json`。

### 方式二：Git 依赖

在宿主工程的 `Packages/manifest.json` 中添加：

```json
{
  "dependencies": {
    "com.erame.framework": "https://github.com/<your-account>/<your-repo>.git?path=/Packages/com.erame.framework#v0.10.0"
  }
}
```

平台能力（Steam / Epic / Wegame）不再作为框架扩展提供；需要时由开发者自行实现模块与服务。

## 快速上手（三步）

1. 菜单 **`Erame/示例/生成项目（按需）`**：勾选这个项目真正需要的能力，再生成对应的目录结构 + 程序集 + 业务模块 + 契约/实现 + 入口类 + 框架设置 + 已挂好组件的空场景。没有勾选的服务从一开始就不生成；想一次要全套就用 **`Erame/示例/生成项目（全部能力）`**。
2. 打开 `Game.unity`，进入 Play。
3. Console 出现启动日志即成功；之后所有业务（包括 Runtime 服务实现）只需维护 `Scripts/` 下的代码。

生成目录（与仓库中 `Assets/Game` 的层级一致）：

```
Assets/Game/
├── Art/            Textures / Materials / Models
├── Audio/
├── Prefabs/
├── Scenes/         Game.unity（已挂载 GameEntry）
├── Scripts/        Game.asmdef（引用 Erame.Core / Erame.Runtime / Game.Runtime）
│   ├── GameEntry.cs
│   ├── Modules/
│   │   ├── LogModule.cs        一个能力域一个模块，只含勾选的注册行
│   │   ├── EventsModule.cs
│   │   └── …（按本次生成的能力出现）
│   └── Runtime/
│       ├── Game.Runtime.asmdef
│       ├── Contracts/       接口、DTO、事件（可直接修改或删除）
│       └── Services/        运行时实现（可直接修改或删除）
└── Settings/       ErameSettings.asset（已生成 32 字节存档密钥）
```

`ErameSettings` 已存在时**不会覆盖**，避免冲掉已固定的存档密钥。

> 勾选「资源 / 场景 / 音频」能力时，请先在 Package Manager 安装 `com.unity.addressables`：
> 框架包本身不声明这个依赖，只有用到资源能力的项目才需要它。生成器会在缺包时给出警告。

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

手写项目如果要使用内置实现，需要把包内 `Samples~/Runtime`（Contracts + Services）复制到业务工程（或直接用菜单生成）；否则只注册你自己的服务即可。

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
    ├── entry.Configure(builder)             ← 业务注册
    ├── 模块统一 Configure                   ← 各 *Module 在这里注册服务
    ├── Build + InitializeAll                ← 依赖拓扑序，失败自动回滚
    └── entry.OnStart()

每帧 Update → ErameRuntime.Update(deltaTime) → entry.OnUpdate + 所有 IFrameUpdate 服务（Tick 等）
退出 → ErameRuntime.Quit() → entry.OnQuit() → 容器逆序 Dispose
```

所有服务（包括默认服务、资源、场景、输入、UI、池、音频、本地化、流程、存档、数据、会话等）都由业务模块注册。Runtime 实现位于 `Assets/Game/Scripts/Runtime`，可直接修改或删除；框架包内只保留 `Erame.Core` 与启动壳。

> 高级/内部：无 UnityEngine 的宿主可继承 `ErameEntry`（纯 C#）并自行驱动 `ErameRuntime`，普通 Unity 项目无需使用。

## 编辑器菜单

| 菜单 | 作用 |
|---|---|
| `Erame/示例/生成项目（按需）` | 勾选能力后生成：只写选中能力的契约/实现与模块注册；再次运行只补缺失文件 |
| `Erame/示例/生成项目（全部能力）` | 一次生成全部能力，等价于"按需"里全选 |
| `Erame/示例/生成框架设置` | 在选中目录创建可选的 `ErameSettings`（含 32 字节密钥） |
| `Erame/配置/校验框架设置` | 检查密钥长度与日志级别 |
| `Erame/配置/生成开发期密钥` | 重新生成密钥 |
| `Erame/配置/定位当前设置` | 选中当前设置资产 |
| `Erame/测试/容器快照` | Play 模式下导出服务注册与初始化顺序 |
| `Erame/测试/事件统计` | 列出各事件类型的订阅者数量（反射查找，不依赖具体事件总线类型） |
| `Erame/诊断/框架自检` | 检查 Addressables、Input System、TMP、存档目录与包版本 |
| `Erame/诊断/检查存档` | 列出存档文件、大小与修改时间 |
| `Erame/构建/发布前检查` | 校验密钥与日志级别 |
| `Erame/检查更新` | 对比本地版本与 Git 远端标签 |
| `Erame/帮助/打开开发手册` | 由浅入深的完整开发教程（模块/事件/状态机/UI/存档/配置表/平台/发布） |
| `Erame/帮助/打开框架文档`、`打开 API 文档` | 打开包内文档 |

## 服务一览

下表的实现类位于生成后的 `Assets/Game/Scripts/Runtime/Services`（属于业务工程），不是框架包内核。

| 接口 | 实现 | 说明 |
|---|---|---|
| `ILogService` | `UnityLogService` | 分级日志（`LogModule` 注册，可删改） |
| `IEventBus` | `EventBus` | 结构体事件，订阅者异常互相隔离（`EventsModule` 注册，可删改） |
| `ITimeService` | `UnityTimeService` | 缩放/非缩放时间（`TimeModule` 注册，可删改） |
| `IAssetService` | `AddressableAssetService` | 同 key 共享句柄 + 引用计数 |
| `IInstantiateService` | `AddressableAssetService` | 按 key 实例化预制体，按实例回收 |
| `ISceneService` | `UnitySceneService` | 单场景切换 / 叠加加载，各自持有资源作用域 |
| `IInputGate` | `InputGate` | 与输入帧无关的模态门闸（`InputModule` 注册，可删改），UI/对话/过场共享 |
| `IInputService` | `InputService` | 输入上下文栈（固定 `InputFrame`，兼容层）；新项目用 `IInputService<TFrame>` |
| `IInputService<TFrame>` | `InputService<TFrame>` | 泛型输入栈，帧类型由业务定义 |
| `IUIService` | `UIService` | 面板栈，操作串行化；支持带参数打开、按 key 关闭 |
| `IPoolService` | `UnityPoolService` | 池化 + 重复回池保护 |
| `IAudioService` | `UnityAudioService` | BGM 交叉淡入淡出，SFX 句柄随播放结束释放 |
| `ILocalizationService` | `LocalizationService` | 内置兜底文案 + CSV 覆盖 |
| `IGameStateMachine` | `GameStateMachine` | 游戏流程状态机 |
| `ITickService` | `TickService` | 固定步长逻辑 Tick；支持暂停/变速 |
| `ICommandService` | `CommandService` | GM/调试指令 |
| `ISaveService` | `FileSaveService` | 随机 IV 加密、原子写入、`.bak` 回退 |
| `ISaveSlotService` | `FileSaveService` | 槽位存档：元数据 + 缩略图，列槽位不解密 |
| `IDataRegistry` | `DataRegistryService` | 配置表注册表 |
| `ISessionService` | `SessionService` | 会话服务（当前为占位实现，单机开局/联机房间尚未实现） |

### 修改或替换内置服务

实现类都在 `Assets/Game/Scripts/Runtime/Services`，与普通业务代码一样可以改：

- **改行为**：直接编辑实现类。例如 `Save/FileSaveService.cs` 可以改成自己的加密、分块或异步策略。
- **换实现**：删掉对应 `*Module` 里对应的注册行，再注册自己的实现：

  ```csharp
  registrar.RegisterSingleton<ISaveService>(p => new MySaveService(p.Get<ILogService>()));
  ```

  容器对同一接口重复注册会直接报错，所以"替换 = 不注册旧实现"，不是"再注册一个"。
- **换接口**：需要新契约时，在业务程序集新增接口（例如 `IMySaveService`），不必改 `Erame.Core`。
- **具体类型**（如 `FileSaveService`）只用于装配；业务代码请依赖接口，避免替换后编译断裂。

生成器逐文件判断：只生成你勾选能力对应的文件；目标文件已存在时**保留**（不覆盖业务修改），因此可以随时再开一次窗口补生成新增能力。

### 平台能力

平台 SDK（Steam / Epic / Wegame）不再由框架提供，也不再作为框架扩展。需要时由开发者自行实现：

- 在业务程序集定义平台接口与实现（例如 `IMyPlatformService` / `MySteamService`）；
- 写一个 `IModule`，在 `Configure` 里注册自己的平台服务；
- 入口 `builder.AddModule<你的平台模块>()`。

包内 `Samples~/Platform` 只是历史实现归档，不参与编译，可作为编写参考。

## 存档

- 密钥来自 `ErameSettings`（可选资产）；未配置时使用 PlayerPrefs 中的开发期密钥
- AES-GCM，格式 `[12 字节 Nonce][密文][16 字节 Tag]`
- 主线程快照 → 后台加密 → 临时文件 → 原子替换，并保留 `.bak`
- 支持 `CancellationToken`；旧存档可通过 `FileSaveService.RawDataMigrator` 迁移
- 槽位存档（`ISaveSlotService`）：`persistentDataPath/Saves/slot_{n}.sav` + `.meta`（标题/摘要/时长/时间）+ `.thumb`（截图）
- 想定制存档：直接改 `Runtime/Services/Save/FileSaveService.cs`，或删掉注册并实现自己的 `ISaveService` / `ISaveSlotService`；`ISaveable` 与 `SaveWriter` 是业务契约（`Game.Runtime`）

## 作用域

根容器只有单例；需要"一局一套服务"时用 `ErameRuntime.Services.CreateScope()`：

```csharp
using var session = ErameRuntime.Services.CreateScope();
session.RegisterSingleton<IEconomyService>(p => new EconomyService(p.Get<IDataRegistry>()));
session.InitializeAll();   // 只初始化/释放作用域内的服务，根容器单例不受影响
```

解析顺序为"作用域 → 根容器"，因此局级服务可以依赖全局单例，反之不行。

## 测试

宿主工程的 EditMode 测试程序集需要引用 `Erame.Core` 与业务程序集 `Game.Runtime`（契约/实现所在程序集）：

`Window → General → Test Runner → EditMode → Run All`

## 命名空间约定

- `Erame.Core`：底座（容器、入口、算法、IFrameUpdate、LogLevel）
- `Erame.Runtime.Bootstrap`：入口与启动编排
- `Erame.Runtime.Config`：可选配置
- `Game.Runtime`：业务契约（接口、DTO、事件）
- `Game.Runtime.Services`：业务实现
- `Game.*`：业务入口与模块（`Game` 程序集）
