using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{

    #region 操作报告

    /// <summary>
    /// 操作结果结构体
    /// </summary>
    public readonly struct InventoryOpReport
    {
        /// <summary> 是否操作成功 </summary>
        public readonly bool IsSuccess;

        /// <summary> 被拖拽物A </summary>
        public readonly ItemRtData ItemDataA;

        /// <summary> 被交换物B </summary>
        public readonly ItemRtData ItemDataB;

        /// <summary> 交换时被挤开的物品列表 </summary>
        public readonly List<ItemRtData> DisplacedItemDataList;

        /// <summary>
        /// 构造函数
        /// </summary>
        public InventoryOpReport(bool isSuccess,
                                 ItemRtData itemDataA,
                                 ItemRtData itemDataB = null,
                                 List<ItemRtData> displacedItemDataList = null)
        {
            IsSuccess = isSuccess;
            ItemDataA = itemDataA;
            ItemDataB = itemDataB;
            DisplacedItemDataList = displacedItemDataList;
        }
    }

    #endregion

    /// <summary>
    /// 此类的职责是 充当算法层与View层之间的桥梁
    /// 算法层只是负责算数据 返回bool 和 位置
    /// 但View层需要知道操作是否成功 并且做出对应的表现
    /// 所以二者之间需要一个桥梁来传递信息,不然直接让View层调用算法层会显得很乱(真的很乱别问我怎么知道的)
    /// </summary>
    public class GridInventoryService
    {
        private InventoryState inventoryState;

        /// <summary>
        /// new一个InventoryState数据层
        /// </summary>
        public void Init(Vector2Int gridSize)
        {
            inventoryState = new InventoryState(gridSize);
        }

        #region 创建与销毁

        /// <summary>
        /// 创建物品数据并占格
        /// </summary>
        public ItemRtData CreatItem(int excelItemId, Vector2Int anchorPos)
        {
            // 获取模版数据
            var itemData = ItemRtDataMgr.Instance.GetItemData<IItemBaseData>(excelItemId);
            if (itemData is null)
            {
                Debug.Log($"创建物品失败 没有找到模版为ID:{excelItemId}的物品");
                return null;
            }

            // 创建运行时数据
            var itemRtData = ItemRtData.FromConfig(itemData);

            // 尝试放到指定锚点
            if (!CommitPlace(itemRtData, anchorPos))
            {
                // 该位置已存在物品 尝试放置到第一个可放置位置
                if (!inventoryState.SetAtFirst(itemRtData, out var firstAnchor))
                {
                    Debug.Log("创建物品失败 没有找到可放置位置");
                    return null;
                }
                itemRtData.SetAnchorPos(firstAnchor);
            }

            return itemRtData;
        }

        /// <summary>
        /// 创建物品并放到首个空位
        /// </summary>
        public ItemRtData CreatItemAtFirstEmpty(int excelItemId)
        {
            // 获取模版数据
            var itemData = ItemRtDataMgr.Instance.GetItemData<IItemBaseData>(excelItemId);
            if (itemData is null)
            {
                Debug.Log($"创建物品失败 没有找到模版为ID:{excelItemId}的物品");
                return null;
            }

            // 创建运行时数据
            var itemRtData = ItemRtData.FromConfig(itemData);
            if (!inventoryState.SetAtFirst(itemRtData, out var firstAnchor))
            {
                Debug.Log("创建物品失败 没有找到可放置位置");
                return null;
            }

            itemRtData.SetAnchorPos(firstAnchor);
            return itemRtData;
        }

        /// <summary>
        /// 尝试移除物品(数据层)
        /// </summary>
        public InventoryOpReport TryRemoveItem(Vector2Int anchorPos)
        {
            var item = inventoryState.GetItemByMask(anchorPos) as ItemRtData;
            if (item is null || !inventoryState.RemoveAtAny(anchorPos))
                return new InventoryOpReport(false, null);

            return new InventoryOpReport(true, item);
        }

        #endregion

        #region 放置 - 跨容器

        /// <summary>
        /// 尝试跨容器接收物品
        /// </summary>
        /// <param name="itemDataA">被拖拽物</param>
        /// <param name="anchorPosB">目标锚点</param>
        /// <returns>操作结果</returns>
        public InventoryOpReport TryReceiveItem(ItemRtData itemDataA, Vector2Int anchorPosB)
        {
            if (itemDataA is null)
                return new InventoryOpReport(false, null);

            // 直接放
            if (inventoryState.CanPlace(itemDataA, anchorPosB))
            {
                if (!CommitPlace(itemDataA, anchorPosB))
                    return new InventoryOpReport(false, itemDataA);
                return new InventoryOpReport(true, itemDataA);
            }

            // 如果不能直接放说明B锚点有东西
            var itemDataB = inventoryState.GetItemByMask(anchorPosB) as ItemRtData;

            // 尝试堆叠
            if (itemDataB is not null
                && inventoryState.CanStack(itemDataA, itemDataB)
                && inventoryState.TryStack(itemDataA, itemDataB))
            {
                return new InventoryOpReport(true, null, itemDataB);
            }

            if (inventoryState.TryGetSwapTargetItem(itemDataA, anchorPosB, out var swapTargetItem)
                && inventoryState.CanSwap(itemDataA, swapTargetItem, anchorPosB, false))
            {
                // 小物品列表
                var swapDisplacedList = new List<IItemRuntime>();
                // 尝试交换
                if (inventoryState.TrySwap(itemDataA, swapTargetItem, swapDisplacedList, anchorPosB, false))
                {
                    return new InventoryOpReport(true,
                        itemDataA,
                        swapTargetItem as ItemRtData,
                        ToItemRtDataList(swapDisplacedList));
                }

                 return new InventoryOpReport(false, itemDataA, swapTargetItem as ItemRtData);
            }
            return new InventoryOpReport(false, itemDataA);
        }

        #endregion

        #region 放置 - 同容器

        /// <summary>
        /// 尝试放置物品
        /// </summary>
        public InventoryOpReport TryPlaceItem(ItemRtData itemDataA,
                                              Vector2Int anchorPosA,
                                              Vector2Int anchorPosB)
        {
            if (itemDataA is null)
                return new InventoryOpReport(false, null);

            void RestoreItemA() => CommitPlace(itemDataA, anchorPosA);

            // 直接放
            if (inventoryState.CanPlace(itemDataA, anchorPosB))
            {
                if (!CommitPlace(itemDataA, anchorPosB))
                {
                    RestoreItemA();
                    return new InventoryOpReport(false, itemDataA);
                }
                return new InventoryOpReport(true, itemDataA);
            }

            var itemDataB = inventoryState.GetItemByMask(anchorPosB) as ItemRtData;

            // 尝试堆叠
            if (itemDataB is not null
                && inventoryState.CanStack(itemDataA, itemDataB)
                && inventoryState.TryStack(itemDataA, itemDataB))
            {
                if (itemDataA.CurrStackCount > 0)
                    RestoreItemA();

                return new InventoryOpReport(true, null, itemDataB);
            }

            // 尝试交换
            if (inventoryState.TryGetSwapTargetItem(itemDataA, anchorPosB, out var swapTargetItem)
                && inventoryState.CanSwap(itemDataA, swapTargetItem, anchorPosB))
            {
                var swapDisplacedList = new List<IItemRuntime>();
                if (inventoryState.TrySwap(itemDataA,
                                           swapTargetItem,
                                           swapDisplacedList,
                                           anchorPosB))
                {
                    return new InventoryOpReport(true,
                                                 itemDataA,
                                                 swapTargetItem as ItemRtData,
                                                 ToItemRtDataList(swapDisplacedList));
                }

                return new InventoryOpReport(false, itemDataA, swapTargetItem as ItemRtData);
            }

            // 全部尝试失败 回滚状态
            RestoreItemA();
            return new InventoryOpReport(false, itemDataA);
        }

        /// <summary>
        /// 强制写入物品锚点与格子占用
        /// </summary>
        public bool PlaceItem(ItemRtData itemData, Vector2Int anchorPos)
        {
            if (itemData is null) return false;
            return CommitPlace(itemData, anchorPos);
        }

        /// <summary>
        /// 判断物品是否可放到指定锚点
        /// </summary>
        public bool CanPlaceItem(ItemRtData itemData, Vector2Int anchorPos)
        {
            if (itemData is null)
                return false;
            return inventoryState.CanPlace(itemData, anchorPos);
        }

        /// <summary>
        /// 在矩形 footprint 内查找首个可放置锚点并写入
        /// </summary>
        public bool TryPlaceInFootprint(ItemRtData itemData,
                                        Vector2Int footprintAnchor,
                                        Vector2Int footprintSize)
        {
            if (itemData is null)
                return false;

            for (int y = 0; y < footprintSize.y; y++)
            {
                for (int x = 0; x < footprintSize.x; x++)
                {
                    var candidate = new Vector2Int(footprintAnchor.x + x, footprintAnchor.y + y);
                    if (!inventoryState.CanPlace(itemData, candidate))
                        continue;
                    return CommitPlace(itemData, candidate);
                }
            }

            return false;
        }

        /// <summary>
        /// 查找背包首个空位并放置
        /// </summary>
        public bool TryPlaceAtFirst(ItemRtData itemData)
        {
            if (itemData is null)
                return false;
            if (!inventoryState.SetAtFirst(itemData, out var anchorPos))
                return false;
            itemData.SetAnchorPos(anchorPos);
            return true;
        }

        #endregion


        #region 查询

        /// <summary>
        /// 获取任意格上的物品
        /// </summary>
        public ItemRtData GetItemAt(Vector2Int anyPos)
        {
            return inventoryState.GetItemByMask(anyPos) as ItemRtData;
        }

        #endregion

        #region 旋转

        /// <summary>
        /// 尝试旋转物品
        /// 这里只是转变数据状态 不影响实际物品的旋转
        /// </summary>
        public InventoryOpReport TryRotateItem(ItemRtData itemData)
        {
            if (itemData is null)
                return new InventoryOpReport(false, null);

            var originData = ItemRtDataMgr.Instance.GetItemData<IItemBaseData>(itemData.ExcelItemId);

            // 可叠加物品不允许旋转
            if (originData is not null && originData.ItemStackType == EItemStackType.Stackable)
                return new InventoryOpReport(false, itemData);

            itemData.SetRotated(!itemData.IsRotated);
            return new InventoryOpReport(true, itemData);
        }

        #endregion

        #region 预览判定

        /// <summary>
        /// 判定拖拽落点预览状态
        /// </summary>
        public EFrameBoard JudgeFrameBoardState(ItemRtData itemDataA,
                                                ItemRtData itemDataB,
                                                Vector2Int dragPreviewAnchorPos,
                                                bool shouldPlaceDisplacedItems = true)
        {
            if (inventoryState.CanPlace(itemDataA, dragPreviewAnchorPos))
                return EFrameBoard.CanPlace;

            if (itemDataB is not null && inventoryState.CanStack(itemDataA, itemDataB))
                return EFrameBoard.CanStack;

            if (inventoryState.TryGetSwapTargetItem(itemDataA, dragPreviewAnchorPos, out var swapTargetItem) &&
                inventoryState.CanSwap(itemDataA,
                                       swapTargetItem,
                                       dragPreviewAnchorPos,
                                       shouldPlaceDisplacedItems))
                return EFrameBoard.CanPlaceSwap;

            return EFrameBoard.CannotPlace;
        }

        #endregion

        #region 工具

        /// <summary>
        /// 写入锚点并占用格子
        /// </summary>
        private bool CommitPlace(ItemRtData itemData, Vector2Int anchorPos)
        {
            itemData.SetAnchorPos(anchorPos);
            return inventoryState.SetAt(anchorPos, itemData);
        }

        /// <summary>
        /// IGridItem列表转ItemRtData列表
        /// </summary>
        private static List<ItemRtData> ToItemRtDataList(List<IItemRuntime> gridItemList)
        {
            var itemRtDataList = new List<ItemRtData>(gridItemList.Count);
            for (int i = 0; i < gridItemList.Count; i++)
                itemRtDataList.Add((ItemRtData)gridItemList[i]);
            return itemRtDataList;
        }

        #endregion
    }
}
