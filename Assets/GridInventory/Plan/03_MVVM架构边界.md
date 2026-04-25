# GridInventory MVVM 架构边界（常看）

## 一句话原则

**Model 管状态，View 管显示，ViewModel 管调度，Service 管算法。**

---

## 边界定义

### Model

- 放：`InventoryState`、`InventorySaveData`、`Model/Data` 下 SO 资产
- 不放：UI、拖拽输入、放置规则流程

### View

- 放：`GridInventoryView`、`GridCellView`、`GridItemView`、`InventoryDragController`
- 职责：显示 + 接收输入 + 抛事件
- 不放：能不能放、怎么交换、怎么堆叠

### ViewModel

- 放：`InventoryViewModel`（唯一入口）
- 职责：接请求 -> 调 Model/Service -> 返回结果
- 不放：具体算法细节、UI 组件细节

### Services

- 放：`GridPlacementService`
- 职责：纯算法（越界、冲突、旋转映射、交换策略）
- 不放：MonoBehaviour UI 逻辑

---

## 开发决策规则

1. “能不能放”相关问题，一律先看/改 `GridPlacementService`。
2. “何时调用、调用顺序”问题，一律先看/改 `InventoryViewModel`。
3. “界面刷新和交互事件”问题，一律先看/改 `View/UI`。
4. 任何文件承担两个以上职责，才允许拆分。
5. 所有 Runtime 功能默认要支持 `Samples` 场景按需启用/禁用。
6. 模块关闭时不得导致其他层报错（例如禁用堆叠后，拖拽放置仍可运行）。

---

## Samples 插拔规则

为支持 `Assets/GridInventory/Samples` 的分步演示，必须遵守：

- 能力分层：放置、旋转、堆叠、容器、保存分别是独立能力。
- 调用入口：所有能力由 `ViewModel` 统一调度，不在 `View` 里硬编码流程。
- 默认降级：某能力不存在或关闭时，返回安全失败，不抛异常。
- 场景组合：每个 Sample 只启用本演示需要的能力，其余可缺省。

---

## 禁止项

- 不要重新引入 `UseCase/Rules/Core` 大量空骨架。
- 不要在 View 写业务判定与回滚。
- 不要为了“未来扩展”提前拆十几个结果类。
