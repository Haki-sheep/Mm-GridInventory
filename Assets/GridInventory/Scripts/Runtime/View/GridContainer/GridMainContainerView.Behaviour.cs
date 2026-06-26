using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 此脚本用于背包容器挂载 用于处理各种拖拽事件
    /// </summary>

    public partial class GridMainContainerView 
    {
        // 拖动运行时状态
        private bool isDragging = false;

        // Item - 物品信息相关
        private RectTransform draggingItemRectTransform;
        private ItemView draggingItem;

        // 拖拽开始时候的锚点
        private Vector2Int dragStartAnchorPos;
        // 吸附框拖拽状态缓存 避免重复计算吸附框状态
        private Vector2Int cachedFrameBoardAnchorPos = Vector2Int.zero;
        // 拖拽物起始层级
        private int dragStartSiblingIndex = -1;

        // 下面两个变量遵从一个公式:
        // 抓取时:抓取相对偏移 = 鼠标位置 - 锚点位置
        // 那么放置时:锚点位置 = 鼠标位置 - 抓取相对偏移
        private Vector2Int dragStartOffset;

        [SerializeField, ReadOnly, LabelText("预览锚点")]
        private Vector2Int dragPreviewAnchorPos;

        // 当前高亮格子索引
        private int curHighLightCellIndex = -1;

        #region A

        private void BeginDragHandler(ItemView itemView, PointerEventData eventData)
        {
            if (itemView is null || itemView.ItemData is null) return;
            if (!TryGetMouseInGridInfo(eventData.position, out var mouseOnGridPos, out _)) return;

            // 设置拖拽物
            draggingItem = itemView;
            draggingItemRectTransform = itemView.ItemRectTransform;

            // 禁用Rect滚动
            if (scrollRect is not null) scrollRect.enabled = false;

            // 设置拖拽物起始锚点
            dragStartAnchorPos = itemView.ItemData.AnchorPos;

            // 物品拖拽的预览锚点 = 物品起始锚点
            dragPreviewAnchorPos = dragStartAnchorPos;

            // 抓取相对偏移 = 鼠标按下时所在锚点 - 物品起始锚点
            dragStartOffset = mouseOnGridPos - dragStartAnchorPos;

            // 移除被抓取物品
            if (!gridInventoryService.TryRemoveItem(dragStartAnchorPos).IsSuccess) return;

            // 设置拖拽层级
            FrameStateHandler(EOnDragState.OnBeginDrag);
            SetIndexInUI(EOnDragState.OnBeginDrag);
        }

        #endregion

        #region B
        private void DraggingHandler(PointerEventData eventData)
        {
            // 物品跟随鼠标
            if (draggingItemRectTransform is null)
            {
                // Debug.LogError("GridMainContainerView: 拖拽物品的RectTransform为空");
                return;
            }

            draggingItemRectTransform.position = eventData.position;

            // 计算网格坐标
            if (!TryGetMouseInGridInfo(eventData.position, out var mouseOnGridPos, out var gridIndex))
            {
                // 如果坐标转换失败 则清除高亮格子 并设置吸附框为隐藏状态
                ClearCellHighlight();
                FrameStateHandler(EOnDragState.None);
                return;
            }

            // 设置高亮格子
            SetCellHighlight(gridIndex);
            // 设置拖拽过程中物品和吸附框在UI中的层级
            SetIndexInUI(EOnDragState.OnDragging);

            // 预览锚点与省略计算吸附状态
            dragPreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragStartOffset);
            if (cachedFrameBoardAnchorPos == dragPreviewAnchorPos)
                return;
            cachedFrameBoardAnchorPos = dragPreviewAnchorPos;

            // 设置吸附框状态
            FrameStateHandler(EOnDragState.OnDragging);
        }

        #endregion

        #region C
        private void EndDragHandler(PointerEventData eventData)
        {
            if (draggingItem is null) return;

            // 计算预览锚点 如果坐标转换失败则恢复起始锚点 
            // 相当于取消放置
            if (!TryGetMouseInGridInfo(eventData.position, out var mouseOnGridPos, out _))
                dragPreviewAnchorPos = dragStartAnchorPos;
            // 不然则计算预览锚点
            else
                dragPreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragStartOffset);

            // 尝试放置物品
            var result = gridInventoryService.TryPlaceItem(draggingItem.ItemData,
                                                         dragStartAnchorPos,
                                                         dragPreviewAnchorPos);

            // 注意 这里的A表示在数据层已经放置了B的位置 A和B已经完全被交换
            var newItemDataA = result.ItemDataA;
            var newItemDataB = result.ItemDataB;
            var displacedItemDataList = result.DisplacedItemDataList;

            // 放置失败
            if (!result.IsSuccess)
            {
                var resetData = draggingItem.ItemData;
                // 数据位置重置
                resetData.SetAnchorPos(dragStartAnchorPos);
                gridInventoryService.PlaceItem(resetData, dragStartAnchorPos);
                // UI位置重置
                draggingItemRectTransform.localPosition = GetItemUIPivotPos(dragStartAnchorPos,
                                                                            resetData.DataSize);
            }
            // A被完全堆叠进B 销毁A的UI
            else if (newItemDataA is null)
            {
                itemViewDict.Remove(draggingItem.ItemData.InstancedItemId);
                Destroy(draggingItem.gameObject);
            }
            // 放置/交换成功
            else
            {
                // A对齐新锚点
                draggingItemRectTransform.localPosition =
                                GetItemUIPivotPos(newItemDataA.AnchorPos, newItemDataA.DataSize);

                // B对齐新锚点
                if (newItemDataB is not null
                            && itemViewDict.TryGetValue(newItemDataB.InstancedItemId, out var targetItemView))
                {
                    targetItemView.ItemRectTransform.localPosition = GetItemUIPivotPos(newItemDataB.AnchorPos, newItemDataB.DataSize);
                }

                // 被挤开的物品逐个对齐
                if (displacedItemDataList is not null && displacedItemDataList.Count > 0)
                {
                    foreach (var itemData in displacedItemDataList)
                    {
                        if (itemViewDict.TryGetValue(itemData.InstancedItemId, out var itemView))
                        {
                            if (itemView is null) continue;
                            itemView.ItemRectTransform.localPosition = GetItemUIPivotPos(itemData.AnchorPos, itemData.DataSize);
                        }
                    }
                }

            }

            // 清除高亮格子
            ClearCellHighlight();
            // 设置吸附框状态
            FrameStateHandler(EOnDragState.OnEndDrag);
            // 设置拖拽过程中物品和吸附框在UI中的层级
            SetIndexInUI(EOnDragState.OnEndDrag);

            draggingItemRectTransform = null;
            if (scrollRect is not null)
                scrollRect.enabled = true;
            curHighLightCellIndex = -1;
            draggingItem = null;
            dragStartSiblingIndex = -1;
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
                if (draggingItem is null || draggingItemRectTransform is null) return;

                // 获取旋转物品后的报告
                var result = gridInventoryService.TryRotateItem(draggingItem.ItemData);
                if (!result.IsSuccess) return;

                var itemDataA = result.ItemDataA;

                // 设置UI旋转表现
                draggingItemRectTransform.localRotation =
                                        Quaternion.Euler(0, 0, itemDataA.IsRotated ? 90f : 0f);

                // 获取鼠标在网格中的坐标
                if (!TryGetMouseInGridInfo(Input.mousePosition, out var mouseOnGridPos, out _)) 
                    return;

                // 获取预览锚点位置
                dragPreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragStartOffset);
                cachedFrameBoardAnchorPos = new Vector2Int(int.MinValue, int.MinValue);
                FrameStateHandler(EOnDragState.OnDragging);
                cachedFrameBoardAnchorPos = dragPreviewAnchorPos;
            }

        }
        #endregion
    }
}
