using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MmInventory
{

    /// <summary>
    /// 此脚本放置数据结构操作和计算方法 是View这边的核心逻辑 
    /// 看懂此脚本就能理解整个View层的网格同步与数据管理
    /// </summary>

    public partial class GridMainContainerView
    {

        #region 数据结构
        [Header("Debug")]
        [SerializeField, ReadOnly]
        /// <summary>网格格子视图 用于显示网格格子</summary>
        private GridCellView[] gridCellViewList;

        [DictionaryDrawerSettings(KeyLabel = "物品实例ID", ValueLabel = "物品视图")]
        [SerializeField]
        private Dictionary<string, ItemView> itemViewDict = new();

        /// <summary>
        /// 把物品视图加入到本容器
        /// </summary>
        private void AddItemView(ItemView itemView)
        {
            if (itemView is null) return;
            itemViewDict[itemView.ItemData.InstancedItemId] = itemView;
            itemView.BindOwner(this);
            itemView.ItemRectTransform.SetParent(itemContent, true);
            itemView.ItemRectTransform.SetAsLastSibling();
        }

        /// <summary>
        /// 把物品视图从本容器移除
        /// </summary>
        /// <param name="itemView"></param>
        private void RemoveItemView(ItemView itemView)
        {
            itemViewDict.Remove(itemView.ItemData.InstancedItemId);
        }

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
                gridInventoryService.SetAnchorAndPlaceItem(itemView.ItemData, anchorPos);
                itemView.ItemRectTransform.localPosition = GetItemUIPivotPos(anchorPos, itemView.ItemData.DataSize);
                itemViewDict[itemView.ItemData.InstancedItemId] = itemView;
            }
        }

        /// <summary>
        /// 实例化物品视图并注册到字典
        /// </summary>
        private ItemView SpawnItemView(ItemRtData itemRtData)
        {
            // 按尺寸取视图预制体
            var prefabList = ItemViewPrefabListSo.Instance;
            if (prefabList is null || !prefabList.TryGetPrefab(itemRtData.DataSize, out var prefab))
            {
                Debug.LogWarning($"创建物品UI失败 未找到尺寸 {itemRtData.DataSize} 的预制体");
                return null;
            }

            // 实例化到物品层
            var itemGo = Instantiate(prefab, itemContent);
            var itemView = itemGo.GetComponent<ItemView>();
            if (itemView is null)
            {
                Debug.LogError("ItemView 预制体缺少 ItemView 组件");
                Destroy(itemGo);
                return null;
            }

            itemView.BindOwner(this);
            itemView.InitWithData(itemRtData);
            // 注册到字典
            itemViewDict[itemRtData.InstancedItemId] = itemView;
            return itemView;
        }


        #endregion
        #region 坐标转换
        /// <summary>
        /// 尝试通过屏幕坐标获取UI容器上对应的网格坐标和索引
        /// 原理: 屏幕坐标-通过API->UI本地坐标--翻转Y轴并向下取整->网格坐标 和 网格Index
        /// </summary>
        /// <param name="mouseOnGridPosInt">鼠标在网格中的二维信息</param>
        /// <param name="mouseOnGridIndex">该网格的一维信息</param>
        public bool TryGetMouseInGridInfo(Vector2 mousePos,
                                     out Vector2Int mouseOnGridPosInt,
                                     out int mouseOnGridIndex)
        {
            mouseOnGridPosInt = Vector2Int.zero;
            mouseOnGridIndex = -1;

            // 屏幕 转 Content本地 以Pivot为原点
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(gridContent,
                                                                     mousePos,
                                                                     CanvasCamera,
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


        #region UI计算
        /// <summary>
        /// 通过锚点坐标获取UI中心的位置
        /// 原理: 网格坐标---> UI坐标 ---加物品半宽高--> UI中心位置
        /// </summary>
        /// <param name="anchorPos">锚点格坐标</param>
        /// <param name="dataSize">物品数据尺寸</param>
        /// <returns>物品RectTransform的localPosition</returns>
        private Vector2 GetItemUIPivotPos(Vector2Int anchorPos, Vector2Int dataSize)
        {
            // 格子步长
            Vector2 step = new Vector2(gridSize + contentLayoutGroup.spacing.x,
                                       gridSize + contentLayoutGroup.spacing.y);

            // 锚点位置反推出UI位置
            // 比如: anchorPos = 1,2 padding =0 格子步长 = 100
            // 左上角 (100, -200)
            var topLeftPos = new Vector2(
                contentLayoutGroup.padding.left + anchorPos.x * step.x,
                -(contentLayoutGroup.padding.top + anchorPos.y * step.y));

            // 物品UI宽高
            var uiSize = GetItemUISize(dataSize);

            // 物品UI中心点位置
            var position = new Vector2(topLeftPos.x + uiSize.x * 0.5f,
                                       topLeftPos.y - uiSize.y * 0.5f);
            return position;
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

            var uiSize = Vector2.zero;
            // 宽度 = 格子宽度 * 网格大小 + 间距 * (格子宽度 - 1)
            uiSize.x = dataSize.x * gridSize
                                    + (dataSize.x - 1) * contentLayoutGroup.spacing.x;

            // 高度 = 格子高度 * 网格大小 + 间距 * (格子高度 - 1)
            uiSize.y = dataSize.y * gridSize
                                    + (dataSize.y - 1) * contentLayoutGroup.spacing.y;
            return uiSize;
        }


        /// <summary>
        /// 判断点是否在视口内
        /// </summary>
        /// <param name="screenPos"></param>
        /// <returns></returns>
        public bool PointIsInViewprot(Vector2 screenPos)
        {
            if (ScrollRect is null || ScrollRect.viewport is null)
                return false;
            // 判断屏幕坐标点是否落在目标 RectTransform 的矩形范围内
            return RectTransformUtility.RectangleContainsScreenPoint(
                    ScrollRect.viewport, screenPos, CanvasCamera);
        }
        #endregion


        #region 锚点相关计算

        /// <summary>
        /// 获取预览锚点位置
        /// 锚点 = 鼠标格子坐标 - 抓取相对偏移
        /// </summary>
        /// <param name="mouseOnGridPos">鼠标当前悬停的格子位置</param>
        /// <param name="dragStartOffset">抓取相对偏移</param>
        /// <param name="itemData">物品数据</param>
        /// <returns>预览锚点</returns>
        private Vector2Int GetPreviewAnchorPos(Vector2Int mouseOnGridPos,
                                               Vector2Int dragStartOffset,
                                               ItemRtData itemData)
        {
            if (itemData is null)
                return dragSession.PreviewAnchorPos;

            Vector2Int anchorPosition = mouseOnGridPos - dragStartOffset;
            return ClampAnchorPositionToGrid(anchorPosition,
                                             itemData.DataSize.x,
                                             itemData.DataSize.y);
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

        /// <summary>
        /// 网格坐标转格子索引
        /// </summary>
        private int ToCellIndex(Vector2Int gridPos)
        {
            if (gridPos.x < 0 || gridPos.y < 0
                || gridPos.x >= gridRowAndCloumns.x || gridPos.y >= gridRowAndCloumns.y)
                return -1;

            return gridPos.y * gridRowAndCloumns.x + gridPos.x;
        }

        /// <summary>
        /// 源容器回滚拖拽物
        /// </summary>
        /// <param name="itemView">拖拽物品</param>
        private void RollbackDragItem(ItemView itemView)
        {
            var resetData = itemView.ItemData;
            resetData.SetRotated(dragSession.StartIsRotated);
            // 数据层位置重置 锚点由数据层同步
            gridInventoryService.SetAnchorAndPlaceItem(resetData, dragSession.StartAnchorPos);
            // UI位置重置
            itemView.ItemRectTransform.localRotation =
                Quaternion.Euler(0, 0, resetData.IsRotated ? 90f : 0f);
            itemView.ItemRectTransform.SetParent(itemContent, true);
            itemView.ItemRectTransform.localPosition =
                GetItemUIPivotPos(dragSession.StartAnchorPos, resetData.DataSize);
        }

        #endregion

    }
}