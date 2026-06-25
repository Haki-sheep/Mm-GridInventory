using UnityEngine;
using UnityEngine.EventSystems;

namespace MmInventory
{

    /// <summary>
    /// 此脚本放置工具方法 一般不提供给外部使用
    /// </summary>

    public partial class GridMainContainerView : MonoBehaviour
    {
        #region 坐标转换
        /// <summary>
        /// 尝试通过屏幕坐标获取网格坐标
        /// </summary>
        /// <param name="mouseOnGridPos">鼠标在网格中的二维信息</param>
        /// <param name="mouseOnGridIndex">该网格的一维信息</param>
        private bool TryGetMousePosInGrid(Vector2 mousePos,
                                     out Vector2Int mouseOnGridPos,
                                     out int mouseOnGridIndex)
        {
            mouseOnGridPos = Vector2Int.zero;
            mouseOnGridIndex = -1;

            // 屏幕 转 Content本地
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridContent,
                                                                     mousePos,
                                                                     canvasCamera,
                                                                     out var localPoint))
                return false;
            // 本地 转 网格
            Vector2 originPoint = localPoint;
            Vector2 effectivePoint = Vector2.zero;
            // 跨越一个网格的步长 = 网格大小 + 间距
            Vector2Int stepSize = new Vector2Int(gridSize + spacing, gridSize + spacing);

            // 鼠标点在Grid有效区域的值 = 原始点 - LayoutGroup的偏移量
            effectivePoint.x = originPoint.x - contentLayoutGroup.padding.left;
            effectivePoint.y = -(originPoint.y - contentLayoutGroup.padding.top);
            if (effectivePoint.x < 0 || effectivePoint.y < 0)
                return false;

            // 网格坐标
            mouseOnGridPos.x = Mathf.FloorToInt(effectivePoint.x / stepSize.x);
            mouseOnGridPos.y = Mathf.FloorToInt(effectivePoint.y / stepSize.y);

            // 越界
            if (mouseOnGridPos.x < 0 || mouseOnGridPos.y < 0
                || mouseOnGridPos.x >= gridRowAndCloumns.x || mouseOnGridPos.y >= gridRowAndCloumns.y)
                return false;

            // 落在spacing空隙 = 有效值 - 网格坐标 * 跨越步长
            float inStepX = effectivePoint.x - mouseOnGridPos.x * stepSize.x;
            float inStepY = effectivePoint.y - mouseOnGridPos.y * stepSize.y;
            if (inStepX > contentLayoutGroup.cellSize.x || inStepY > contentLayoutGroup.cellSize.y)
                return false;

            // 网格索引
            // position.y * gridInventorySize.x + position.x;
            mouseOnGridIndex = mouseOnGridPos.y * gridRowAndCloumns.x + mouseOnGridPos.x;

