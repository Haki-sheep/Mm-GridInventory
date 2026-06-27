using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 此脚本放置行为方法 是View层的流程控制 
    /// ABC就是物品拖拽的三个阶段 D是物品旋转
    /// </summary>
    public partial class GridMainContainerView
    {
        // 拖动运行时状态
        private bool isDragging = false;

        /// <summary> 拖拽中的物品 </summary>
        private ItemView draggingItem;

        /// <summary> 拖拽开始时候的锚点 </summary>
        private Vector2Int dragStartAnchorPos;
        /// <summary> 吸附框拖拽状态缓存 避免重复计算吸附框状态 </summary>
        private Vector2Int cachedFrameBoardAnchorPos = Vector2Int.zero;
        /// <summary> 拖拽物起始层级 </summary>
        private int dragStartSiblingIndex = -1;
        /// <summary>
        /// 拖拽物起始旋转状态
        /// </summary>
        private bool dragStartIsRotated = false;

        // 下面两个变量遵从一个公式:
        // 抓取时:抓取相对偏移 = 鼠标位置 - 锚点位置
        // 那么放置时:锚点位置 = 鼠标位置 - 抓取相对偏移
        /// <summary> 抓取相对偏移 </summary>
        private Vector2Int dragStartOffset;

        [SerializeField, ReadOnly, LabelText("预览锚点")]
        private Vector2Int dragPreviewAnchorPos;

        /// <summary> 当前高亮格子索引 </summary>
        private int curHighLightCellIndex = -1;

        /// <summary> 鼠标从哪个容器开始拖拽的 </summary>
        private GridMainContainerView aContainer;
        /// <summary> 鼠标悬停的背包容器 可能是新容器b 也可能是起始的a </summary>
        private GridMainContainerView bContainer;

        #region A

        private bool BeginDragHandler(ItemView itemView, PointerEventData eventData)
        {
            if (itemView is null || itemView.ItemData is null) return false;
            if (!TryGetMouseInGridInfo(eventData.position, out var mouseOnGridPos, out _)) return false;

            // 设置拖拽物
            draggingItem = itemView;

            // 禁用Rect滚动
            if (ScrollRect is not null) ScrollRect.enabled = false;

            // 设置拖拽物起始锚点
            dragStartAnchorPos = itemView.ItemData.AnchorPos;
            dragStartIsRotated = itemView.ItemData.IsRotated;

            // 物品拖拽的预览锚点 = 物品起始锚点
            dragPreviewAnchorPos = dragStartAnchorPos;

            // 抓取相对偏移 = 鼠标按下时所在锚点 - 物品起始锚点
            dragStartOffset = mouseOnGridPos - dragStartAnchorPos;

            // 移除被抓取物品
            if (!gridInventoryService.TryRemoveItem(dragStartAnchorPos).IsSuccess)
            {
                if (ScrollRect is not null) ScrollRect.enabled = true;
                draggingItem = null;
                return false;
            }

            // 挂到 Canvas 脱离 scrollContent 避免滚轮滚动时物品跟着跳
            dragStartSiblingIndex = draggingItem.ItemRectTransform.GetSiblingIndex();
            draggingItem.ItemRectTransform.SetParent(Canvas.transform, true);

            aContainer = this;
            bContainer = null;

            // 设置拖拽层级
            HandlerFrameBoardState(EOnDragState.OnBeginDrag);
            SetTransformSibingIndex(EOnDragState.OnBeginDrag);
            return true;
        }

        #endregion

        #region B
        private void DraggingHandler(PointerEventData eventData)
        {
            // 物品跟随鼠标
            if (draggingItem is null)
                return;
            draggingItem.ItemRectTransform.position = eventData.position;

            // 解析当前悬停容器
            var lastBContainer = bContainer;
            var hasHit = GridMainContainerManager.TryResolveHoverContainer(
                eventData.position,
                out bContainer,
                out var mouseOnGridPos,
                out var gridIndex);

            // 如果什么容器都没命中 则清除预览
            if (!hasHit)
            {
                lastBContainer?.ClearDragPreview();
                ClearDragPreview();
                return;
            }

            // 悬停在 B 容器且 B 不是 A 则在外部容器显示预览
            if (bContainer != aContainer)
            {
                lastBContainer?.ClearDragPreview();

                bContainer.HandleForeignDragPreview(
                        draggingItem, dragStartOffset, mouseOnGridPos, gridIndex);
                return;
            }

            // 设置高亮格子
            SetCellHighlight(gridIndex);
            // 设置拖拽过程中物品和吸附框在UI中的层级
            SetTransformSibingIndex(EOnDragState.OnDragging);

            // 更新预览锚点
            dragPreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragStartOffset, draggingItem.ItemData);

            // 如果与上一帧相同则不进行后续操作则不更新吸附框状态
            if (cachedFrameBoardAnchorPos == dragPreviewAnchorPos)
                return;
            cachedFrameBoardAnchorPos = dragPreviewAnchorPos;

            // 设置吸附框状态
            HandlerFrameBoardState(EOnDragState.OnDragging);
        }

        #endregion

        #region C
        private void EndDragHandler(PointerEventData eventData)
        {
            if (draggingItem is null) return;

            // 解析落点容器
            if (GridMainContainerManager.TryResolveHoverContainer(
                    eventData.position,
                    out bContainer,
                    out var mouseOnGridPos,
                    out _))
            {
                dragPreviewAnchorPos = bContainer.GetPreviewAnchorPos(
                    mouseOnGridPos, dragStartOffset, draggingItem.ItemData);
            }
            else
            {
                dragPreviewAnchorPos = dragStartAnchorPos;
                bContainer = aContainer;
            }

            aContainer.ClearDragPreview();

            // 跨容器交换逻辑
            if (bContainer != aContainer)
            {
                bContainer.ClearDragPreview();
                HandleCrossContainerEndDrag(aContainer, bContainer);
            }
            // 同容器交换逻辑
            else
                HandleLocalEndDrag();

            // 清除高亮格子
            ClearCellHighlight();
            // 设置吸附框状态
            HandlerFrameBoardState(EOnDragState.OnEndDrag);
            // 设置拖拽过程中物品和吸附框在UI中的层级
            SetTransformSibingIndex(EOnDragState.OnEndDrag);

            if (ScrollRect is not null)
                ScrollRect.enabled = true;
            curHighLightCellIndex = -1;
            draggingItem = null;
            dragStartSiblingIndex = -1;
            dragStartIsRotated = false;
            aContainer = null;
            bContainer = null;
        }


        #endregion


        #region D

        /// <summary>
        /// 处理拖拽物品的旋转
        /// 此方法在Update实时调用
        /// </summary>
        private void HandleDraggingItemRotation()
        {
            // TODO:接入Input
            if (isDragging && Input.GetKeyDown(KeyCode.R))
            {
                if (draggingItem is null) return;

                // 获取旋转物品后的报告
                var result = gridInventoryService.TryRotateItem(draggingItem.ItemData);
                if (!result.IsSuccess) return;

                var itemDataA = result.ItemDataA;

                // 设置UI旋转表现
                draggingItem.ItemRectTransform.localRotation =
                                        Quaternion.Euler(0, 0, itemDataA.IsRotated ? 90f : 0f);

                // 获取鼠标所在容器并刷新对应容器的预览
                if (!GridMainContainerManager.TryResolveHoverContainer(
                        Input.mousePosition,
                        out var hoverContainer,
                        out var mouseOnGridPos,
                        out var gridIndex))
                    return;

                cachedFrameBoardAnchorPos = new Vector2Int(int.MinValue, int.MinValue);
                if (hoverContainer != aContainer)
                {
                    bContainer = hoverContainer;
                    ClearDragPreview();
                    hoverContainer.HandleForeignDragPreview(
                        draggingItem,
                        dragStartOffset,
                        mouseOnGridPos,
                        gridIndex);
                    return;
                }

                // 获取预览锚点位置
                dragPreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragStartOffset, draggingItem.ItemData);
                HandlerFrameBoardState(EOnDragState.OnDragging);
                cachedFrameBoardAnchorPos = dragPreviewAnchorPos;
            }

        }
        #endregion


        #region E 跨容器交换逻辑
        /// <summary>
        /// 同容器内结束拖拽
        /// </summary>
        private void HandleLocalEndDrag()
        {
            var itemView = draggingItem;
            itemView.ItemRectTransform.SetParent(itemContent, true);

            var result = gridInventoryService.TryPlaceItem(
                itemView.ItemData, dragStartAnchorPos, dragPreviewAnchorPos);

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
        /// <param name="aContainer">起始容器</param>
        /// <param name="bContainer">落点容器</param>
        private void HandleCrossContainerEndDrag(GridMainContainerView aContainer,
                                                 GridMainContainerView bContainer)
        {
            // 记录拖拽物 然后让落点容器尝试接收
            var aitemView = draggingItem;
            var result = bContainer.gridInventoryService.TryReceiveItem(
                aitemView.ItemData, dragPreviewAnchorPos);

            // 落点容器接收失败 回滚拖拽物
            if (!result.IsSuccess)
            {
                aContainer.RollbackDragItem(aitemView);
                return;
            }

            // 解析落点容器接收结果 newA/B 引用仍是原 A/B 对象 锚点与占格已是 B 侧处理后的状态
            var newA = result.ItemDataA;
            var newB = result.ItemDataB;

           // A 侧数据层接收 B 换回来的物
            if (!aContainer.gridInventoryService.TryReceiveSwapReturnItem(result,
                                                                          aContainer.dragStartAnchorPos,
                                                                          bContainer.dragPreviewAnchorPos))
            {
                // b和a都回滚
                bContainer.gridInventoryService.TryRemoveItem(newA.AnchorPos);
                bContainer.gridInventoryService.SetAnchorAndPlaceItem(newB, newB.AnchorPos);
                aContainer.RollbackDragItem(aitemView);
                return;
            }

            // 互换a和b的字典管理数据
            aContainer.RemoveItemView(aitemView);
            bContainer.AddItemView(aitemView);

            // 跨容器堆叠满了的时候 newA会被销毁
            if (newA is null)
            {
                Destroy(aitemView.gameObject);
                return;
            }

            // 换走的
            aitemView.ItemRectTransform.localPosition =
                bContainer.GetItemUIPivotPos(newA.AnchorPos, newA.DataSize);

            // 换回来的
            aContainer.ApplyCrossContainerReturnViews(result, bContainer);
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
