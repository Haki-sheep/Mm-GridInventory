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
            var startOffset = mouseOnGridPos - startAnchorPos;

            // 移除被抓取物品
            if (!gridInventoryService.TryRemoveItem(startAnchorPos).IsSuccess)
                return false;

            // 禁用Rect滚动
            if (ScrollRect is not null) ScrollRect.enabled = false;

            dragSession.Begin(itemView,
                              startAnchorPos,
                              startIsRotated,
                              startOffset,
                              itemView.ItemRectTransform.GetSiblingIndex(),
                              this);

            // 挂到 Canvas 脱离 scrollContent 避免滚轮滚动时物品跟着跳
            dragSession.DraggingItem.ItemRectTransform.SetParent(Canvas.transform, true);

            // 设置拖拽层级
            HandlerDragPreview(EOnDragState.OnBeginDrag);
            SetTransformSibingIndex(EOnDragState.OnBeginDrag);
            return true;
        }

        #endregion

        #region B 拖拽中
        private void DraggingHandler(PointerEventData eventData)
        {
            // 物品跟随鼠标
            if (dragSession.DraggingItem is null)
                return;
            dragSession.DraggingItem.ItemRectTransform.position = eventData.position;

            // 解析当前悬停容器
            var lastHoverContainer = dragSession.HoverContainer;
            var hasHit = GridMainContainerManager.TryResolveHoverContainer(
                eventData.position,
                out var hoverContainer,
                out var mouseOnGridPos,
                out var gridIndex);

            // 如果什么容器都没命中 则清除预览
            if (!hasHit)
            {
                lastHoverContainer?.ClearDragPreview();
                ClearDragPreview();
                dragSession.HoverContainer = null;
                return;
            }

            dragSession.HoverContainer = hoverContainer;

            // 悬停在 B 容器且 B 不是 A 则在外部容器显示预览
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

            // 设置拖拽过程中物品在UI中的层级
            SetTransformSibingIndex(EOnDragState.OnDragging);

            // 更新预览锚点
            dragSession.PreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos,
                                                               dragSession.StartOffset,
                                                               dragSession.DraggingItem.ItemData);

            if (dragSession.CachedPreviewAnchorPos == dragSession.PreviewAnchorPos)
                return;

            var previewAnchorPos = dragSession.PreviewAnchorPos;
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

            // 解析落点容器
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
                dragSession.PreviewAnchorPos = dragSession.StartAnchorPos;
                hoverContainer = sourceContainer;
            }

            sourceContainer.ClearDragPreview();

            // 跨容器交换逻辑
            if (hoverContainer != sourceContainer)
            {
                hoverContainer.ClearDragPreview();
                HandleCrossContainerEndDrag(sourceContainer, hoverContainer);
            }
            // 同容器交换逻辑
            else
                HandleLocalEndDrag();

            // 清除高亮格子
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

            dragSession.ManualRotationLocked = true;

            var itemDataA = result.ItemDataA;
            ApplyItemViewRotation(dragSession.DraggingItem, itemDataA.IsRotated);

            if (!GridMainContainerManager.TryResolveHoverContainer(
                    Input.mousePosition,
                    out var hoverContainer,
                    out var mouseOnGridPos,
                    out var gridIndex))
                return;

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


        #region E 跨容器交换
        /// <summary>
        /// 同容器内结束拖拽
        /// </summary>
        private void HandleLocalEndDrag()
        {
            var itemView = dragSession.DraggingItem;
            itemView.ItemRectTransform.SetParent(itemContent, true);

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

            if (newItemDataA is null)
            {
                itemViewDict.Remove(itemView.ItemData.InstancedItemId);
                Destroy(itemView.gameObject);
                return;
            }

            itemView.ItemRectTransform.localPosition =
                GetItemUIPivotPos(newItemDataA.AnchorPos, newItemDataA.DataSize);

            if (newItemDataB is not null
                && itemViewDict.TryGetValue(newItemDataB.InstancedItemId, out var targetItemView))
            {
                targetItemView.ItemRectTransform.localPosition =
                    GetItemUIPivotPos(newItemDataB.AnchorPos, newItemDataB.DataSize);
            }

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
            // 记录拖拽物 先给两侧容器拍快照 失败时整体还原
            var aitemView = dragSession.DraggingItem;
            var aSnapshot = sourceContainer.gridInventoryService.CaptureSnapshot();
            var bSnapshot = hoverContainer.gridInventoryService.CaptureSnapshot();
            var dropAnchorPos = dragSession.PreviewAnchorPos;

            var result = hoverContainer.gridInventoryService.TryReceiveItem(
                aitemView.ItemData, dropAnchorPos);

            // 落点容器接收失败 回滚拖拽物
            if (!result.IsSuccess)
            {
                sourceContainer.RollbackDragItem(aitemView);
                return;
            }

            // 解析落点容器接收结果 newA/B 引用仍是原 A/B 对象 锚点与占格已是 B 侧处理后的状态
            var newA = result.ItemDataA;

           // A 侧数据层接收 B 换回来的物
            if (!sourceContainer.gridInventoryService.TryReceiveSwapReturnItem(result,
                                                                          sourceContainer.dragSession.StartAnchorPos,
                                                                          dropAnchorPos))
            {
                // 双容器整体还原到落点前状态 拖拽物由回滚方法放回
                sourceContainer.gridInventoryService.RestoreSnapshot(aSnapshot);
                hoverContainer.gridInventoryService.RestoreSnapshot(bSnapshot);
                sourceContainer.RollbackDragItem(aitemView);
                return;
            }

            // 互换a和b的字典管理数据
            sourceContainer.RemoveItemView(aitemView);
            hoverContainer.AddItemView(aitemView);

            // 跨容器堆叠满了的时候 newA会被销毁
            if (newA is null)
            {
                Destroy(aitemView.gameObject);
                return;
            }

            // 换走的
            aitemView.ItemRectTransform.localPosition =
                hoverContainer.GetItemUIPivotPos(newA.AnchorPos, newA.DataSize);

            // 换回来的
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
                case ESwapState.Same:
                case ESwapState.SmallToLarge:
                    MoveSingleReturnItemView(result.ItemDataB, fromContainer);
                    break;
                case ESwapState.LargeToSmall:
                    MoveDisplacedItemViews(result, fromContainer);
                    break;
            }
        }

        /// <summary>
        /// 迁移等量或小换大的单个返回物视图
        /// <param name="itemDataB">返回物数据</param>
        /// <param name="fromContainer">起始容器</param>
        /// </summary>
        private void MoveSingleReturnItemView(ItemRtData itemDataB, GridMainContainerView fromContainer)
        {
            if (itemDataB is null
                || !fromContainer.itemViewDict.TryGetValue(itemDataB.InstancedItemId, out var swapView))
                return;

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
