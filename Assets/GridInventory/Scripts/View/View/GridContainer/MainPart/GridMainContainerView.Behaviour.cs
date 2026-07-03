using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 此脚本放置行为方法 是View层的流程控制 
    /// ABC就是物品拖拽的三个阶段 D是物品旋转
    /// </summary>
    public partial class GridMainContainerView
    {
        /// <summary> 拖拽会话 </summary>
        private readonly GridDragSession dragSession = new();

        /// <summary> 当前高亮格子索引 </summary>
        private int curHighLightCellIndex = -1;

        /// <summary> footprint 预览高亮格子索引列表 </summary>
        private readonly List<int> previewFootprintCellIndexList = new();

        [ShowInInspector, ReadOnly, LabelText("预览锚点")]
        private Vector2Int DebugPreviewAnchorPos => dragSession.PreviewAnchorPos;

        #region A 拿起

        private bool BeginDragHandler(ItemView itemView, PointerEventData eventData)
        {
            if (itemView is null || itemView.ItemData is null) return false;
            if (!TryGetMouseInGridInfo(eventData.position, out var mouseOnGridPos, out _)) return false;

            var startAnchorPos = itemView.ItemData.AnchorPos;
            var startIsRotated = itemView.ItemData.IsRotated;
            // 记录鼠标相对物品锚点的偏移 后续算预览锚点用
            var startOffset = mouseOnGridPos - startAnchorPos;

            // 数据层先 RemoveAt 腾出占格 失败则不进入拖拽
            if (!gridInventoryService.TryRemoveItem(startAnchorPos).IsSuccess)
                return false;

            // 拖拽期间禁用滚动 避免 ScrollRect 抢输入
            if (ScrollRect is not null) ScrollRect.enabled = false;

            dragSession.Begin(itemView,
                              startAnchorPos,
                              startIsRotated,
                              startOffset,
                              itemView.ItemRectTransform.GetSiblingIndex(),
                              this);

            // 挂到 Canvas 脱离 scrollContent 避免滚轮滚动时物品跟着跳
            dragSession.DraggingItem.ItemRectTransform.SetParent(Canvas.transform, true);

            HandlerDragPreview(EOnDragState.OnBeginDrag);
            SetTransformSibingIndex(EOnDragState.OnBeginDrag);
            return true;
        }

        #endregion

        #region B 拖拽中
        private void DraggingHandler(PointerEventData eventData)
        {
            if (dragSession.DraggingItem is null)
                return;

            // 物品 UI 跟随鼠标 不参与网格吸附
            dragSession.DraggingItem.ItemRectTransform.position = eventData.position;

            var lastHoverContainer = dragSession.HoverContainer;
            var hasHit = GridMainContainerManager.TryResolveHoverContainer(
                eventData.position,
                out var hoverContainer,
                out var mouseOnGridPos,
                out var gridIndex);

            // 鼠标不在任何容器上 清掉两侧预览
            if (!hasHit)
            {
                lastHoverContainer?.ClearDragPreview();
                ClearDragPreview();
                dragSession.HoverContainer = null;
                return;
            }

            dragSession.HoverContainer = hoverContainer;

            // 悬停在外部容器 由落点容器算 CrossContainer 预览
            if (hoverContainer != dragSession.SourceContainer)
            {
                lastHoverContainer?.ClearDragPreview();

                hoverContainer.HandleForeignDragPreview(
                        dragSession.DraggingItem,
                        dragSession.StartOffset,
                        mouseOnGridPos,
                        gridIndex,
                        dragSession);
                return;
            }

            SetTransformSibingIndex(EOnDragState.OnDragging);

            dragSession.PreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos,
                                                               dragSession.StartOffset,
                                                               dragSession.DraggingItem.ItemData);

            // 锚点未变则跳过 减少 footprint 重算
            if (dragSession.CachedPreviewAnchorPos == dragSession.PreviewAnchorPos)
                return;

            var previewAnchorPos = dragSession.PreviewAnchorPos;
            // 当前朝向放不下时尝试自动旋转一次
            TryAutoRotateForPreview(dragSession.DraggingItem,
                                    ref previewAnchorPos,
                                    mouseOnGridPos,
                                    dragSession.StartOffset,
                                    ESwapPlaceMode.SameContainer,
                                    dragSession);
            dragSession.PreviewAnchorPos = previewAnchorPos;

            if (dragSession.CachedPreviewAnchorPos == dragSession.PreviewAnchorPos)
                return;
            dragSession.CachedPreviewAnchorPos = dragSession.PreviewAnchorPos;

            HandlerDragPreview(EOnDragState.OnDragging);
        }

        #endregion

        #region C 放下
        private void EndDragHandler(PointerEventData eventData)
        {
            if (dragSession.DraggingItem is null) return;

            var sourceContainer = dragSession.SourceContainer;
            GridMainContainerView hoverContainer;

            // 根据松手位置确定落点容器与最终预览锚点
            if (GridMainContainerManager.TryResolveHoverContainer(
                    eventData.position,
                    out hoverContainer,
                    out var mouseOnGridPos,
                    out _))
            {
                dragSession.PreviewAnchorPos = hoverContainer.GetPreviewAnchorPos(
                    mouseOnGridPos, dragSession.StartOffset, dragSession.DraggingItem.ItemData);
            }
            else
            {
                // 落在空白处视为回到源容器原锚点
                dragSession.PreviewAnchorPos = dragSession.StartAnchorPos;
                hoverContainer = sourceContainer;
            }

            sourceContainer.ClearDragPreview();

            if (hoverContainer != sourceContainer)
            {
                hoverContainer.ClearDragPreview();
                HandleCrossContainerEndDrag(sourceContainer, hoverContainer);
            }
            else
                HandleLocalEndDrag();

            ClearCellHighlight();
            HandlerDragPreview(EOnDragState.OnEndDrag);
            SetTransformSibingIndex(EOnDragState.OnEndDrag);

            if (ScrollRect is not null)
                ScrollRect.enabled = true;
            curHighLightCellIndex = -1;
            dragSession.Clear();
        }


        #endregion


        #region D 旋转

        /// <summary>
        /// 处理拖拽物品的旋转
        /// 此方法在Update实时调用
        /// </summary>
        private void HandleDraggingItemRotation()
        {
            if (!dragSession.IsActive || Keyboard.current is null || !Keyboard.current.rKey.wasPressedThisFrame)
                return;

            if (dragSession.DraggingItem is null)
                return;

            var result = gridInventoryService.TryRotateItem(dragSession.DraggingItem.ItemData);
            if (!result.IsSuccess)
                return;

            // 玩家手动转过之后 关闭自动旋转
            dragSession.ManualRotationLocked = true;

            var itemDataA = result.ItemDataA;
            ApplyItemViewRotation(dragSession.DraggingItem, itemDataA.IsRotated);

            if (!GridMainContainerManager.TryResolveHoverContainer(
                    Input.mousePosition,
                    out var hoverContainer,
                    out var mouseOnGridPos,
                    out var gridIndex))
                return;

            // 旋转后 footprint 变了 强制刷新预览缓存
            dragSession.InvalidatePreviewCache();
            if (hoverContainer != dragSession.SourceContainer)
            {
                dragSession.HoverContainer = hoverContainer;
                ClearDragPreview();
                hoverContainer.HandleForeignDragPreview(
                    dragSession.DraggingItem,
                    dragSession.StartOffset,
                    mouseOnGridPos,
                    gridIndex,
                    dragSession);
                return;
            }

            dragSession.PreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos,
                                                               dragSession.StartOffset,
                                                               dragSession.DraggingItem.ItemData);
            HandlerDragPreview(EOnDragState.OnDragging);
            dragSession.CachedPreviewAnchorPos = dragSession.PreviewAnchorPos;
        }

        /// <summary>
        /// 预览锚点放不下时尝试自动旋转
        /// </summary>
        private bool TryAutoRotateForPreview(ItemView itemView,
                                             ref Vector2Int previewAnchorPos,
                                             Vector2Int mouseOnGridPos,
                                             Vector2Int dragOffset,
                                             ESwapPlaceMode swapPlaceMode,
                                             GridDragSession sourceDragSession)
        {
            if (sourceDragSession.ManualRotationLocked)
                return false;

            if (itemView?.ItemData is null)
                return false;

            var itemData = itemView.ItemData;
            // 正方形物品旋转无意义
            if (itemData.DataSize.x == itemData.DataSize.y)
                return false;

            if (IsPreviewPlaceable(itemView, previewAnchorPos, swapPlaceMode))
                return false;

            if (!gridInventoryService.TryRotateItem(itemData).IsSuccess)
                return false;

            previewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragOffset, itemData);
            if (IsPreviewPlaceable(itemView, previewAnchorPos, swapPlaceMode))
            {
                ApplyItemViewRotation(itemView, itemData.IsRotated);
                return true;
            }

            // 旋转后仍放不下 转回原朝向
            gridInventoryService.TryRotateItem(itemData);
            previewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragOffset, itemData);
            return false;
        }

        /// <summary>
        /// 当前预览锚点是否可放置
        /// </summary>
        private bool IsPreviewPlaceable(ItemView itemView,
                                        Vector2Int previewAnchorPos,
                                        ESwapPlaceMode swapPlaceMode)
        {
            var itemDataB = gridInventoryService.GetItemAt(previewAnchorPos);
            // 只问 Service 能否放 不在 View 里写放置算法
            var state = gridInventoryService.JudgeDragPreviewState(itemView.ItemData,
                                                                 itemDataB,
                                                                 previewAnchorPos,
                                                                 swapPlaceMode);
            return state != EDragPreviewState.CannotPlace;
        }

        /// <summary>
        /// 同步物品视图旋转
        /// </summary>
        private static void ApplyItemViewRotation(ItemView itemView, bool isRotated)
        {
            itemView.ItemRectTransform.localRotation = Quaternion.Euler(0, 0, isRotated ? 90f : 0f);
        }

        #endregion


        #region E 同容器与跨容器落点

        /// <summary>
        /// 同容器内结束拖拽
        /// </summary>
        private void HandleLocalEndDrag()
        {
            var itemView = dragSession.DraggingItem;
            itemView.ItemRectTransform.SetParent(itemContent, true);

            // 一次 TryPlaceItem 完成放 堆叠 交换 失败时 Service 内回滚
            var result = gridInventoryService.TryPlaceItem(
                itemView.ItemData, dragSession.StartAnchorPos, dragSession.PreviewAnchorPos);

            if (!result.IsSuccess)
            {
                RollbackDragItem(itemView);
                return;
            }

            var newItemDataA = result.ItemDataA;
            var newItemDataB = result.ItemDataB;
            var displacedItemDataList = result.DisplacedItemDataList;

            // 堆叠耗尽时 A 数据被消耗 销毁对应 ItemView
            if (newItemDataA is null)
            {
                itemViewDict.Remove(itemView.ItemData.InstancedItemId);
                Destroy(itemView.gameObject);
                return;
            }

            // 同步拖动物 UI 位置
            itemView.ItemRectTransform.localPosition =
                GetItemUIPivotPos(newItemDataA.AnchorPos, newItemDataA.DataSize);

            // 交换时同步被换物 B 的 UI
            if (newItemDataB is not null
                && itemViewDict.TryGetValue(newItemDataB.InstancedItemId, out var targetItemView))
            {
                targetItemView.ItemRectTransform.localPosition =
                    GetItemUIPivotPos(newItemDataB.AnchorPos, newItemDataB.DataSize);
            }

            // 大换小 同步被挤开小物的 UI
            if (displacedItemDataList is not null && displacedItemDataList.Count > 0)
            {
                foreach (var itemData in displacedItemDataList)
                {
                    if (itemViewDict.TryGetValue(itemData.InstancedItemId, out var displacedView))
                        displacedView.ItemRectTransform.localPosition =
                            GetItemUIPivotPos(itemData.AnchorPos, itemData.DataSize);
                }
            }
        }

        /// <summary>
        /// 处理跨容器交换
        /// </summary>
        /// <param name="sourceContainer">起始容器</param>
        /// <param name="hoverContainer">落点容器</param>
        private void HandleCrossContainerEndDrag(GridMainContainerView sourceContainer,
                                                 GridMainContainerView hoverContainer)
        {
            var aitemView = dragSession.DraggingItem;
            var dropAnchorPos = dragSession.PreviewAnchorPos;

            // Core 双背包事务 快照与 B→A 两步都在 Service 内完成
            var result = sourceContainer.gridInventoryService.TryCrossContainerDrop(
                hoverContainer.gridInventoryService,
                aitemView.ItemData,
                sourceContainer.dragSession.StartAnchorPos,
                dropAnchorPos);

            if (!result.IsSuccess)
            {
                // 失败只回滚拖拽物 UI 网格已由 Core 还原或未改动
                sourceContainer.RollbackDragItem(aitemView);
                return;
            }

            var newA = result.ItemDataA;

            // 拖动物 ItemView 从源容器字典迁到落点容器
            sourceContainer.RemoveItemView(aitemView);
            hoverContainer.AddItemView(aitemView);

            // 跨容器堆叠满时 A 被消耗 newA 为 null
            if (newA is null)
            {
                Destroy(aitemView.gameObject);
                return;
            }

            // 拖动物落到 B 侧新锚点
            aitemView.ItemRectTransform.localPosition =
                hoverContainer.GetItemUIPivotPos(newA.AnchorPos, newA.DataSize);

            // B 换出的物或大换小小物 视图迁回 A 侧
            sourceContainer.ApplyCrossContainerReturnViews(result, hoverContainer);
        }

        /// <summary>
        /// 按交换类型迁移跨容器返回物视图
        /// </summary>
        private void ApplyCrossContainerReturnViews(InventoryOpReport result,
                                                    GridMainContainerView fromContainer)
        {
            switch (result.SwapState)
            {
                // 等量或小换大 单个 B 从 B 容器迁到 A 容器
                case ESwapState.Same:
                case ESwapState.SmallToLarge:
                    MoveSingleReturnItemView(result.ItemDataB, fromContainer);
                    break;

                // 大换小 多个小物逐个迁回 A
                case ESwapState.LargeToSmall:
                    MoveDisplacedItemViews(result, fromContainer);
                    break;
            }
        }

        /// <summary>
        /// 迁移等量或小换大的单个返回物视图
        /// </summary>
        /// <param name="itemDataB">返回物数据</param>
        /// <param name="fromContainer">起始容器</param>
        private void MoveSingleReturnItemView(ItemRtData itemDataB, GridMainContainerView fromContainer)
        {
            if (itemDataB is null
                || !fromContainer.itemViewDict.TryGetValue(itemDataB.InstancedItemId, out var swapView))
                return;

            // ItemView 仍挂在 B 容器字典 迁到当前 A 容器并刷新坐标
            fromContainer.RemoveItemView(swapView);
            this.AddItemView(swapView);

            swapView.ItemRectTransform.localPosition =
                GetItemUIPivotPos(itemDataB.AnchorPos, itemDataB.DataSize);
        }

        /// <summary>
        /// 迁移大换小被挤开物品视图
        /// </summary>
        private void MoveDisplacedItemViews(InventoryOpReport result,
                                            GridMainContainerView fromContainer)
        {
            if (result.DisplacedItemDataList is null || result.DisplacedItemDataList.Count == 0)
                return;

            foreach (var data in result.DisplacedItemDataList)
            {
                if (!fromContainer.itemViewDict.TryGetValue(data.InstancedItemId, out var displacedView))
                    continue;

                fromContainer.RemoveItemView(displacedView);

                AddItemView(displacedView);
                displacedView.ItemRectTransform.localPosition =
                    GetItemUIPivotPos(data.AnchorPos, data.DataSize);
            }
        }

        #endregion
    }
}
