# Minimal Entry 最小入口示例

演示「只写一个入口类，挂到场景即用」。

## 文件

- `GameEntry.cs` — 入口类：继承 `ErameMonoEntry`，重写 `Configure`
- `Game.unity` — 已挂好 `GameEntry` 组件的示例场景

## 使用

1. 在 Package Manager 中导入本样本。
2. 打开导入后的 `Game.unity`。
3. 进入 Play，Console 出现 `[Game] 入口启动成功` 即完成。

## 说明

- 启动链路：`Entry.Awake → ErameRuntime.Start(this)`，全程静态调用、无反射。
- 本示例不注册任何服务：需要时在 `Configure` 里 `builder.AddModule<LogModule>()`（或其他按能力域拆分的模块），或直接 `RegisterSingleton` 自己的实现。
- 模块由「`Erame/示例/生成项目（按需）`」生成，一个能力域一个文件，可删可改。
- 入口自动 `DontDestroyOnLoad`，重复实例会被销毁。

