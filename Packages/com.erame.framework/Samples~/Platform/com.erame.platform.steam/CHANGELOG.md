# Changelog

本文件记录 Steam 平台包的对外变更。

## 0.6.0

配合框架 0.6.0。这版把"能初始化"补成"能上线"：补全 Steam 的回调面、出错时的可诊断性，以及 Steamworks 独有能力。

### 新增：Steam 扩展抽象（`Erame.Platform.Steam.Abstractions`）

- 新增独立程序集，**不依赖 Steamworks.NET、无 define 约束**，业务与测试都可无条件引用。
- `ISteamIntegration`：`AppId`、`InitializationError`、`IsOverlayEnabled` / `IsOverlayActive`、`StatsReady`、
  `IsLoggedOn`、`OverlayActivated` / `ConnectionChanged` 事件、`RequestStats()`、
  `GetAuthSessionTicketAsync()` / `CancelAuthTicket()`、`GetGameLanguage()` / `SyncLanguageFromSteam()`。
- `ISteamLobbyExtras`：`ChatMessageReceived`、`LobbyInviteReceived`、`Kicked`、`MaxMembers`、
  `RefreshLobbyData()`、`RemoveData(key)`。
- `ISteamWorkshopExtras`：`DownloadItemAsync`、`TryGetDownloadProgress`、`DeleteItemAsync`、`TryGetUpdateProgress`。
- `ISteamCloudExtras`：`GetFileNames()`、`TryGetQuota`、`GetFileTimestamp`、`RenameFileAsync`。
- `SteamLanguageMap` / `SteamPresenceMap`：纯映射，附 EditMode 单测。

### 身份与生命周期（`SteamService`）

- 注册 `GameOverlayActivated`、`UserStatsReceived`、`SteamServersConnected` / `SteamServersDisconnected` 回调；
  关闭时正确解绑，避免回调持有已释放对象。
- 初始化成功后主动 `RequestCurrentStats()`；断线重连自动重新拉取。
- 新增会话票据获取/作废，可用于自建服务器鉴权。
- 初始化失败不再只有一句 Warn：记录 `InitializationError` 并在日志里给出具体排查项。
- AppID 与 `steam_appid.txt` 不一致时告警，避免"配了 480 却连上正式包"。
- 支持按 Steam 客户端语言自动切换本地化语言（`ErameSettings.SteamSyncLanguage`，默认开）。
  正式 App 以"游戏语言"为准，玩家可以在库里单独把某个游戏设成英文；演示 AppID 480 的游戏语言恒为 `english`，
  此时改用客户端界面语言（`SteamUtils.GetSteamUILanguage`），避免中文客户端被判成英文。日志会打印两个原始值。

### 玩家能力（`SteamPlayerFeatures`）

- 六个接口现在共享**同一个实例**（此前每个接口各 new 一个）。
- 成就/统计读写加 `StatsReady` 门控：未就绪时返回 false，而不是被 Steam 静默丢弃。
- 好友在线状态改走 `SteamPresenceMap`，越界值降级为 Offline。

### 联机大厅（`SteamLobbyService`）

- 新增大厅聊天**接收**（此前只能发不能收）、大厅邀请、被踢/断线事件。
- `JoinAsync` 会先离开当前大厅，重复加入同一大厅直接成功。
- `Leave()` / `Dispose()` 会结束未完成的创建/加入请求，调用方不再永久等待。
- 大厅数据值缓冲从 256 提升到 8192（Steam 上限），不再截断长文本。
- 移除与 `CallResult` 重复的空 `Callback<LobbyCreated_t>`。

### 创意工坊（`SteamWorkshopService`）

- 新增条目下载、下载进度、上传进度、删除条目。
- 提交时若"需要接受创意工坊协议"，现在返回 **false**（此前返回 true 但内容不可见）。
- 实现 `IService`，随容器统一初始化/释放。

### 云存档（`SteamCloudService`）

- 新增文件列表、配额、文件时间戳、重命名（读-写-删模拟）。
- `IsEnabled` 同时检查账号级与应用级云开关。

### 编辑器

- 框架侧新增 `Erame/平台/检查 Steam 环境` 与 `Erame/平台/生成 steam_appid.txt`（见框架 CHANGELOG）。

### ⚠ 破坏性变更

- `SteamService` 构造函数新增可选参数 `(ILogService, uint, ILocalizationService = null, bool syncLanguage = true)`。
- `SteamPlayerFeatures` 构造函数新增可选参数 `(IPlatformService, ISteamIntegration = null)`。
- `SubmitAsync` 在待接受协议时返回 `false`。
- 依赖框架 `0.6.0`。
