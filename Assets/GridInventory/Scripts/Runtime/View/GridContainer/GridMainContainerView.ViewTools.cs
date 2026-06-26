using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MmInventory
{
    public enum EOnDragState
    {
        None,
        OnBeginDrag,
        OnDragging,
        OnEndDrag,
    }

    public partial class GridMainContainerView
    {

        private void SetCellHighlight(int cellIndex)
        {
            if (cellIndex == -1) return;
            // 设置高亮格子 如果当前格子与上一帧格子不同 则设置高亮格子
            if (curHighLightCellIndex != cellIndex )
            {
                if(curHighLightCellIndex != -1)
                    gridCellViews[curHighLightCellIndex].SetBkHighLight(false);
                gridCellViews[cellIndex].SetBkHighLight(true);

                curHighLightCellIndex = cellIndex;
            }
        }
        /// <summary>
        /// 清除所有高亮格子
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


        /// <summary>
        /// 处理吸附框状态
        /// </summary>
        /// <param name="state"></param>
        private void FrameStateHandler(EOnDragState state)
        {
            if (frameBoardView is null) return;

            switch (state)
            {
                case EOnDragState.None:
                    frameBoardView.SetFrameBoardView(EFrameBoard.Hidden, Vector2.zero, Vector2.zero);
                    break;

                case EOnDragState.OnBeginDrag:
                case EOnDragState.OnDragging:
                case EOnDragState.OnEndDrag:
                    if (draggingItem is null || draggingItem.ItemData is null) return;

                    var itemDataB = gridInventoryService.GetItemAt(dragPreviewAnchorPos);
                    var framePos = GetFrameBoardTransform(draggingItem.ItemData, dragPreviewAnchorPos);
                    var frameSize = GetItemUISize(draggingItem.ItemData.DataSize);

                    if (state == EOnDragState.OnBeginDrag)
                    {
                        frameBoardView.SetFrameBoardView(EFrameBoard.Normal, framePos, frameSize);
                        break;
                    }

                    if (state == EOnDragState.OnEndDrag)
                    {
                        frameBoardView.SetFrameBoardView(EFrameBoard.Hidden, framePos, frameSize);
                        break;
                    }

                    SetFrameBoardState(draggingItem.ItemData,
                                       itemDataB,
                                       dragPreviewAnchorPos,
                                       framePos,
                                       frameSize);
                    break;
            }
        }

        /// <summary>
        /// 设置拖拽过程中物品和吸附框在UI中的层级
        /// </summary>
        /// <param name="state"></param>
        private void SetIndexInUI(EOnDragState state)
        {
            switch (state)
            {
                // 拖拽起来的时候 将被拖拽物放在最上层
                case EOnDragState.OnBeginDrag:
                    dragStartSiblingIndex = draggingItemRectTransform.GetSiblingIndex();
                    draggingItemRectTransform.SetAsLastSibling();
                    frameBoardView.transform.SetAsLastSibling();
                    break;
                // 吸附框在次最上层
                case EOnDragState.OnDragging:
                    frameBoardView.transform.SetAsLastSibling();
                    break;
                // 放置的时候 将物品放在原来的层级 吸附框重置于最上层

                case EOnDragState.OnEndDrag:
                    if (draggingItemRectTransform is not null)
                    {
                        int maxIndex = Mathf.Max(0, draggingItemRectTransform.parent.childCount - 1);
                        int safeIndex = Mathf.Clamp(dragStartSiblingIndex, 0, maxIndex);
                        draggingItemRectTransform.SetSiblingIndex(safeIndex);
                    }

                    // 吸附框保持最高层级。
                    frameBoardView.transform.SetAsLastSibling();
                    break;
            }
        }

        private void SetFrameBoardState(ItemRtData itemDataA,
                                        ItemRtData itemDataB,
                                        Vector2Int dragPreviewAnchorPos,
                                        Vector2 pos,
                                        Vector2 size)
        {
            if (frameBoardView is null) return;

            var state = gridInventoryService.JudgeFrameBoardState(itemDataA,
                                                                  itemDataB,
                                                                  dragPreviewAnchorPos);

            frameBoardView.SetFrameBoardView(state, pos, size);
        }

    }
}