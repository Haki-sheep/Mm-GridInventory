using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MmInventory
{

    /// <summary>
    /// 此脚本放置工具方法 一般不提供给外部使用
    /// </summary>

    public partial class GridMainContainerView 
    {
        #region 坐标转换
        /// <summary>
        /// 尝试通过屏幕坐标获取网格坐标
        /// 原理: 屏幕坐标-通过API->UI本地坐标--翻转Y轴并向下取整->网格坐标 和 网格Index
        /// </summary>
        /// <param name="mouseOnGridPosInt">鼠标在网格中的二维信息</param>
        /// <param name="mouseOnGridIndex">该网格的一维信息</param>
        private bool TryGetMouseInGridInfo(Vector2 mousePos,
                                     out Vector2Int mouseOnGridPosInt,
                                     out int mouseOnGridIndex)
        {
            mouseOnGridPosInt = Vector2Int.zero;
            mouseOnGridIndex = -1;

            // 屏幕 转 Content本地 以Pivot为原点
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridContent,
                                                                     mousePos,
                                                                     canvasCamera,
                                                                     out var localPoint))
            {
                return false;
            }

            // 翻转Y轴 将UI本地坐标系转为网格坐标系
            Vector2 layoutLocalMousePos = Vector2.zero;
            layoutLocalMousePos.x = localPoint.x - contentLayoutGroup.padding.left;
            layoutLocalMousePos.y = -(localPoint.y - contentLayoutGroup.padding.top);

            // 越界
            if (layoutLocalMousePos.x < 0 || layoutLocalMousePos.y < 0)
                return false;

            // 计算网格坐标 向下取整
            // 比如一个格子大小为100，间距为0，那么一个格子的步长就是100
            // layoutLocalMousePos = 214,128
            // 那么网格坐标就是 214/100 = 2.14 向下取整为2
            // 128/100 = 1.28 向下取整为1
            // 所以网格坐标就是 (2,1)
            Vector2Int stepSize = new Vector2Int(gridSize + spacing, gridSize + spacing);
            mouseOnGridPosInt.x = Mathf.FloorToInt(layoutLocalMousePos.x / stepSize.x);
            mouseOnGridPosInt.y = Mathf.FloorToInt(layoutLocalMousePos.y / stepSize.y);

            // 越界
            if (mouseOnGridPosInt.x < 0 || mouseOnGridPosInt.y < 0
                || mouseOnGridPosInt.x >= gridRowAndCloumns.x || mouseOnGridPosInt.y >= gridRowAndCloumns.y)
                return false;

            // 如果是鼠标落在间距内 则不算数
            // 比如一个格子大小为100，间距为10，那么一个格子的步长就是110
            // 假设 layoutLocalMousePos.x = 214
            // mouseOnGridPos.x= 214 / 110 = 1.94 向下取整为1
            // inStepX = 214 - 1 * 110 = 104
            float inStepX = layoutLocalMousePos.x - mouseOnGridPosInt.x * stepSize.x;
            float inStepY = layoutLocalMousePos.y - mouseOnGridPosInt.y * stepSize.y;
            // 104>100 所以明显是落在了空隙中
            if (inStepX > contentLayoutGroup.cellSize.x || inStepY > contentLayoutGroup.cellSize.y)
                return false;

            // 计算最终鼠标在网格中的索引 二维坐标转一维坐标 
            // position.y * gridInventorySize.x + position.x;
            mouseOnGridIndex = mouseOnGridPosInt.y * gridRowAndCloumns.x + mouseOnGridPosInt.x;

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
            Vector2 step = new Vector2(gridSize + contentLayoutGroup.spacing.x,
                                       gridSize + contentLayoutGroup.spacing.y);

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


        /// <summary>
        /// 设置吸附框状态
        /// </summary>
        /// <param name="oldItemData"></param>
        /// <param name="newItemData"></param>
        /// <param name="dragPreviewAnchorPos"></param>
        /// <param name="pos"></param>
        /// <param name="size"></param>
        public void SetFrameBoardState(ItemRtData oldItemData,
                                            ItemRtData newItemData,
                                            Vector2Int dragPreviewAnchorPos,
                                            Vector2 pos,
                                            Vector2 size)
        {
            if (frameBoardView is null) return;

            // 判断吸附框状态
            var state = gridInventoryService.JudgeFrameBoardState(oldItemData,
                                                                newItemData,
                                                                dragPreviewAnchorPos);

            // 设置吸附框状态
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
            var itemData = draggingItem != null ? draggingItem.ItemData : null;
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


        /// <summary>
        /// 注册场景内物品到逻辑层与字典
        /// </summary>
        private void RegisterSceneItemViews()
        {
            itemViewDict.Clear();

            for (int i = 0; i < itemViews.Length; i++)
            {
                var itemView = itemViews[i];
                itemView.BindOwner(this);
                itemView.Init();

                if (itemView.ItemData is null)
                    continue;

                Vector2Int anchorPos = itemView.ItemData.AnchorPos;
                gridInventoryService.PlaceItem(itemView.ItemData, anchorPos);
                itemView.ItemRectTransform.localPosition = GetItemUIPos(anchorPos, itemView.ItemData.DataSize);
                itemViewDict[itemView.ItemData.InstancedItemId] = itemView;
            }
        }

        /// <summary>
        /// 外部调用，创建物品格子
        /// </summary>
        [Button]
        public void CreatItemCells()
        {
            var cellCount = gridRowAndCloumns.x * gridRowAndCloumns.y;

            // 设置容器大小
            containertRectTransform.sizeDelta = new Vector2(gridSize * gridRowAndCloumns.x,
                                                            gridSize * gridRowAndCloumns.y);

            // 设置物品内容容器
            if (gridContent == null || itemContent == null) return;
            if (gridContent.parent != itemContent.parent) return;

            // 设置物品内容容器锚点
            itemContent.anchorMin = gridContent.anchorMin;
            itemContent.anchorMax = gridContent.anchorMax;
            itemContent.pivot = gridContent.pivot;
            itemContent.localScale = gridContent.localScale;
            itemContent.localRotation = gridContent.localRotation;

            itemContent.offsetMin = gridContent.offsetMin;
            itemContent.offsetMax = gridContent.offsetMax;

            // 场景格子数量已够则跳过销毁
            if (contentLayoutGroup.transform.childCount == cellCount && cellCount > 0)
            {
                gridCellViews = gridContent.GetComponentsInChildren<GridCellView>();
                return;
            }

            for (int i = contentLayoutGroup.transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = contentLayoutGroup.transform.GetChild(i).gameObject;
                // 勿删掉 CellPrefab 槽位里拖的场景实例
                if (CellPrefab is not null && child == CellPrefab)
                    continue;

                GameObject.DestroyImmediate(child);
            }

            if (cellCount <= 0) return;

            GameObject prefabSource = GetCellPrefabSource();
            if (prefabSource is null)
            {
                Debug.LogWarning("GridMainContainerView: CellPrefab 无效 请拖 Project 里的预制体资源");
                return;
            }

            for (int i = 0; i < cellCount; i++)
                CreateCellInstance(prefabSource, i);

            gridCellViews = gridContent.GetComponentsInChildren<GridCellView>();
        }

        /// <summary>
        /// 创建格子实例并保持预制体关联
        /// </summary>
        private void CreateCellInstance(GameObject prefabSource, int index)
        {
            GameObject cell;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                cell = (GameObject)PrefabUtility.InstantiatePrefab(prefabSource, contentLayoutGroup.transform);
                Undo.RegisterCreatedObjectUndo(cell, "Creat Item Cells");
            }
            else
#endif
                cell = Instantiate(prefabSource, contentLayoutGroup.transform);

            cell.name = $"Cell_{index}";

            if (cell.GetComponent<GridCellView>() is null)
                cell.AddComponent<GridCellView>();
        }

        /// <summary>
        /// 解析可实例化的格子预制体资源
        /// </summary>
        private GameObject GetCellPrefabSource()
        {
            if (CellPrefab is null) return null;

#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabAsset(CellPrefab))
                return CellPrefab;

            GameObject asset = PrefabUtility.GetCorrespondingObjectFromSource(CellPrefab) as GameObject;
            if (asset is not null)
                return asset;
#endif
            return CellPrefab;
        }
    }
}