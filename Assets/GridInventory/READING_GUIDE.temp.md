# GridInventory 源码阅读路线

> 用途：离开项目一段时间后快速重新上手  
> 预计用时：1～2 小时  
> 阅读原则：**先建立地图 → 跟一条操作链路 → 最后看复杂分支**

---

## 0. 三层分工（先记这句）

| 层 | 目录 | 只回答一个问题 |
|---|---|---|
| **Data** | `Scripts/Data/` | 配表模板是什么 |
| **Core** | `Scripts/Core/` | 网格算法怎么算 |
| **Runtime** | `Scripts/Runtime/` | 场景里怎么表现 |

### 数据流

```
ItemBaseData  ──FromConfig──▶  ItemRtData  ──占格──▶  InventoryState
                                      │
                                      ▼
                            GridInventoryService
                                      │
                                      ▼
                            GridMainContainerView / ItemView

存档：ItemRtData ◀──▶ ItemSaveData
```

### 依赖方向（单向）

```
Data  ←  Core  ←  Runtime/Logic  ←  Runtime/View
```

---

## 1. Core 层（30 min · 最重要）

Core 不依赖 Unity 场景，是算法内核。

### 1.1 数据结构入口

**文件：** `Scripts/Core/InventoryState.cs`

| 字段 / 方法 | 含义 |
|---|---|
| `itemAnchorArray` | 每个锚点格子上放的是哪个物品（仅锚点格有值） |
| `occupancyOwnerArray` | 每个格子被谁占用（ footprint 内每格都有值） |
| `SetItemData` | **锚点唯一写入口**（写数组 + 同步 `item.SetAnchorPos`） |
| `CaptureSnapshot` / `RestoreSnapshot` | 快照事务（跨容器回滚用） |

### 1.2 四个 Partial Service

| 文件 | 职责 | 优先看的方法 |
|---|---|---|
| `InventoryState.PlacementService.cs` | 放置 / 移除 | `CanPlace` `SetAt` `SetAtFirst` `RemoveAtAny` |
| `InventoryState.StackableService.cs` | 堆叠 | `CanStack` `TryStack` `TrySplit` |
| `InventoryState.SwapService.cs` | 交换 | 见下方专题 |
| `InventoryState.PersisService.cs` | 存读档 | `Save` `Load` |

### 1.3 交换逻辑阅读顺序（SwapService 专题）

**文件：** `Scripts/Core/InventoryState.SwapService.cs`

```
GetSwapPlan              判断 Same / LargeToSmall / SmallToLarge
    ↓
TryGetSwapTargetItem     拖动物 footprint 盖住了谁
    ↓
SimulateSwap (CanSwap)   试算，快照还原，不改真数据
    ↓
CommitSwap (TrySwap)     真提交，失败快照回滚
    ↓
├── SwapSameItem         等量交换
├── SwapLargeToSmallItem 大换小（含小物回填 fallback）
└── SwapSmallToLargeItem 小换大
```

**大换小回填顺序（`TryPlaceLittleItemAfterLargeSwap`）：**

```
1. 相对位置放回
2. 大物原占用区域内扫描
3. 全背包 SetAtFirst 兜底
```

**跨容器注意点（已修复）：**

- `GetSwapPlan` 不再用 `aAnchorPos == bAnchorPos` 判非法（不同容器坐标可相同）
- 边界检查只验证目标物 `bAnchorPos` 在当前容器网格内

### 1.4 数据类

| 文件 | 角色 |
|---|---|
| `IItemRt.cs` | Core 只依赖此接口 |
| `ItemRtData.cs` | 运行时实例（锚点 / 旋转 / 堆叠） |
| `ItemSaveData.cs` | 存档快照（与 ItemRtData 一一对应） |
| `Scripts/Data/TableData/ItemBaseData.cs` | 配表模板（静态） |

---

## 2. Runtime 桥接层（15 min）

**文件：** `Scripts/Runtime/Logic/GridInventoryService.cs`

算法层与 View 层的桥梁，对外统一返回 `InventoryOpReport`。

### 三个核心入口

| 方法 | 场景 |
|---|---|
| `TryPlaceItem` | 同容器放下（直放 / 堆叠 / 交换） |
| `TryReceiveItem` | 跨容器 B 侧接收拖入物 |
| `TryReceiveSwapReturnItem` | 跨容器 A 侧接收换回来的物 |

### 预览判定

| 方法 | 用途 |
|---|---|
| `JudgeDragPreviewState` | 拖拽 footprint 绿 / 红依据 |
| `SetAnchorAndPlaceItem` | 设置锚点并占格（锚点由 Core 同步，此处不手动 SetAnchorPos） |

### 操作结果结构

**文件：** 同文件内 `InventoryOpReport`

| 字段 | 含义 |
|---|---|
| `ItemDataA` | 拖动物（堆叠满可能为 null） |
| `ItemDataB` | 被交换物 |
| `DisplacedItemDataList` | 大换小被挤开列表 |
| `SwapState` | Same / LargeToSmall / SmallToLarge |

