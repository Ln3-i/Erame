# Erame

面向独立开发者的 Unity 通用开发底座：**纯 C# 内核 + 启动壳 + 可按需生成的业务层契约/实现**。

底座只负责"任何游戏都需要"的部分——容器、模块装配、启动/退出、帧驱动；具体服务（输入、UI、存档、音频、资源……）以契约 + 默认实现的形式放在业务工程里，**用不用、改不改、重写不重写都由项目决定**。

- Unity 2022.3+
- 当前版本 **0.10.0**
- 文档：[框架 README](Packages/com.erame.framework/README.md) · [开发手册](Packages/com.erame.framework/开发手册.md) · [CHANGELOG](Packages/com.erame.framework/CHANGELOG.md)

## 在别的项目里安装

在宿主工程的 `Packages/manifest.json` 中加入：

```json
{
  "dependencies": {
    "com.erame.framework": "https://github.com/Ln3-i/Erame.git?path=/Packages/com.erame.framework#v0.10.0"
  }
}
```

或者：Package Manager → `+` → **Add package from git URL…** → 粘贴

```
https://github.com/Ln3-i/Erame.git?path=/Packages/com.erame.framework#v0.10.0
```

把 `#v0.10.0` 换成 `#main` 会跟踪最新提交（只建议试验项目这么用）。
装了框架包之后，用菜单 **`Erame/示例/生成项目（按需）`** 勾选本项目需要的能力，生成业务层的契约、实现与模块。

## 仓库结构

```
Erame/
├── Packages/com.erame.framework/   框架包本体（UPM，可被其它工程引用）
│   ├── Runtime/Erame.Core/         纯 C# 底座：容器、模块、入口抽象、算法
│   ├── Runtime/Erame.Runtime/      启动壳：MonoBehaviour 入口、运行时编排、设置
│   ├── Editor/Erame.Editor/        生成器、按需生成窗口、诊断菜单
│   └── Samples~/                   Minimal 示例 + 业务层契约/实现模板
└── Assets/Game/                    用框架搭的示例工程（14 个能力域模块 + 业务层服务）
```

示例工程本身就是一个完整可运行的 Unity 项目，可以直接打开 Play 观察启动链。

## 版本

| 标签 | 内容 |
|---|---|
| `v0.10.0` | 模块按能力域拆分；框架包移除 Addressables 硬依赖；生成器改按需生成 |
| `v0.9.0` | 底座移除能力契约（`IEventBusDiagnostics` 下沉业务层） |
| `v0.8.0` | 业务服务与契约全部下沉到业务工程，底座只剩纯 C# 内核 |

历史变更见 [CHANGELOG](Packages/com.erame.framework/CHANGELOG.md)。

## 说明

- 平台能力（Steam / Epic / WeGame / TapTap 等）不再由框架提供，按项目需要自行实现模块与服务。
- 仓库暂未附带开源许可证；在对外分发前请自行选择（例如 MIT）并补一个 `LICENSE`。