            return true;

        }
        #endregion

        #region 视觉表现
        /// <summary>
        /// 通过锚点格坐标获取其UI位置
        /// </summary>
        /// <param name="anchorPos">锚点格坐标</param>
        /// <param name="dataSize">物品数据尺寸</param>
        /// <returns>物品RectTransform的localPosition</returns>
        private Vector2 GetItemUIPos(Vector2Int anchorPos, Vector2Int dataSize)
        {
            Vector2 step = new Vector2(gridSize + contentLayoutGroup.spacing.x, gridSize + contentLayoutGroup.spacing.y);

            Vector2 topLeft = new Vector2(
                contentLayoutGroup.padding.left + anchorPos.x * step.x,
                -(contentLayoutGroup.padding.top + anchorPos.y * step.y));

            Vector2 uiSize = GetItemUISize(dataSize);


            // return new Vector2(topLeft.x + uiSize.x , topLeft.y - uiSize.y );
            var position = new Vector2(topLeft.x + uiSize.x * 0.5f, topLeft.y - uiSize.y * 0.5f);
            return position;
        }


        /// <summary>
        /// 计算吸附框位置和尺寸
        /// </summary>
        /// <param name="itemData">物品数据</param>
        /// <param name="anchorPos">锚点位置</param>
        /// <param name="position">吸附框位置</param>
        /// <param name="size">吸附框尺寸</param>
        private Vector2 GetFrameBoardTransform(ItemRtData itemData,
                                               Vector2Int anchorPos)
        {
            var occupiedSize = GetItemSizeInCells(itemData);
            return GetItemUIPos(anchorPos, occupiedSize);
        }


        /// <summary>
        /// 获取物品当前占用宽高
        /// </summary>
        /// <param name="itemData">物品数据</param>
        private Vector2Int GetItemSizeInCells(ItemRtData itemData)
        {
            return itemData.DataSize;
        }

        /// <summary>
        /// 获取物品UI宽高
        /// </summary>
        /// <param name="dataSize">格子理论尺寸</param>
        /// <returns></returns>
        public Vector2 GetItemUISize(Vector2Int dataSize)
        {
            if (dataSize.x <= 0 || dataSize.y <= 0)
                throw new System.Exception("格子大小不能小于1");

            Vector2 uiSize = Vector2.zero;
            // 宽度 = 格子数量 * 网格大小 + 间距 * (格子数量 - 1)
            uiSize.x = dataSize.x * gridSize + (dataSize.x - 1) * contentLayoutGroup.spacing.x;
            uiSize.y = dataSize.y * gridSize + (dataSize.y - 1) * contentLayoutGroup.spacing.y;
            return uiSize;
        }


        public void SetFrameBoardState(ItemRtData oldItemData,
                                            ItemRtData newItemData,
                                            Vector2Int dragPreviewAnchorPos,
                                            Vector2 pos,
                                            Vector2 size)
        {
            if (frameBoardView is null) return;

            var state = gridInventoryService.JudgeFrameBoardState(oldItemData,
                                                                newItemData,
                                                                dragPreviewAnchorPos);

            frameBoardView.SetFrameBoardView(state, pos, size);
        }


        #endregion


        #region 锚点计算
        /// <summary>
        /// 获取预览锚点位置
        /// 塔科夫式连续锚点 鼠标格减去抓取偏移
        /// </summary>
        /// <param name="mouseOnGridPos">鼠标当前悬停的格子位置</param>
        /// <param name="dragStartOffset">抓取相对偏移</param>
        /// <returns>预览锚点</returns>
        private Vector2Int GetPreviewAnchorPos(Vector2Int mouseOnGridPos, Vector2Int dragStartOffset)
        {
            var itemData = draggingItem != null ? draggingItem.ItemData : activeItem?.ItemData;
            if (itemData is null)
                return dragPreviewAnchorPos;

            var gridSizeInCells = GetItemSizeInCells(itemData);
            Vector2Int anchorPosition = mouseOnGridPos - dragStartOffset;
            return ClampAnchorPositionToGrid(anchorPosition, gridSizeInCells.x, gridSizeInCells.y);
        }

        /// <summary>
        /// 将锚点夹取到合法范围 保证矩形框不超出网格
        /// </summary>
        /// <param name="anchorPosition">锚点位置</param>
        /// <param name="width">物品宽度</param>
        /// <param name="height">物品高度</param>
        /// <returns></returns>
        private Vector2Int ClampAnchorPositionToGrid(
                                Vector2Int anchorPosition,
                                int width,
                                int height)
        {
            int anchorMaximumX = Mathf.Max(0, gridRowAndCloumns.x - width);
            int anchorMaximumY = Mathf.Max(0, gridRowAndCloumns.y - height);
            anchorPosition.x = Mathf.Clamp(anchorPosition.x, 0, anchorMaximumX);
            anchorPosition.y = Mathf.Clamp(anchorPosition.y, 0, anchorMaximumY);
            return anchorPosition;
        }

        #endregion
    }
}