---

## 3. View 层（20 min）

View 负责 **输入 + 表现**，算法全在 Core。

### 3.1 入口

**文件：** `Scripts/Runtime/View/GridContainer/ContainerMainPart/GridMainContainerView.cs`

- 生命周期 `Start` → `Init` + `RegisterSceneItemViews`
- 对外 API：`CreatItemUI` / `DestroyItemUI`
- 拖拽回调：`OnBeginDrag` / `OnDragging` / `OnEndDrag`

### 3.2 四个 Partial + 会话类

| 文件 | 看什么 |
|---|---|
| `GridDragSession.cs` | 拖拽会话状态（Begin / Clear） |
| `GridMainContainerView.Behaviour.cs` | 拖拽 ABC + 跨容器 E |
| `GridMainContainerView.Compute.cs` | 坐标换算、`GetPreviewAnchorPos`、回滚 |
| `GridMainContainerView.ViewTools.cs` | footprint 高亮、滚轮、外部预览 |

### 3.3 拖拽一条线跟到底

```
ItemView 发起拖拽
  │
  ├─ BeginDragHandler
  │    TryRemoveItem（从网格移除）
  │    dragSession.Begin(...)
  │    挂到 Canvas
  │
  ├─ DraggingHandler
  │    算 previewAnchorPos = mousePos - dragStartOffset
  │    同容器 → 本容器预览
  │    跨容器 → hoverContainer.HandleForeignDragPreview
  │
  └─ EndDragHandler
       ├─ 同容器 → HandleLocalEndDrag → TryPlaceItem
       └─ 跨容器 → HandleCrossContainerEndDrag
            ├─ B.TryReceiveItem
            ├─ A.TryReceiveSwapReturnItem
            └─ 失败 → 双容器 RestoreSnapshot + RollbackDragItem
```

### 3.4 容器管理

**文件：** `Scripts/Runtime/View/GridContainerMgr/GridMainContainerManager.cs`

- `TryResolveHoverContainer`：鼠标当前悬停哪个背包容器

---

## 4. 编辑器工具（10 min · 验证理解）

**文件：** `Scripts/Core/Editor/GridInventoryDebugWindow.cs`

- 不跑完整 UI 也能测 Core 放置 / 交换
- 适合验证算法改动是否正确

**文件：** `Scripts/Data/Editor/`、`Scripts/Runtime/Editor/`

- 物品配表编辑、View 编辑窗口

---

## 5. 推荐阅读顺序（清单）

按顺序打开，每文件只抓上表提到的点即可：

```
□ 1.  InventoryState.cs
□ 2.  InventoryState.PlacementService.cs
□ 3.  InventoryState.SwapService.cs          （GetSwapPlan → 三个 Swap 分支）
□ 4.  ItemRtData.cs + IItemRt.cs + ItemSaveData.cs
□ 5.  GridInventoryService.cs                 （三个 Try 入口 + JudgeDragPreviewState）
□ 6.  GridDragSession.cs
□ 7.  GridMainContainerView.Behaviour.cs     （Begin → Drag → End）
□ 8.  GridMainContainerView.Compute.cs        （GetPreviewAnchorPos）
□ 9.  GridInventoryDebugWindow.cs            （可选，验证 Core）
```

---

## 6. 带问题跳转（常用搜索词）

| 你想搞懂的事 | 直接搜 / 跳 |
|---|---|
| 能不能放这里 | `CanPlace` |
| 预览为什么变红 | `JudgeDragPreviewState` |
| 大换小后小物放哪 | `TryPlaceLittleItemAfterLargeSwap` |
| 跨容器失败怎么回滚 | `CaptureSnapshot` / `RestoreSnapshot` |
| 锚点谁负责写 | `SetItemData` |
| 交换分几种 | `GetSwapPlan` / `ESwapState` |
| 跨容器坐标 (0,0) 问题 | `GetSwapPlan` 内注释 |
| 拖拽状态存在哪 | `GridDragSession` |

---

## 7. 改代码前三层自检

改任何功能前，先确认动的是哪一层：

```
View 层：算什么锚点、怎么显示
    ↓
Service 层：调哪个 Try、返回什么 Report
    ↓
Core 层：改哪两个数组、要不要快照回滚
```

**规则：** 算法改 Core，表现改 View，Core 不要依赖 View。

---

## 8. 近期重构备忘（避免踩旧印象）

| 改动 | 说明 |
|---|---|
| 锚点唯一写入口 | `SetItemData` 内同步 `SetAnchorPos`，外层不再手动写 |
| 快照事务 | `CommitSwap` / 跨容器回滚用 `CaptureSnapshot` |
| ItemRtData 搬到 Core | 路径 `Scripts/Core/ItemRtData.cs` |
| ItemSaveData 独立文件 | `Scripts/Core/ItemSaveData.cs` |
| GridDragSession | 拖拽字段收进会话类 |
| 跨容器 GetSwapPlan | 不再因坐标相同判 CanNotSwap |

---

*临时文档，熟悉后可删。*
