# Minimal Entry 最小入口示例

演示「一个入口启动整个项目」的最小接线方式。

## 文件

- `MinimalEntry.cs` — 入口类：继承 `ErameEntry`，重写四个虚方法
- `MinimalEntryProvider.cs` — 入口提供者：ScriptableObject，负责创建入口

## 使用步骤

1. 点击 Package Manager 中本样本的 **Import** 导入到工程中。
2. 创建配置资产：`Assets → Create → Erame → 框架设置`（可留空，使用默认配置）。
3. 右键 `Create → Erame → Minimal Entry Provider` 创建提供者资产，按需指定设置。
4. 在场景中创建 GameObject，添加 `ErameBootstrap` 组件，把提供者资产拖到 **Provider** 字段。
5. 进入 Play，Console 出现 `[Minimal] 入口启动完成` 即成功。

## 说明

- 启动顺序由框架保证：注册默认服务 → `Configure`（业务注册）→ 模块 Configure → Build + InitializeAll → `OnStart`。
- 每帧驱动走 `ErameRuntime.Update()` → `OnUpdate()`，退出时 `OnQuit()` → 容器 Dispose。
- 也可以不放场景组件：把提供者资产命名为 `ErameEntryProvider` 放进 `Resources/`，
  并在 Player Settings 定义 `ERAME_AUTO_START` 宏，由 `ErameAutoStart` 自动启动。

也可以直接使用菜单 `Erame/生成最小入口`，一步生成入口 + 提供者 + 资产 + 挂好组件的场景。
