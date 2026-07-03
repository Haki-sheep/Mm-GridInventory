using System;
using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 交换状态
    /// </summary>
    public enum ESwapState
    {
        // 不能交换
        CanNotSwap,
        // 相同物品
        Same,
        // 大物品到小物品
        LargeToSmall,
        // 小物品到大物品
        SmallToLarge,
    }

    /// <summary>
    /// 交换放置模式
    /// </summary>
    public enum ESwapPlaceMode
    {
        // 同容器交换
        SameContainer,
        // 跨容器交换
        CrossContainer,
    }

    /// <summary>
    /// 交换结构体 
    /// aItemData: 交换的物品
    /// bItemData: 交换的物品
    /// SwapState: 交换状态
    /// </summary>
    public struct SwapPlan
    {
        public IItemRuntime aItemData;
        public IItemRuntime bItemData;
        public ESwapState SwapState;
    }

    /// <summary>
    /// 这里是背包的核心算法与数据结构
    /// </summary>
    public partial class InventoryState
    {
        private int inventoryStateId;
        private readonly Vector2Int inventorySize;

        /// <summary>锚点数组 运行时记录每一个物品的锚点在哪里</summary>
        private IItemRuntime[] itemAnchorArray;

        /// <summary>物品的全部占用格子信息</summary>
        private IItemRuntime[] occupancyOwnerArray;

        private readonly InventorySwapService inventorySwapService;
        private readonly InventoryPlacementService inventoryPlacementService;
        private readonly InventoryStackableService inventoryStackableService;

        /// <summary>
        /// 初始化背包状态
        /// </summary>
        /// <param name="gridInventorySize">背包尺寸</param>
        public InventoryState(Vector2Int gridInventorySize)
        {
            this.inventorySize = gridInventorySize;
            int totalCount = gridInventorySize.x * gridInventorySize.y;
            itemAnchorArray = new IItemRuntime[totalCount];
            occupancyOwnerArray = new IItemRuntime[totalCount];
            inventorySwapService = new InventorySwapService(this);
            inventoryPlacementService = new InventoryPlacementService(this);
            inventoryStackableService = new InventoryStackableService(this);
            crossContainerService = new InventoryCrossContainerService(this);
            quickMoveService = new InventoryQuickMoveService(this);
        }

        /// <summary>
        /// 二维坐标 → 一维索引
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        private int ToIndex(Vector2Int position) =>
            position.y * inventorySize.x + position.x;

        /// <summary>
        /// 一维索引 → 二维坐标
        /// </summary>
        /// <param name="index">一维索引</param>
        /// <returns>二维坐标</returns>
        private Vector2Int ToVector2Int(int index) =>
            new Vector2Int(index % inventorySize.x, index / inventorySize.x);

        /// <summary>
        /// 写入占用信息
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="anchorPos">锚点</param>
        /// <param name="occupied">是否占用</param>
        private void WriteOccupancy(IItemRuntime item,
                                    Vector2Int anchorPos,
                                    bool occupied)
        {
            if (item is null) return;

            // 获取物品的占用尺寸
            var occupiedSize = item.DataSize;

            // 遍历占用尺寸
            for (int x = 0; x < occupiedSize.x; x++)
            {
                for (int y = 0; y < occupiedSize.y; y++)
                {
                    // 计算目标位置
                    var targetPos = new Vector2Int(anchorPos.x + x, anchorPos.y + y);
                    if (!IsInside(targetPos)) continue;

                    // 计算目标位置的索引
                    int index = ToIndex(targetPos);

                    // 如果需要占用 则写入数组 如果不需要则情况对应位置
                    if (occupied)
                        occupancyOwnerArray[index] = item;
                    else
                        occupancyOwnerArray[index] = null;
                }
            }
        }

        /// <summary>
        /// 设置锚点物品并更新占用信息
        /// 锚点数据唯一写入口 物品锚点在此同步
        /// </summary>
        /// <param name="item">物品</param>
        /// <param name="anchorPos">锚点</param>
        private void SetItemData(IItemRuntime item, Vector2Int anchorPos)
        {
            itemAnchorArray[ToIndex(anchorPos)] = item;
            WriteOccupancy(item, anchorPos, true);
            item.SetAnchorPos(anchorPos);
        }

        /// <summary>
        /// 移除锚点物品并更新占用信息
        /// </summary>
        /// <param name="anchorPos">锚点</param>
        /// <returns>是否成功</returns>
        private bool RemoveItemData(Vector2Int anchorPos)
        {
            if (!IsInside(anchorPos)) return false;

            // 获取目标位置的索引
            int index = ToIndex(anchorPos);
            // 获取目标位置的物品
            var item = itemAnchorArray[index];
            // 如果物品为空 则返回失败
            if (item is null) return false;

            // 清空目标位置的物品
            itemAnchorArray[index] = null;
            // 更新占用信息
            WriteOccupancy(item, anchorPos, false);
            return true;
        }

        /// <summary>
        /// 范围判定
        /// </summary>
        /// <param name="position">位置</param>
        /// <returns>是否在背包范围内</returns>
        public bool IsInside(Vector2Int position)
        {
            return position.x >= 0 && position.x < inventorySize.x &&
                   position.y >= 0 && position.y < inventorySize.y;
        }

        /// <summary>
        /// 判断目标格子是否已被占用
        /// </summary>
        /// <param name="pos">格子坐标</param>
        /// <returns>是否被占用</returns>
        private bool IsOccupied(Vector2Int pos) =>
            occupancyOwnerArray[ToIndex(pos)] != null;

        #region 放置功能
        /// <summary>
        /// 放置判定
        /// </summary>
        public bool CanPlace(IItemRuntime item, Vector2Int anchorPos) =>
        inventoryPlacementService.CanPlace(item, anchorPos);

        /// <summary>
        /// 放置物品
        /// </summary>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <param name="itemData"> 物品数据 </param>
        public bool SetAt(Vector2Int anchorPos, IItemRuntime itemData) =>
        inventoryPlacementService.SetAt(anchorPos, itemData);

        /// <summary>
        /// 遍历所有格子 放置物品到第一个可放置位置
        /// </summary>
        /// <param name="itemData"> 物品数据 </param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns></returns>
        public bool FindSetAtFirst(IItemRuntime itemData, out Vector2Int anchorPos) =>
        inventoryPlacementService.FindSetAtFirst(itemData, out anchorPos);

        /// <summary>
        /// 遍历所有格子 找到第一个可放置位置 并放置物品
        /// 仅使用物品当前朝向
        /// </summary>
        /// <param name="itemData"> 物品数据 </param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns> 是否成功 </returns>
        public bool SetAtFirst(IItemRuntime itemData, out Vector2Int anchorPos) =>
        inventoryPlacementService.SetAtFirst(itemData, out anchorPos);

        /// <summary>
        /// 找首个空位并放置 当前朝向优先 放不下再旋转后重扫
        /// </summary>
        /// <param name="itemData"> 物品数据 </param>
        /// <param name="anchorPos"> 锚点坐标 </param>
        /// <returns> 是否成功 </returns>
        public bool SetAtFirstWithRotate(IItemRuntime itemData, out Vector2Int anchorPos) =>
        inventoryPlacementService.SetAtFirstWithRotate(itemData, out anchorPos);
        #endregion

        #region 堆叠功能
        /// <summary>
        /// 是否可堆叠
        /// </summary>
        public bool CanStack(IItemRuntime dragItem, IItemRuntime targetItem) =>
            inventoryStackableService.CanStack(dragItem, targetItem);

        /// <summary>
        /// 尝试堆叠
        /// </summary>
        public bool TryStack(IItemRuntime dragItem, IItemRuntime targetItem) =>
            inventoryStackableService.TryStack(dragItem, targetItem);

        /// <summary>
        /// 尝试拆分
        /// </summary>
        public bool TrySplit(IItemRuntime itemData, int splitCount) =>
            inventoryStackableService.TrySplit(itemData, splitCount);
        #endregion

        #region 交换功能

        public bool CanSwap(IItemRuntime aItemData,
                            IItemRuntime bItemData,
                            Vector2Int placeAnchorPos,
                            ESwapPlaceMode swapPlaceMode = ESwapPlaceMode.SameContainer) =>
        inventorySwapService.CanSwap(aItemData, bItemData, placeAnchorPos, swapPlaceMode);


        public bool TrySwap(IItemRuntime aItemData,
                            IItemRuntime bItemData,
                            List<IItemRuntime> oldItemDataList,
                            Vector2Int placeAnchorPos,
                            ESwapPlaceMode swapPlaceMode = ESwapPlaceMode.SameContainer) =>
        inventorySwapService.TrySwap(aItemData,
                                     bItemData,
                                     oldItemDataList,
                                     placeAnchorPos,
                                     swapPlaceMode);

        /// <summary>
        /// 获取两物品的交换类型
        /// </summary>
        /// <param name="aItemData">拖动物品</param>
        /// <param name="bItemData">目标物品</param>
        /// <returns>交换类型</returns>
        public ESwapState GetSwapState(IItemRuntime aItemData, IItemRuntime bItemData) =>
        inventorySwapService.GetSwapState(aItemData, bItemData);

        /// <summary>
        /// 尝试获取交换目标物品信息
        /// </summary>
        /// <param name="dragItemData">拖动物品</param>
        /// <param name="placeAnchorPos">放置锚点</param>
        /// <param name="swapTargetItem">交换目标物品</param>
        /// <returns>是否成功</returns>
        public bool TryGetSwapTargetItem(IItemRuntime dragItemData,
                                         Vector2Int placeAnchorPos,
                                         out IItemRuntime swapTargetItem) =>
        inventorySwapService.TryGetSwapTargetItem(dragItemData, placeAnchorPos, out swapTargetItem);


        /// <summary>
        /// 移除物品
        /// </summary>
        public bool RemoveAt(Vector2Int anchorPos) =>
        inventoryPlacementService.RemoveAt(anchorPos);

        /// <summary>
        /// 移除物品
        /// </summary>
        public bool RemoveAtAny(Vector2Int pos) =>
        inventoryPlacementService.RemoveAtAny(pos);
        #endregion

        #region 查询功能
        /// <summary>
        /// 获取格子物品
        /// </summary>
        public IItemRuntime GetItemAt(Vector2Int pos) =>
        inventoryPlacementService.GetItemAt(pos);
        /// <summary>
        /// 获取格子上的物品
        /// </summary>
        /// <param name="pos"></param>
        /// <returns></returns>
        public IItemRuntime GetItemByMask(Vector2Int pos) =>
        inventoryPlacementService.GetItemByMask(pos);

        /// <summary>
        /// 判断a是否覆盖b
        /// </summary>
        /// <param name="aAnchorPos">a的锚点</param>
        /// <param name="bAnchorPos">b的锚点</param>
        /// <param name="aSize">a的尺寸</param>
        /// <param name="bSize">b的尺寸</param>
        /// <returns></returns>
        public bool IsCover(Vector2Int aAnchorPos, Vector2Int bAnchorPos, Vector2Int aSize, Vector2Int bSize) =>
        inventoryPlacementService.IsCover(aAnchorPos, bAnchorPos, aSize, bSize);

        /// <summary>
        /// 收集所有锚点物品去重
        /// </summary>
        /// <param name="itemList">输出列表</param>
        public void CollectAnchorItems(List<IItemRuntime> itemList)
        {
            itemList.Clear();
            var idHashList = new HashSet<string>();
            for (int i = 0; i < itemAnchorArray.Length; i++)
            {
                var item = itemAnchorArray[i];
                if (item is null || !idHashList.Add(item.InstancedItemId))
                    continue;

                itemList.Add(item);
            }
        }
        #endregion

        #region 快照功能

        /// <summary>
        /// 背包快照 记录网格与物品锚点的完整状态
        /// </summary>
        public sealed class Snapshot
        {
            /// <summary> 锚点数组备份 </summary>
            internal IItemRuntime[] itemAnchorArray;
            /// <summary> 占用数组备份 </summary>
            internal IItemRuntime[] occupancyOwnerArray;
            /// <summary>
            /// 物品锚点备份字典
            /// </summary>
            internal Dictionary<IItemRuntime, Vector2Int> anchorDict;
        }

        /// <summary>
        /// 捕获当前背包快照
        /// </summary>
        /// <returns>快照对象</returns>
        public Snapshot CaptureSnapshot()
        {
            var snapshot = new Snapshot
            {
                itemAnchorArray = (IItemRuntime[])itemAnchorArray.Clone(),
                occupancyOwnerArray = (IItemRuntime[])occupancyOwnerArray.Clone(),
                anchorDict = new Dictionary<IItemRuntime, Vector2Int>()
            };

            foreach (var item in itemAnchorArray)
            {
                if (item is null) continue;
                snapshot.anchorDict[item] = item.AnchorPos;
            }

            return snapshot;
        }

        /// <summary>
        /// 还原背包到快照状态
        /// </summary>
        /// <param name="snapshot">快照对象</param>
        public void RestoreSnapshot(Snapshot snapshot)
        {
            if (snapshot is null) return;

            // 克隆一份写回 保证快照可重复使用
            itemAnchorArray = (IItemRuntime[])snapshot.itemAnchorArray.Clone();
            occupancyOwnerArray = (IItemRuntime[])snapshot.occupancyOwnerArray.Clone();

            foreach (var pair in snapshot.anchorDict)
                pair.Key.SetAnchorPos(pair.Value);
        }

        #endregion

        #region 存档功能

        /// <summary> 背包尺寸 </summary>
        public Vector2Int GridSize => inventorySize;

        /// <summary>
        /// 保存当前背包
        /// </summary>
        public void Save(int containerId, string filePath = null) =>
            PersisServiceInstance.Save(this, containerId, filePath);

        /// <summary>
        /// 读取背包存档
        /// </summary>
        public static InventoryState Load(int containerId, string filePath = null) =>
            PersisServiceInstance.Load(containerId, filePath);

        #endregion
    }
}