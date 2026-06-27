using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    public partial class InventoryState
    {
        private sealed partial class InventorySwapService
        {
            /// <summary> 背包状态引用 </summary>
            private readonly InventoryState inventoryState;
            /// <summary> 临时覆盖物品集合 </summary>
            private readonly HashSet<IItemRuntime> tempLittleItemHashList = new();
            /// <summary>
            /// 临时小物品相对偏移字典
            /// </summary>
            private readonly Dictionary<IItemRuntime, Vector2Int> tempLittleItemOffsetDict = new();
            /// <summary> 试算用临时列表 </summary>
            private readonly List<IItemRuntime> simulateOldItemList = new();

            /// <summary>
            /// 初始化交换服务
            /// </summary>
            /// <param name="inventoryState">背包状态</param>
            public InventorySwapService(InventoryState inventoryState)
            {
                this.inventoryState = inventoryState;
            }

            #region 对外交换接口
            /// <summary>
            /// 检查是否可交换
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否可交换</returns>
            public bool CanSwap(IItemRuntime aItemData,
                                IItemRuntime bItemData,
                                Vector2Int placeAnchorPos,
                                ESwapPlaceMode swapPlaceMode = ESwapPlaceMode.SameContainer) =>
            SimulateSwap(aItemData, bItemData, placeAnchorPos, swapPlaceMode);

            /// <summary>
            /// 执行交换
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否成功</returns>
            public bool TrySwap(IItemRuntime aItemData,
                                IItemRuntime bItemData,
                                List<IItemRuntime> oldItemDataList,
                                Vector2Int placeAnchorPos,
                                ESwapPlaceMode swapPlaceMode = ESwapPlaceMode.SameContainer) =>
            CommitSwap(aItemData, bItemData, placeAnchorPos, oldItemDataList, swapPlaceMode);
            #endregion

            #region 提交与试算
            /// <summary>
            /// 在克隆网格上试算交换
            /// 不会修改真实背包数据
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否可以交换</returns>
            private bool SimulateSwap(IItemRuntime aItemData,
                                      IItemRuntime bItemData,
                                      Vector2Int placeAnchorPos,
                                      ESwapPlaceMode swapPlaceMode)
            {
                // 获取交换计划
                var plan = GetSwapPlan(aItemData, bItemData);
                if (plan.SwapState == ESwapState.CanNotSwap) return false;

                // 获取交换物品的锚点索引
                int aIndex = inventoryState.ToIndex(plan.aItemData.AnchorPos);
                int bIndex = inventoryState.ToIndex(plan.bItemData.AnchorPos);

                // 克隆物品数组和占用信息
                var simAnchorArray = (IItemRuntime[])inventoryState.itemAnchorArray.Clone();
                var simOccupancyArray = (IItemRuntime[])inventoryState.occupancyOwnerArray.Clone();
                // 真实物品数组和占用信息
                var realAnchorArray = inventoryState.itemAnchorArray;
                // 真实占用信息
                var realOccupancyArray = inventoryState.occupancyOwnerArray;

                // 临时交换物品数组和占用信息
                inventoryState.itemAnchorArray = simAnchorArray;
                inventoryState.occupancyOwnerArray = simOccupancyArray;
                try
                {
                    // 尝试执行交换
                    return TryExecuteSwap(plan,
                                          aIndex,
                                          bIndex,
                                          placeAnchorPos,
                                          simulateOldItemList,
                                          swapPlaceMode);
                }
                // 如果交换失败 则回滚真实网格
                finally
                {
                    inventoryState.itemAnchorArray = realAnchorArray;
                    inventoryState.occupancyOwnerArray = realOccupancyArray;
                }
            }

            /// <summary>
            /// 提交交换 失败时回滚真实网格
            /// </summary>
            /// <param name="aItemData">拖动物品</param>
            /// <param name="bItemData">目标物品</param>
            /// <param name="placeAnchorPos">放置锚点</param>
            /// <param name="oldItemDataList">被覆盖旧物品列表</param>
            /// <param name="swapPlaceMode">交换放置模式</param>
            /// <returns>是否成功</returns>
            private bool CommitSwap(IItemRuntime aItemData,
                                    IItemRuntime bItemData,
                                    Vector2Int placeAnchorPos,
                                    List<IItemRuntime> oldItemDataList,
                                    ESwapPlaceMode swapPlaceMode)
            {
                // 获取交换计划
                var plan = GetSwapPlan(aItemData, bItemData);
                if (plan.SwapState == ESwapState.CanNotSwap)
                    return false;

                // 获取交换物品的锚点索引
                int aIndex = inventoryState.ToIndex(plan.aItemData.AnchorPos);
                int bIndex = inventoryState.ToIndex(plan.bItemData.AnchorPos);

                // 备份物品数组和占用信息
                var backupItemArray = (IItemRuntime[])inventoryState.itemAnchorArray.Clone();
                var backupOccupancyOwner = (IItemRuntime[])inventoryState.occupancyOwnerArray.Clone();

                // 是否需要回滚标记
                bool shouldRollback = true;
                try
                {
                    // 尝试执行交换
                    var canSwap = TryExecuteSwap(plan,
                                                 aIndex,
                                                 bIndex,
                                                 placeAnchorPos,
                                                 oldItemDataList,
                                                 swapPlaceMode);
                    if (!canSwap) return false;

                    // 更新物品信息
                    for (int i = 0; i < inventoryState.itemAnchorArray.Length; i++)
                    {
                        // 获取物品
                        var item = inventoryState.itemAnchorArray[i];
                        if (item is null) continue;
                        // 更新物品锚点
                        item.SetAnchorPos(inventoryState.ToVector2Int(i));
                    }
                    // 不需要回滚
                    shouldRollback = false;
                    return true;
                }
                // 如果交换失败 则回滚真实网格
                finally
                {
                    // 如果需要回滚 则回滚物品数组和占用信息
                    if (shouldRollback)
                    {
                        inventoryState.itemAnchorArray = backupItemArray;
                        inventoryState.occupancyOwnerArray = backupOccupancyOwner;
                    }
                }
            }

            /// <summary>
            /// 在当前网格上执行交换分支
            /// </summary>
            private bool TryExecuteSwap(SwapPlan plan,
                                        int aIndex,
                                        int bIndex,
                                        Vector2Int placeAnchorPos,
                                        List<IItemRuntime> oldItemDataList,
                                        ESwapPlaceMode swapPlaceMode)
            {
                oldItemDataList.Clear();
                switch (plan.SwapState)
                {
                    case ESwapState.Same:
                        return SwapSameItem(plan,
                                            aIndex,
                                            bIndex,
                                            placeAnchorPos,
                                            swapPlaceMode);

                    case ESwapState.LargeToSmall:
                        return SwapLargeToSmallItem(plan,
                                                    placeAnchorPos,
                                                    oldItemDataList,
                                                    swapPlaceMode);

                    case ESwapState.SmallToLarge:
                        return SwapSmallToLargeItem(plan, placeAnchorPos, swapPlaceMode);

                    default:
                        return false;
                }
            }
            #endregion
        }
    }
}
