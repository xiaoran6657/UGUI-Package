# UGUI-Package

## 1. 项目概述 (Project Overview)

UGUI-Package 是一个模块化的 Unity UI 工具包，专为 RPG、生存及掠夺类游戏中常见的 inventory/grid（ inventory/网格）界面设计。本工具包的核心设计目标是高性能与优秀的用户体验（UX）。

### 核心设计理念
- **性能优先**：采用虚拟化网格技术，仅实例化可见槽位。
- **交互流畅**：实现平滑可靠的滚动体验。
- **功能完备**：支持拖拽、合并、堆叠、上下文菜单及物品提示等完整交互逻辑。

## 2. 主要特性 (Key Features)

- **虚拟化 UI (Virtualized UI)**
    - `RecycledInventoryUI` 创建少量槽位视图池，随滚动重新定位并绑定数据。
    - 支持数万逻辑槽位的高效渲染。
- **对象池与性能优化 (Pooling & Performance)**
    - 最小化 GameObject 数量。
    - 摒弃缓慢的 `GridLayoutGroup` 重建，采用手动布局与缓存计算。
- **稳健的拖拽系统 (Robust Drag & Drop)**
    - `DragController` 管理非阻塞拖拽图标。
    - 基于射线检测的目标识别，支持移动、合并、交换行为。
- **堆叠逻辑 (Stacking Logic)**
    - 支持 `ItemInstance.TryStack`、合并、拆分及自动拆分。
    - `SortAndCompactByRarity()` 可按配置规则分组并 compact 物品。
- **上下文菜单与拆分 UI (Context Menu + Split UI)**
    - 支持右键菜单（使用/拆分/出售/丢弃）。
    - 提供带滑块与自动拆分按钮的 `SplitWindow`。
- **物品提示 (Tooltip)**
    - 跟随鼠标显示，采用稀有度颜色区分。
    - 非阻塞设计（不干扰指针事件）。
- **工具组件 (Utilities)**
    - `SelectiveScrollRect`：仅在抓取滚动条时拖动内容，滚轮仍可正常工作，分离物品拖拽与列表滚动。

## 3. 快速开始 (Quick Start)

Unity版本：Unity 2022.3.45f1c1

### 3.1 安装
1. 将 `/Assets/UGUI-Package` 目录复制到您的 Unity 项目中。
2. 打开示例场景，或将以下 Prefab 放置于主 Canvas 下：
    - `SlotPrefab`：绑定 `SlotView` 引用。
    - `ItemTooltip`：绑定 TMP/Text 及背景。
    - `ContextMenu`：绑定根 RectTransform 及四个按钮。
    - `SplitWindow`：绑定滑块、文本及按钮。

### 3.2 配置
1. 安装TextMeshPro 和 Unity-TMPro-Chinese-Font（用于中文字体）。https://github.com/jkl54555/Unity-TMPro-Chinese-Font
2. 在场景中创建一个 `ItemDataManager`，并 populate ScriptableObject 物品配置（或使用提供的示例配置）。
2. ItemTooltip / ContextMenu / SplitWindow 的根对象中的RootCanvas字段要绑定为场景中的主 Canvas。
2. 放置 `RecycledInventoryUI` 组件并进行如下赋值：
    - **Content**: ScrollView 的 Content RectTransform。
    - **Slot Prefab**: 您的槽位预制体。
    - **Scroll Rect**: 推荐assign `SelectiveScrollRect`。
3. 运行场景，使用 Play 模式下的 `DebugInventoryTester` 添加物品并测试功能。

## 4. API 参考 (API Reference)

### 4.1 RecycledInventoryUI
核心控制器，管理后端数据与 UI 表现。
- `Initialize(List<ItemInstance> records, int maxCapacity)`:  externally 初始化后端列表与容量。
- `AddItemById(int itemId, int count)`: 添加物品；自动堆叠后填充空槽；必要时扩展后端。
- `SetSlot(int index, ItemInstance inst) / ClearSlot(int index)`: 设置或清除指定槽位。
- `GetBackendAt(int index)`: 只读访问后端物品实例。
- `SortAndCompactByRarity()`: 按稀有度降序分组/compact（满堆叠优先）。
- `ForceRebuildLayout()`: 运行时更改布局字段后调用。
- **上下文操作**: `UseItemAt`, `SellItemAt`, `DropItemAt`, `SplitItemAt`, `AutoSplitItemAt`。

### 4.2 DragController
管理拖拽行为。
- `BeginDragFromSlot(SlotView source)`: 由 `SlotView` 长按自动调用，创建拖拽图标。
- 处理射线查找并调用 `RecycledInventoryUI.HandleDrop(srcIndex, dstIndex)`。

### 4.3 SlotView
单个槽位的视图逻辑。
- 实现指针处理器 (Pointer Handlers)。
- 暴露 `SetDisplayIndex(int)` 与 `Bind(ItemInstance)`。
- 负责指针进入/退出（提示框）、点击（上下文菜单）及委托长按给拖拽控制器。

### 4.4 ItemTooltip
物品提示框管理。
- `Show(ItemInstance) / Show(ItemInfo, count)`: 显示提示。
- `Hide() / Disable() / Enable()`: 控制显示状态。
- **注意**: `Disable()` 用于 ContextMenu/SplitWindow 打开时暂时防止提示框显示。

## 5. 实现细节 (Implementation Details)

- **布局系统**: 不使用 `GridLayoutGroup`，采用手动定位以避免昂贵的布局重计算。
- **内容高度**: 由 `RecalculateLayout()` 计算，考虑列数、总行数、槽位高度、间距及内边距。
- **池重建**: 当布局参数或视口大小发生显著变化时重建池。
- **射线检测顺序**: 将交互式 UI 元素（如滚动条）在层级结构中置于 Viewport 之后，以确保预期的指针行为。

## 6. 性能优化建议 (Performance Tips)

- **减少重计算**: 降低 `spacingX/spacingY` 重计算频率，仅在影响布局的参数变更时调用 `ForceRebuildLayout()`。
- **射线目标**: 禁用子图像/文本上不必要的 `raycastTarget` (设为 false)，减小射线检测列表大小。
- **池大小 (Buffer)**: 较小值减少 Draw Call 但可能导致弹出 (pop-in)；请在目标平台测试。
- **大规模后端**: 扩展极大后端时，考虑异步增长 `_backendRecords` 或跨帧批处理池重建。

## 7. 调试清单 (Debugging Checklist)

| 问题现象 | 排查步骤 |
| :--- | :--- |
| **指针事件未响应** | 检查 EventSystem、GraphicRaycaster；确保槽位 Prefab 拥有带 `raycastTarget=true` 的 Graphic (Image)。 |
| **上下文菜单无响应** | 确保菜单设置为 `SetAsLastSibling()` 且 CanvasGroup 的 `blocksRaycasts=true`。 |
| **提示框闪烁** | 确保 Tooltip 的 UI `blocksRaycasts=false` 且 CanvasGroup 的 `blocksRaycasts=false`。 |

## 8. 许可证与贡献 (License & Contribution)
- **贡献**: 欢迎在 GitHub 仓库提交 Issues 与 PRs。
- **PR 要求**: UI 变更请包含 Playmode 测试场景。
