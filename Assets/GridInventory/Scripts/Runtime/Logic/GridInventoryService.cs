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

        /// <summary> 交换类型 </summary>
        public readonly ESwapState SwapState;

        /// <summary>
        /// 构造函数
        /// </summary>
        public InventoryOpReport(bool isSuccess,
                                 ItemRtData itemDataA,
                                 ItemRtData itemDataB = null,
                                 List<ItemRtData> displacedItemDataList = null,
                                 ESwapState swapState = ESwapState.CanNotSwap)
        {
            IsSuccess = isSuccess;
            ItemDataA = itemDataA;
            ItemDataB = itemDataB;
            DisplacedItemDataList = displacedItemDataList;
            SwapState = swapState;
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
            if (!SetAnchorAndPlaceItem(itemRtData, anchorPos))
            {
                // 该位置已存在物品 尝试放置到第一个可放置位置 锚点由数据层同步
                if (!inventoryState.SetAtFirst(itemRtData, out _))
                {
                    Debug.Log("创建物品失败 没有找到可放置位置");
                    return null;
                }
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

            // 创建运行时数据 锚点由数据层同步
            var itemRtData = ItemRtData.FromConfig(itemData);
            if (!inventoryState.SetAtFirst(itemRtData, out _))
            {
                Debug.Log("创建物品失败 没有找到可放置位置");
                return null;
            }

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
                if (!SetAnchorAndPlaceItem(itemDataA, anchorPosB))
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

            if (inventoryState.TryGetSwapTargetItem(itemDataA, anchorPosB, out var swapTargetItem))
            {
                // 小物品列表
                var swapDisplacedList = new List<IItemRuntime>();
                var swapState = inventoryState.GetSwapState(itemDataA, swapTargetItem);
                // 尝试交换 TrySwap 失败时内部自行回滚 无需预演
                if (inventoryState.TrySwap(itemDataA,
                                           swapTargetItem,
                                           swapDisplacedList,
                                           anchorPosB,
                                           ESwapPlaceMode.CrossContainer))
                {
                    return new InventoryOpReport(true,
                        itemDataA,
                        swapTargetItem as ItemRtData,
                        ToItemRtDataList(swapDisplacedList),
                        swapState);
                }

                 return new InventoryOpReport(false, itemDataA, swapTargetItem as ItemRtData, swapState: swapState);
            }
            return new InventoryOpReport(false, itemDataA);
        }

        /// <summary>
        /// 尝试接收跨容器交换 A 侧返回物
        /// </summary>
        /// <param name="result">交换结果</param>
        /// <param name="returnBaseAnchorPos">A 侧大物拖起锚点</param>
        /// <param name="dropAnchorPos">B 侧大物落点锚点</param>
        /// <returns>是否成功</returns>
        public bool TryReceiveSwapReturnItem(InventoryOpReport result,
                                             Vector2Int returnBaseAnchorPos,
                                             Vector2Int dropAnchorPos)
        {
            switch (result.SwapState)
            {
                case ESwapState.Same:
                case ESwapState.SmallToLarge:
                    if (result.ItemDataB is null)
                        return false;

                    if (SetAnchorAndPlaceItem(result.ItemDataB, returnBaseAnchorPos))
                        return true;

                    return result.SwapState == ESwapState.SmallToLarge
                           && TryPlaceAtFirst(result.ItemDataB);

                case ESwapState.LargeToSmall:
                    return TryReceiveDisplacedItemList(result.DisplacedItemDataList,
                                                       dropAnchorPos,
                                                       returnBaseAnchorPos);

                default:
                    return true;
            }
        }

        /// <summary>
        /// 接收大换小被挤物列表
        /// </summary>
        private bool TryReceiveDisplacedItemList(List<ItemRtData> displacedItemDataList,
                                                 Vector2Int dropAnchorPos,
                                                 Vector2Int returnBaseAnchorPos)
        {
            if (displacedItemDataList is null || displacedItemDataList.Count == 0)
                return true;

            for (int i = 0; i < displacedItemDataList.Count; i++)
            {
                var itemData = displacedItemDataList[i];
                if (itemData is null)
                    return false;

                // 小物在 B 相对大物落点的偏移 映射到 A 拖起锚点
                var relativeOffset = itemData.AnchorPos - dropAnchorPos;
                if (!SetAnchorAndPlaceItem(itemData, returnBaseAnchorPos + relativeOffset))
                    return false;
            }

            return true;
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

            void RestoreItemA() => SetAnchorAndPlaceItem(itemDataA, anchorPosA);

            // 直接放
            if (inventoryState.CanPlace(itemDataA, anchorPosB))
            {
                if (!SetAnchorAndPlaceItem(itemDataA, anchorPosB))
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

            // 尝试交换 TrySwap 失败时内部自行回滚 无需预演
            if (inventoryState.TryGetSwapTargetItem(itemDataA, anchorPosB, out var swapTargetItem))
            {
                var swapDisplacedList = new List<IItemRuntime>();
                var swapState = inventoryState.GetSwapState(itemDataA, swapTargetItem);
                if (inventoryState.TrySwap(itemDataA,
                                           swapTargetItem,
                                           swapDisplacedList,
                                           anchorPosB))
                {
                    return new InventoryOpReport(true,
                                                 itemDataA,
                                                 swapTargetItem as ItemRtData,
                                                 ToItemRtDataList(swapDisplacedList),
                                                 swapState);
                }

                // 交换失败 数据层已自行回滚 这里把拖起的 A 放回原位
                RestoreItemA();
                return new InventoryOpReport(false, itemDataA, swapTargetItem as ItemRtData, swapState: swapState);
            }

            // 全部尝试失败 回滚状态
            RestoreItemA();
            return new InventoryOpReport(false, itemDataA);
        }

        /// <summary>
        /// 查找背包首个空位并放置
        /// </summary>
        public bool TryPlaceAtFirst(ItemRtData itemData)
        {
            if (itemData is null)
                return false;
            // 锚点由数据层同步
            return inventoryState.SetAtFirst(itemData, out _);
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
        public EDragPreviewState JudgeDragPreviewState(ItemRtData itemDataA,
                                                       ItemRtData itemDataB,
                                                       Vector2Int dragPreviewAnchorPos,
                                                       ESwapPlaceMode swapPlaceMode = ESwapPlaceMode.SameContainer)
        {
            if (inventoryState.CanPlace(itemDataA, dragPreviewAnchorPos))
                return EDragPreviewState.CanPlace;

            if (itemDataB is not null && inventoryState.CanStack(itemDataA, itemDataB))
                return EDragPreviewState.CanStack;

            if (inventoryState.TryGetSwapTargetItem(itemDataA, dragPreviewAnchorPos, out var swapTargetItem) &&
                inventoryState.CanSwap(itemDataA,
                                       swapTargetItem,
                                       dragPreviewAnchorPos,
                                       swapPlaceMode))
                return EDragPreviewState.CanPlaceSwap;

            return EDragPreviewState.CannotPlace;
        }

        #endregion

        #region 工具

        /// <summary>
        /// 设置锚点并占用格子
        /// 锚点由数据层 SetItemData 统一同步 此处不再手动写入
        /// </summary>
        public bool SetAnchorAndPlaceItem(ItemRtData itemData, Vector2Int anchorPos)
        {
            if (itemData is null)
                return false;

            return inventoryState.SetAt(anchorPos, itemData);
        }

        /// <summary>
        /// 捕获背包快照
        /// </summary>
        public InventoryState.Snapshot CaptureSnapshot() =>
            inventoryState.CaptureSnapshot();

        /// <summary>
        /// 还原背包快照
        /// </summary>
        public void RestoreSnapshot(InventoryState.Snapshot snapshot) =>
            inventoryState.RestoreSnapshot(snapshot);

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
