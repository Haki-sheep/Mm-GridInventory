using System;
using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    public enum ESwapState
    {
        CanNotSwap,
        Same,
        LargeToSmall,
        SmallToLarge,
    }

    public struct SwapPlan
    {
        public ESwapState SwapState;
        public RunTimeItemData aItemData;
        public RunTimeItemData bItemData;
    }

    /// <summary>
    /// 背包站位数据
    /// </summary>
    public class InventoryState
    {
        // X=宽度(列数) Y=高度(行数)
        private Vector2Int gridInventorySize;

        // 锚点数组
        private RunTimeItemData[] runTimeItemDataArray;

        // 占用掩码
        private bool[] mask;

        // 占用者数组 用于记录每个格子的占用者
        private RunTimeItemData[] occupancyOwnerArray;

        /// <summary> 临时容器 用于交换时存储被覆盖的一堆小物品 </summary>
        private readonly HashSet<RunTimeItemData> tempItemList = new();


        public InventoryState(Vector2Int gridInventorySize)
        {
            this.gridInventorySize = gridInventorySize;
            int totalCount = gridInventorySize.x * gridInventorySize.y;
            runTimeItemDataArray = new RunTimeItemData[totalCount];
            occupancyOwnerArray = new RunTimeItemData[totalCount];
            mask = new bool[totalCount];
        }

        // 二维坐标 → 一维索引
        private int ToIndex(Vector2Int position) => position.y * gridInventorySize.x + position.x;

        // 一维索引 → 二维坐标
        private Vector2Int ToPosition(int index) => new Vector2Int(index % gridInventorySize.x, index / gridInventorySize.x);

        // 当前占用尺寸
        private static Vector2Int GetOccupiedSize(RunTimeItemData item) => item.DataSize;

        /// <summary>
        /// 更新占用信息
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="anchorPos">锚点</param>
        /// <param name="occupied">是否占用</param>
        private void WriteOccupancy(RunTimeItemData item, Vector2Int anchorPos, bool occupied)
        {
            if (item is null) return;

            var occupiedSize = GetOccupiedSize(item);
            for (int x = 0; x < occupiedSize.x; x++)
            {
                for (int y = 0; y < occupiedSize.y; y++)
                {
                    var targetPos = new Vector2Int(anchorPos.x + x, anchorPos.y + y);
                    if (!IsInside(targetPos)) continue;

                    int idx = ToIndex(targetPos);
                    mask[idx] = occupied;
                    occupancyOwnerArray[idx] = occupied ? item : null;
                }
            }
        }

        /// <summary>
        /// 设置锚点物品并更新占用信息
        /// </summary>
        /// <param name="anchorPos">锚点</param>
        /// <param name="item">物品</param>
        private void SetAnchorItem(Vector2Int anchorPos, RunTimeItemData item)
        {
            runTimeItemDataArray[ToIndex(anchorPos)] = item;
            WriteOccupancy(item, anchorPos, true);
        }

        /// <summary>
        /// 移除锚点物品并更新占用信息
        /// </summary>
        /// <param name="anchorPos">锚点</param>
        /// <returns>是否成功</returns>
        private bool RemoveAnchorItem(Vector2Int anchorPos)
        {
            if (!IsInside(anchorPos)) return false;

            int index = ToIndex(anchorPos);
            var item = runTimeItemDataArray[index];
            if (item is null) return false;

            runTimeItemDataArray[index] = null;
            WriteOccupancy(item, anchorPos, false);
            return true;
        }

        /// <summary>
        /// 更新占用掩码
        /// </summary>
        private void UpdateMask()
        {
            // 清空旧掩码和占用者数组
            Array.Fill(mask, false);
            Array.Fill(occupancyOwnerArray, null);

            for (int i = 0; i < runTimeItemDataArray.Length; i++)
            {
                var item = runTimeItemDataArray[i];
                if (item is null) continue;

                // 1. 获取锚点坐标(物品左上角)
                Vector2Int anchorPos = ToPosition(i);

                // 2. 获取运行时占用宽高
                var occupiedSize = GetOccupiedSize(item);
                int w = occupiedSize.x;
                int h = occupiedSize.y;

                // 4. 遍历物品占用的所有格子(锚点+偏移)
                for (int xOffset = 0; xOffset < w; xOffset++)
                {
                    for (int yOffset = 0; yOffset < h; yOffset++)
                    {
                        // 真实坐标 = 锚点 + 偏移
                        Vector2Int targetPos = new Vector2Int(
                            anchorPos.x + xOffset,
                            anchorPos.y + yOffset
                        );

                        // 越界检查
                        if (!IsInside(targetPos))
                            continue;

                        // 标记掩码
                        int idx = ToIndex(targetPos);
                        mask[idx] = true;
                        // 标记占用者
                        occupancyOwnerArray[idx] = item;
                    }
                }
            }
        }

        #region 边界判定
        /// <summary>
        /// 范围判定
        /// </summary>
        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.x < gridInventorySize.x &&
                   position.y >= 0 && position.y < gridInventorySize.y;
        }
        #endregion

        #region 放置功能（增/改）
        /// <summary>
        /// 放置判定
        /// </summary>
        public bool CanPlace(RunTimeItemData item, Vector2Int anchorPos)
        {
            if (!IsInside(anchorPos))
                return false;

            // 获取物品当前占用宽高
            var occupiedSize = GetOccupiedSize(item);
            int w = occupiedSize.x;
            int h = occupiedSize.y;

            // 检查所有占用格子
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    Vector2Int targetPos = new Vector2Int(anchorPos.x + x, anchorPos.y + y);
                    if (!IsInside(targetPos))
                        return false;

                    if (mask[ToIndex(targetPos)])
                        return false;
                }
            }
            return true;
        }

        /// <summary>
        /// 放置物品
        /// </summary>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <param name="itemData"> 物品数据 </param>
        public bool SetAt(Vector2Int anchorPos, RunTimeItemData itemData)
        {
            // 校验多格区域
            if (itemData is null || !CanPlace(itemData, anchorPos))
                return false;

            SetAnchorItem(anchorPos, itemData);
            return true;
        }

        /// <summary>
        /// 遍历所有格子 放置物品到第一个可放置位置
        /// </summary>
        /// <param name="itemData"> 物品数据 </param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns></returns>
        public bool FindSetAtFirst(RunTimeItemData itemData, out Vector2Int anchorPos)
        {
            anchorPos = Vector2Int.zero;
            for (int y = 0; y < gridInventorySize.y; y++)
            {
                for (int x = 0; x < gridInventorySize.x; x++)
                {
                    var candidate = new Vector2Int(x, y);
                    if (CanPlace(itemData, candidate))
                    {
                        anchorPos = candidate;
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 遍历所有格子 找到第一个可放置位置 并放置物品
        /// </summary>
        /// <param name="itemData"> 物品数据 </param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns> 是否成功 </returns>
        public bool SetAtFirst(RunTimeItemData itemData, out Vector2Int anchorPos)
        {
            if (!FindSetAtFirst(itemData, out anchorPos)) return false;
            return SetAt(anchorPos, itemData);
        }
        #endregion

        #region 堆叠功能
        /// <summary>
        /// 堆叠判定
        /// </summary>
        /// <param name="aItemData">拖动物品</param>
        /// <param name="bItemData">目标物品</param>
        /// <returns></returns>
        public bool CanStack(RunTimeItemData aItemData, RunTimeItemData bItemData, out int remainingCount)
        {
            remainingCount = 0;

            if (aItemData is null || bItemData is null) return false;
            // 物品是否相同
            if (aItemData.PersistenceItemId != bItemData.PersistenceItemId)
                return false;

            // 堆叠类型是否为可堆
            var a = RunTimeItemDataMgr.Instance.GetItemData<IItemRootData>(aItemData.PersistenceItemId);
            var b = RunTimeItemDataMgr.Instance.GetItemData<IItemRootData>(bItemData.PersistenceItemId);
            if (a.ItemStackType is EItemStackType.Single || b.ItemStackType is EItemStackType.Single)
                return false;

            // 计算剩余数量 向b堆叠 所以是b + a - b.MaxStackCount
            remainingCount = b.MaxStackCount - (aItemData.CurStackCount + bItemData.CurStackCount);
            return true;
        }
        #endregion

        #region 交换功能


        public bool CanSwap(RunTimeItemData aItemData,
                            RunTimeItemData bItemData,
                            Vector2Int placeAnchorPos) =>
        SwapItem(aItemData, bItemData, placeAnchorPos, commit: false, out _);


        public bool TrySwap(RunTimeItemData aItemData,
                            RunTimeItemData bItemData,
                            out List<RunTimeItemData> oldItemDataList,
                            Vector2Int placeAnchorPos) =>
        SwapItem(aItemData, bItemData, placeAnchorPos, commit: true, out oldItemDataList);

        private bool SwapItem(RunTimeItemData aItemData,
                                     RunTimeItemData bItemData,
                                     Vector2Int placeAnchorPos,
                                     bool commit,
                                     out List<RunTimeItemData> oldItemDataList)
        {
            oldItemDataList = new List<RunTimeItemData>();

            var plan = GetSwapPlan(aItemData, bItemData);
            if (plan.SwapState == ESwapState.CanNotSwap) return false;

            // 拿索引
            int aIndex = ToIndex(plan.aItemData.AnchorPos);
            int bIndex = ToIndex(plan.bItemData.AnchorPos);

            // 备份原始数据 : 物品数组、掩码、占用者数组
            var backupItemArray = (RunTimeItemData[])runTimeItemDataArray.Clone();
            var backupMask = (bool[])mask.Clone();
            var backupOccupancyOwner = (RunTimeItemData[])occupancyOwnerArray.Clone();

            bool canSwap = false;
            bool shouldRollback = true;
            try
            {
                switch (plan.SwapState)
                {
                    case ESwapState.Same:
                        canSwap = SwapSameItem(plan, aIndex, bIndex);
                        break;

                    case ESwapState.LargeToSmall:
                        canSwap = SwapLargeToSmallItem(plan,
                                                       placeAnchorPos,
                                                       out oldItemDataList);
                        break;

                    case ESwapState.SmallToLarge:
                        Debug.Log("尝试交换小物品到大物品");
                        canSwap = SwapSmallToLargeItem(plan, placeAnchorPos);
                        break;
                }

                if (canSwap && commit)
                {
                    for (int i = 0; i < runTimeItemDataArray.Length; i++)
                    {
                        var item = runTimeItemDataArray[i];
                        if (item is null) continue;
                        item.SetAnchorPos(ToPosition(i));
                    }
                    shouldRollback = false;
                }

                return canSwap;
            }
            finally
            {
                if (shouldRollback)
                {
                    runTimeItemDataArray = backupItemArray;
                    mask = backupMask;
                    occupancyOwnerArray = backupOccupancyOwner;
                }
            }
        }



        /// <summary>
        /// 尝试获取交换目标物品信息
        /// </summary>
        /// <param name="dragItemData">拖动物品</param>
        /// <param name="placeAnchorPos">放置锚点</param>
        /// <param name="swapTargetItem">交换目标物品</param>
        /// <returns></returns>
        public bool TryGetSwapTargetItem(RunTimeItemData dragItemData,
                                         Vector2Int placeAnchorPos,
                                         out RunTimeItemData swapTargetItem)
        {
            swapTargetItem = null;
            if (dragItemData is null) return false;

            // 获取拖动物品占用尺寸
            var dragSize = GetOccupiedSize(dragItemData);
            if (dragSize.x <= 0 || dragSize.y <= 0) return false;

            var overlapItems = new HashSet<RunTimeItemData>();

            // 遍历拖动物品占用区域 获取所有覆盖的物品
            for (int x = 0; x < dragSize.x; x++)
            {
                for (int y = 0; y < dragSize.y; y++)
                {
                    var pos = new Vector2Int(placeAnchorPos.x + x, placeAnchorPos.y + y);
                    if (!IsInside(pos)) return false;

                    // 获取覆盖当前pos的物品
                    var overlapItem = GetItemByMask(pos);
                    if (overlapItem is null) continue;
                    if (overlapItem.InstancedItemId == dragItemData.InstancedItemId) continue;
                    overlapItems.Add(overlapItem);
                }
            }

            if (overlapItems.Count == 0) return false;

            RunTimeItemData fullCoveredItem = null;
            foreach (var overlapItem in overlapItems)
            {
                var swapPlan = GetSwapPlan(dragItemData, overlapItem);
                if (swapPlan.SwapState == ESwapState.CanNotSwap)
                    return false;

                // 小换大：只要命中一个可交换的大物品即可，不要求完整覆盖。
                if (swapPlan.SwapState == ESwapState.SmallToLarge)
                {
                    if (fullCoveredItem is not null &&
                        fullCoveredItem.InstancedItemId != overlapItem.InstancedItemId)
                        return false;

                    fullCoveredItem = overlapItem;
                    continue;
                }

                // 获取覆盖物品占用尺寸
                var overlapSize = GetOccupiedSize(overlapItem);
                // 获取覆盖物品锚点
                var overlapAnchorPos = overlapItem.AnchorPos;
                bool isFullyCovered = true;

                // 遍历覆盖物品占用区域 判断是否被完整覆盖
                for (int x = 0; x < overlapSize.x && isFullyCovered; x++)
                {
                    for (int y = 0; y < overlapSize.y; y++)
                    {
                        var overlapPos = new Vector2Int(overlapAnchorPos.x + x, overlapAnchorPos.y + y);
                        bool coveredByDrag =
                            overlapPos.x >= placeAnchorPos.x &&
                            overlapPos.x < placeAnchorPos.x + dragSize.x &&
                            overlapPos.y >= placeAnchorPos.y &&
                            overlapPos.y < placeAnchorPos.y + dragSize.y;
                        if (!coveredByDrag)
                        {
                            isFullyCovered = false;
                            break;
                        }
                    }
                }

                if (!isFullyCovered) return false;
                if (fullCoveredItem is null)
                    fullCoveredItem = overlapItem;
            }

            swapTargetItem = fullCoveredItem;
            return swapTargetItem is not null;
        }

        #region 相同尺寸
        public bool SwapSameItem(SwapPlan plan, int aIndex, int bIndex)
        {

            var aItemData = plan.aItemData;
            var bItemData = plan.bItemData;
            // 如果旋转不同则无法交换
            if (aItemData.IsRotated != bItemData.IsRotated)
                return false;

            Debug.Log("尝试交换相同尺寸物品");

            runTimeItemDataArray[aIndex] = null;
            runTimeItemDataArray[bIndex] = null;
            WriteOccupancy(aItemData, aItemData.AnchorPos, false);
            WriteOccupancy(bItemData, bItemData.AnchorPos, false);

            // 尺寸相同则直接尝试交换
            if (!CanPlace(aItemData, bItemData.AnchorPos))
            {
                return false;
            }

            // 尝试放旧物品到新锚点
            SetAnchorItem(bItemData.AnchorPos, aItemData);

            // 在旧物品放到新锚点后 尝试新物品放到旧锚点
            if (!CanPlace(bItemData, aItemData.AnchorPos))
            {
                return false;
            }

            SetAnchorItem(aItemData.AnchorPos, bItemData);
            return true;
        }
        #endregion

        #region 大换小  
        public bool SwapLargeToSmallItem(SwapPlan plan,
                                         Vector2Int placeAnchorPos,
                                         out List<RunTimeItemData> oldItemDataList)
        {
            Debug.Log("尝试交换大物品到小物品");
            oldItemDataList = new List<RunTimeItemData>();
            tempItemList.Clear();

            var largeItemData = plan.aItemData;
            // var smallItemData = plan.bItemData; 这里不会用到小物品数据 因为会收集到tempItemList中

            // 获取大物品会覆盖多少小物品
            var largeSize = GetOccupiedSize(largeItemData);

            for (int x = 0; x < largeSize.x; x++)
            {
                for (int y = 0; y < largeSize.y; y++)
                {
                    var pos = new Vector2Int(placeAnchorPos.x + x, placeAnchorPos.y + y);
                    var item = GetItemByMask(pos);
                    if (item is not null && item.InstancedItemId != plan.aItemData.InstancedItemId)
                    {
                        tempItemList.Add(item);
                        oldItemDataList.Add(item);
                    }
                }
            }

            // 清空大小物品的锚点
            RemoveAt(largeItemData.AnchorPos);
            foreach (var item in tempItemList)
                RemoveAt(item.AnchorPos);

            // 如果空间足够 大物品放在哪就是哪
            if (!CanPlace(largeItemData, placeAnchorPos))
            {
                return false;
            }
            // 放置大物品到新锚点
            SetAnchorItem(placeAnchorPos, largeItemData);

            foreach (var item in tempItemList)
            {
                bool placed = false;

                // 未被覆盖时，优先尝试放回旧大物品区域
                for (int i = largeItemData.AnchorPos.x; i < largeItemData.AnchorPos.x + largeItemData.DataSize.x && !placed; i++)
                {
                    for (int j = largeItemData.AnchorPos.y; j < largeItemData.AnchorPos.y + largeItemData.DataSize.y; j++)
                    {
                        // 先放能放的
                        var candidate = new Vector2Int(i, j);
                        if (!CanPlace(item, candidate)) continue;

                        SetAnchorItem(candidate, item);
                        placed = true;
                        break;
                    }
                }

                // 如果小物品被大物品的新位置所覆盖 则尝试放到背包之中任意第一个可放置位置
                if (!placed && FindSetAtFirst(item, out Vector2Int anchorPos))
                {
                    SetAnchorItem(anchorPos, item);
                    placed = true;
                }

                if (!placed)
                {
                    return false;
                }
            }

            return true;
        }
        #endregion

        #region 小换大
        private bool SwapSmallToLargeItem(SwapPlan plan, Vector2Int placeAnchorPos)
        {
            var smallItemData = plan.aItemData;
            var largeItemData = plan.bItemData;
            Debug.Log("尝试交换小物品到大物品");

            // 清空大小物品在网格上的锚点
            RemoveAt(smallItemData.AnchorPos);
            RemoveAt(largeItemData.AnchorPos);

            // 小物品放置到想放置的位置
            if (!CanPlace(smallItemData, placeAnchorPos))
                return false;

            SetAnchorItem(placeAnchorPos, smallItemData);

            // 大物品尝试放到小物品的位置
            // 如果不能放则在背包中找空位去放
            if (!CanPlace(largeItemData, smallItemData.AnchorPos))
            {
                if (FindSetAtFirst(largeItemData, out Vector2Int anchorPos))
                {
                    SetAnchorItem(anchorPos, largeItemData);
                    return true;
                }
                return false;
            }

            SetAnchorItem(smallItemData.AnchorPos, largeItemData);

            return true;

        }
        #endregion

        public SwapPlan GetSwapPlan(RunTimeItemData aItemData, RunTimeItemData bItemData)
        {
            // 判空
            if (aItemData == null || bItemData == null) return new SwapPlan { SwapState = ESwapState.CanNotSwap };
            if (aItemData.InstancedItemId == bItemData.InstancedItemId) return new SwapPlan { SwapState = ESwapState.CanNotSwap };
            // if (aItemData.IsRotated != bItemData.IsRotated) return new SwapPlan { SwapState = ESwapState.CanNotSwap };

            // 判越界
            var aAnchorPos = aItemData.AnchorPos;
            var bAnchorPos = bItemData.AnchorPos;
            if (!IsInside(aAnchorPos) || !IsInside(bAnchorPos)) return new SwapPlan { SwapState = ESwapState.CanNotSwap };
            if (aAnchorPos == bAnchorPos) return new SwapPlan { SwapState = ESwapState.CanNotSwap };

            SwapPlan swapPlan = new();

            // 判尺寸 构建不同的交换计划
            var aSize = aItemData.DataSize.x * aItemData.DataSize.y;
            var bSize = bItemData.DataSize.x * bItemData.DataSize.y;
            if (aSize == bSize)
            {
                swapPlan.SwapState = ESwapState.Same;
                swapPlan.aItemData = aItemData;
                swapPlan.bItemData = bItemData;
                return swapPlan;
            }
            else if (aSize > bSize)
            {
                swapPlan.SwapState = ESwapState.LargeToSmall;
                swapPlan.aItemData = aItemData;
                swapPlan.bItemData = bItemData;
                return swapPlan;
            }
            else
            {
                swapPlan.SwapState = ESwapState.SmallToLarge;
                swapPlan.aItemData = aItemData;
                swapPlan.bItemData = bItemData;
                return swapPlan;
            }
        }
        #endregion

        #region 删除功能
        /// <summary>
        /// 移除物品
        /// </summary>
        public bool RemoveAt(Vector2Int anchorPos)
        {
            return RemoveAnchorItem(anchorPos);
        }

        /// <summary>
        /// 移除物品
        /// </summary>
        public bool RemoveAtAny(Vector2Int pos)
        {
            // 获取覆盖当前pos的物品
            var targetItem = GetItemByMask(pos);
            if (targetItem is null) return false;

            // 遍历数组找到其锚点
            int anchorIndex = Array.FindIndex(
                runTimeItemDataArray,
                anchorItem => anchorItem != null && anchorItem.InstancedItemId == targetItem.InstancedItemId);

            if (anchorIndex == -1) return false;
            return RemoveAnchorItem(ToPosition(anchorIndex));
        }
        #endregion

        #region 查询功能
        /// <summary>
        /// 获取格子物品
        /// </summary>
        public RunTimeItemData GetItemAt(Vector2Int pos)
        {
            return IsInside(pos) ? runTimeItemDataArray[ToIndex(pos)] : null;
        }
        /// <summary>
        /// 获取格子上的物品
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public RunTimeItemData GetItemByMask(Vector2Int pos)
        {
            if (!IsInside(pos)) return null;

            return occupancyOwnerArray[ToIndex(pos)];
        }

        /// <summary>
        /// 判断a是否覆盖b
        /// </summary>
        /// <param name="aAnchorPos">a的锚点</param>
        /// <param name="bAnchorPos">b的锚点</param>
        /// <param name="aSize">a的尺寸</param>
        /// <param name="bSize">b的尺寸</param>
        /// <returns></returns>
        public bool IsCover(Vector2Int aAnchorPos, Vector2Int bAnchorPos, Vector2Int aSize, Vector2Int bSize)
        {
            bool noOverlap =
                aAnchorPos.x + aSize.x <= bAnchorPos.x ||
                bAnchorPos.x + bSize.x <= aAnchorPos.x ||
                aAnchorPos.y + aSize.y <= bAnchorPos.y ||
                bAnchorPos.y + bSize.y <= aAnchorPos.y;
            return !noOverlap;
        }
        #endregion
    }
}