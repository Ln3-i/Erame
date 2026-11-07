# Erame Platform - Steam

Erame 框架的 Steam 实现（可选包）。除通用平台接口外，还提供一层不依赖 Steamworks 的扩展抽象。

## 安装

1. 添加包 `com.erame.platform.steam`（依赖 `com.erame.framework >= 0.6.0`，会自动带上 Steamworks.NET 2025.165.0）。
2. 菜单 `Erame/平台/Steam` 切换平台宏 `USE_STEAM`；缺 Steamworks.NET 时会询问并自动下载。
3. 菜单 `Erame/平台/生成 steam_appid.txt` 写入本地调试用的 AppID。
4. 菜单 `Erame/平台/检查 Steam 环境` 逐项确认（平台包 / Steamworks.NET / 宏 / appid 文件 / 原生库 / AppID 是否仍是 480）。
5. 入口必须显式开启：`builder.AddPlatformModule();`

## 能力一览

通用接口（`Erame.Core.Platform`）：

| 接口 | 内容 |
|---|---|
| `IPlatformService` | 身份、生命周期、回调泵 |
| `IPlatformAchievementService` / `IPlatformStatsService` | 成就、统计（带就绪门控） |
| `IPlatformPresenceService` / `IPlatformOverlayService` | 富状态、Overlay 打开 |
| `IPlatformFriendService` / `IPlatformDlcService` | 好友、DLC |
| `IPlatformLobbyService` | 大厅创建/加入/离开、成员、数据、发消息 |
| `IPlatformCloudService` | 云存档读写删 |
| `IPlatformWorkshopService` | 工坊条目创建/提交/订阅/安装信息 |

Steam 扩展（`Erame.Platform.Steam.Abstractions`，不需要 Steamworks 即可引用）：

| 接口 | 内容 |
|---|---|
| `ISteamIntegration` | Overlay 状态与事件、统计就绪、服务器连接、会话票据、客户端语言同步 |
| `ISteamLobbyExtras` | 聊天接收、邀请、被踢、人数上限、数据刷新/删除 |
| `ISteamWorkshopExtras` | 下载、下载进度、删除、上传进度 |
| `ISteamCloudExtras` | 文件列表、配额、时间戳、重命名 |

```csharp
if (services.TryGet<ISteamIntegration>(out var steam)){
    steam.OverlayActivated += active => { if (active) tick.Pause(); else tick.Resume(); };
}

if (services.TryGet<ISteamLobbyExtras>(out var lobbyExtras)){
    lobbyExtras.ChatMessageReceived += (sender, text) => Debug.Log($"{sender}: {text}");
}
```

## 注意

- **统计数据就绪前写成就无效**：Steam 在 `UserStatsReceived` 之前会丢弃写入。框架已门控，未就绪时 `UnlockAchievement` / `SetInt` 返回 false。
- **`steam_appid.txt` 不要进正式包**：发布由 Steam 客户端传入 AppID；该文件只用于编辑器/本地调试。
- **AppID 480 是演示值**：发布前必须在 `ErameSettings` 里换成正式 AppID。
- **Overlay 不会自动暂停游戏**：需要自己订阅 `OverlayActivated` 暂停逻辑 Tick。
- **语言同步看的是"客户端界面语言"，不是 Steam 的显示名**：`GetCurrentGameLanguage()` 返回的是"这个 App 当前以哪种语言运行"。
  用演示 AppID 480 时它恒为 `english`（Spacewar 只声明了英文），所以框架在 480 下改用客户端 UI 语言；正式 App 仍以游戏语言为准。
  启动日志里的 `语言探测：游戏语言=... 客户端语言=... 采用=...` 可以直接看出判定依据。
- **创意工坊协议**：未在 Overlay 里接受协议时，`SubmitAsync` 返回 false。
- 本包只在 `USE_STEAM` + `STEAMWORKS_AVAILABLE` 同时成立时参与编译；缺任一条时所有通用接口都不注册，业务用 `TryGet` 自然降级。

## 测试

`Tests/EditMode` 覆盖 `SteamLanguageMap` 与 `SteamPresenceMap`（纯映射，不需要 Steam 客户端）。Steamworks 相关代码需要真实客户端，未纳入自动化测试。
