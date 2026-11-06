# Minimal Entry 最小入口示例

演示「只写一个入口类，挂到场景即用」。

## 文件

- `MinimalEntry.cs` — 入口类：继承 `ErameMonoEntry`，重写 `Configure`
- `Minimal.unity` — 已挂好 `MinimalEntry` 组件的示例场景

## 使用

1. 在 Package Manager 中导入本样本。
2. 打开导入后的 `Minimal.unity`。
3. 进入 Play，Console 出现 `[Minimal] 入口启动成功` 即完成。

## 说明

- 启动链路：`MinimalEntry.Awake → ErameRuntime.Start(this)`，全程静态调用、无反射。
- 框架默认注册日志、事件、时间三个服务；其余服务在 `Configure` 里自行注册或通过 `builder.AddModule<T>()` 添加。
- 入口自动 `DontDestroyOnLoad`，重复实例会被销毁。
