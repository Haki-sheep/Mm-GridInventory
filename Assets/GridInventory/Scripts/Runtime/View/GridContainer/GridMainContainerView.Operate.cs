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

    public partial class GridMainContainerView : MonoBehaviour, 
                                    IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        [Header("运行时信息")]
        // 鼠标按下的物品
        [SerializeField] private ItemView activeItem;

        // 拖动运行时状态
        private bool isDragging = false;

        // Item - 物品信息相关
        private RectTransform draggingItemRectTransform;
        private ItemView draggingItem;

        /// Drag - 拖拽信息相关
        private Vector2Int dragStartAnchorPos;
        // 拖拽缓存
        private bool hasFrameBoardStateCache = false;
        // 拖拽缓存锚点
        private Vector2Int cachedFrameBoardAnchorPos = new Vector2Int(int.MinValue, int.MinValue);
        // 拖拽物起始层级
        private int dragStartSiblingIndex = -1;

        [SerializeField, ReadOnly, LabelText("预览锚点")]
        private Vector2Int dragPreviewAnchorPos;

        // 抓取相对偏移 鼠标格减去物品锚点
        private Vector2Int dragStartOffset;

        // View - 高亮格子与吸附框
        private int curHighLightCellIndex = -1;


        #region ItemEvent

        public void AddItemEventListener(ItemView itemView)
        {
            itemView.OnPointerEnterEvent += OnItemEnter;
            itemView.OnPointerExitEvent += OnItemExit;
            itemView.OnPointerDownEvent += OnItemSelect;
        }

        public void RemoveItemEventListener(ItemView itemView)
        {
            itemView.OnPointerEnterEvent -= OnItemEnter;
            itemView.OnPointerExitEvent -= OnItemExit;
            itemView.OnPointerDownEvent -= OnItemSelect;
        }
        private void OnItemEnter(ItemView itemView) => activeItem = itemView;
        private void OnItemSelect(ItemView itemView) => activeItem = itemView;
        private void OnItemExit(ItemView itemView)
        {
            if (isDragging) return;
            activeItem = null;
        }
        #endregion

        #region A

        private void BeginDragHandler(PointerEventData eventData)
        {

            if (activeItem is null) return;
            if (!TryGetMousePosInGrid(eventData.position, out var mouseOnGridPos, out _)) return;
            
            // 选中物品音效
            if(IGridAudioAndAnimation is not null)
            {
                IGridAudioAndAnimation.OnSelectItem();
            }

            // 设置拖拽物品信息
            draggingItem = activeItem;
            draggingItemRectTransform = activeItem.ItemRectTransform;
            dragStartSiblingIndex = draggingItemRectTransform.GetSiblingIndex();

            // 禁用父容器滚动
            scrollRect.enabled = false;

            // 物品拖拽的起始锚点
            dragStartAnchorPos = activeItem.ItemData.AnchorPos;

            // 物品拖拽的预览锚点 = 物品起始锚点
            dragPreviewAnchorPos = dragStartAnchorPos;
            hasFrameBoardStateCache = false;

            // 抓取相对偏移 = 鼠标按下时所在锚点 - 物品起始锚点
            dragStartOffset = mouseOnGridPos - dragStartAnchorPos;

            if (!inventoryViewModel.TryRemoveItem(dragStartAnchorPos).IsSuccess) return;

            // frameBoard 永远最高，拖拽物次高
            draggingItemRectTransform.SetAsLastSibling();
            frameBoardView.transform.SetAsLastSibling();
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
            draggingItemRectTransform.SetAsLastSibling();
            draggingItemRectTransform.position = eventData.position;

            // 计算网格坐标
            if (!TryGetMousePosInGrid(eventData.position, out var mouseOnGridPos, out var gridIndex))
            {
                ClearCellHighlight();
                frameBoardView.SetFrameBoardView(EFrameBoard.Hidden, Vector2.zero, Vector2.zero);
                hasFrameBoardStateCache = false;
                return;
            }

            // 高亮格子
            if (curHighLightCellIndex != gridIndex)
            {
                if (curHighLightCellIndex != -1)
                    gridCellViews[curHighLightCellIndex].SetBkHighLight(false);

                gridCellViews[gridIndex].SetBkHighLight(true);
                curHighLightCellIndex = gridIndex;
            }

            dragPreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragStartOffset);

            // 预览锚点未变化时，直接复用上一帧吸附框状态，避免重复判定。
            if (hasFrameBoardStateCache && cachedFrameBoardAnchorPos == dragPreviewAnchorPos)
                return;

            // 改动
            var newItemData = inventoryViewModel.GetItemAt(dragPreviewAnchorPos);

            // 吸附框
            var framePos = GetFrameBoardTransform(draggingItem.ItemData, dragPreviewAnchorPos);
            var frameSize = GetItemUISize(draggingItem.ItemData.DataSize);

            SetFrameBoardState(draggingItem.ItemData,
                               newItemData,
                               dragPreviewAnchorPos,
                               framePos,
                               frameSize);
            cachedFrameBoardAnchorPos = dragPreviewAnchorPos;
            hasFrameBoardStateCache = true;
            frameBoardView.transform.SetAsLastSibling();
        }

        #endregion

        #region C
        private void EndDragHandler(PointerEventData eventData)
        {
            if (draggingItem is null) return;

            // 松手时物品音效
            if(IGridAudioAndAnimation is not null)
            {
                IGridAudioAndAnimation.OnDeselectItem();
            }

            if (!TryGetMousePosInGrid(eventData.position, out var mouseOnGridPos, out _))
                dragPreviewAnchorPos = dragStartAnchorPos;
            else
                dragPreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragStartOffset);

            var result = inventoryViewModel.TryPlaceItem(draggingItem.ItemData,
                                                         dragStartAnchorPos,
                                                         dragPreviewAnchorPos);

            var oldItemData = result.OldItemData;
            var newItemData = result.NewItemData;
            var oldItemDataList = result.OldItemDataList;

            // 如果放置失败 原物品复位
            if (!result.IsSuccess)
            {
                draggingItemRectTransform.localPosition = GetItemUIPos(oldItemData.AnchorPos, oldItemData.DataSize);
            }
            // 堆叠成功：被拖拽物被合并并销毁
            else if (oldItemData is null)
            {
                itemViewDict.Remove(draggingItem.ItemData.InstancedItemId);
                Destroy(draggingItem.gameObject);
            }
            // 放置/交换成功：先更新拖拽物，再按需更新目标物
            else
            {
                draggingItemRectTransform.localPosition = GetItemUIPos(oldItemData.AnchorPos, oldItemData.DataSize);

                if (newItemData is not null && itemViewDict.TryGetValue(newItemData.InstancedItemId, out var targetItemView))
                {
                    targetItemView.ItemRectTransform.localPosition = GetItemUIPos(newItemData.AnchorPos, newItemData.DataSize);
                }

                if (oldItemDataList is not null && oldItemDataList.Count > 0)
                {
                    foreach (var itemData in oldItemDataList)
                    {
                        if (itemViewDict.TryGetValue(itemData.InstancedItemId, out var itemView))
                        {
                            if (itemView is null) continue;
                            itemView.ItemRectTransform.localPosition = GetItemUIPos(itemData.AnchorPos, itemData.DataSize);
                        }
                    }
                }

            }

            // 清除高亮格子
            ClearCellHighlight();

            // 清除吸附框
            var normalPos = GetFrameBoardTransform(draggingItem.ItemData, dragPreviewAnchorPos);
            var normalSize = GetItemUISize(draggingItem.ItemData.DataSize);
            frameBoardView.SetFrameBoardView(EFrameBoard.Normal, normalPos, normalSize);

            // 恢复拖拽物层级，避免拖拽后长期处于高层
            if (draggingItemRectTransform is not null)
            {
                int maxIndex = Mathf.Max(0, draggingItemRectTransform.parent.childCount - 1);
                int safeIndex = Mathf.Clamp(dragStartSiblingIndex, 0, maxIndex);
                draggingItemRectTransform.SetSiblingIndex(safeIndex);
            }

            // 吸附框保持最高层级。
            frameBoardView.transform.SetAsLastSibling();

            activeItem = null;
            draggingItemRectTransform = null;
            scrollRect.enabled = true;
            curHighLightCellIndex = -1;
            draggingItem = null;
            dragStartSiblingIndex = -1;
            hasFrameBoardStateCache = false;
        }
        #endregion

        /// <summary>
        /// 清除高亮格子
        /// </summary>
        private void ClearCellHighlight()
        {
            if (curHighLightCellIndex >= 0)
            {
                gridCellViews[curHighLightCellIndex].SetBkHighLight(false);
                curHighLightCellIndex = -1;
                return;
            }

            foreach (var cellView in gridCellViews)
            {
                cellView.SetBkHighLight(false);
            }
        }


        #region D
        private void HandleDraggingItemRotation()
        {
            if (isDragging && Input.GetKeyDown(KeyCode.R))
            {
                if (draggingItem is null || draggingItemRectTransform is null) return;

                var result = inventoryViewModel.TryRotateItem(draggingItem.ItemData);
                if (!result.IsSuccess) return;

                var itemData = result.OldItemData;

                // 旋转物品
                draggingItemRectTransform.localRotation =
                                        Quaternion.Euler(0, 0, itemData.IsRotated ? 90f : 0f);

                if (!TryGetMousePosInGrid(Input.mousePosition, out var mouseOnGridPos, out _)) return;
                dragPreviewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos, dragStartOffset);
                var pos = GetFrameBoardTransform(itemData, dragPreviewAnchorPos);
                var size = GetItemUISize(itemData.DataSize);

                SetFrameBoardState(itemData,
                                   inventoryViewModel.GetItemAt(dragPreviewAnchorPos),
                                   dragPreviewAnchorPos,
                                   pos,
                                   size);
                cachedFrameBoardAnchorPos = dragPreviewAnchorPos;
                hasFrameBoardStateCache = true;
            }

        }
        #endregion
    }
}
