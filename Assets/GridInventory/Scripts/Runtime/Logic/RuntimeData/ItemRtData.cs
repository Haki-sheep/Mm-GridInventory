using System;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 运行时物品数据
    /// </summary>
    public class ItemRtData : IItemRuntime
    {
        /// <summary>
        /// 配置表中的物品ID
        /// </summary>
        [SerializeField] private int excelItemId;

        /// <summary>
        /// 物品在背包中的锚点位置
        /// </summary>
        [SerializeField] private Vector2Int anchorPos;

        /// <summary>
        /// 物品当前尺寸
        /// </summary>
        [SerializeField] private Vector2Int dataSize;

        /// <summary>
        /// 当前堆叠数量
        /// </summary>
        [SerializeField] private int curStackCount;

        /// <summary>
        /// 最大堆叠数量
        /// </summary>
        [SerializeField] private int maxStackCount;

        /// <summary>
        /// 是否可堆叠
        /// </summary>
        [SerializeField] private EItemStackType itemStackType;

        /// <summary>
        /// 注意旋转只有两种情况 0 和 90
        /// </summary>
        [SerializeField] private bool isRotated;

        /// <summary>
        /// 背包ID 只有容器类物品才有
        /// </summary>
        [SerializeField] private int containerId;

        /// <summary>
        /// 物品实例ID 用于唯一标识一个物品
        /// </summary>
        [SerializeField] private string instancedItemId;

        public int ExcelItemId => excelItemId;
        public Vector2Int AnchorPos => anchorPos;
        public Vector2Int DataSize => dataSize;
        public bool IsRotated => isRotated;
        public int ContainerId => containerId;
        public string InstancedItemId => instancedItemId;
        public EItemStackType ItemStackType => itemStackType;
        public int MaxStackCount => maxStackCount;

        public int CurrStackCount
        {
            get => curStackCount;
            set => SetStackCount(value);
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        public ItemRtData(int excelItemId,
                          Vector2Int dataSize,
                          int curStackCount,
                          bool isRotated,
                          int containerId,
                          int maxStackCount = 1,
                          EItemStackType itemStackType = EItemStackType.NoStackable,
                          string instancedItemId = null)
        {
            this.excelItemId = excelItemId;
            this.dataSize = dataSize;
            this.curStackCount = curStackCount;
            this.isRotated = isRotated;
            this.containerId = containerId;
            this.maxStackCount = maxStackCount;
            this.itemStackType = itemStackType;
            this.instancedItemId = string.IsNullOrEmpty(instancedItemId)
                ? Guid.NewGuid().ToString()
                : instancedItemId;
        }

        /// <summary>
        /// 从存档恢复
        /// </summary>
        public static ItemRtData FromSave(ItemSaveData save)
        {
            var item = new ItemRtData(
                save.excelItemId,
                save.dataSize,
                save.hasStackCount,
                save.rotated,
                save.containerId,
                save.maxStackCount,
                save.itemStackType,
                save.instancedItemId);

            item.SetAnchorPos(save.anchorPos);
            return item;
        }

        /// <summary>
        /// 从配置表创建
        /// </summary>
        public static ItemRtData FromConfig(IItemBaseData config,
                                            int curStackCount = 1,
                                            bool isRotated = false,
                                            int containerId = 0)
        {
            return new ItemRtData ( config.ExcelItemId,
                                    config.DataSize,
                                    curStackCount,
                                    isRotated,
                                    containerId,
                                    config.MaxStackCount,
                                    config.ItemStackType);
        }

        /// <summary>
        /// 设置物品在背包中的锚点位置
        /// </summary>
        public void SetAnchorPos(Vector2Int newAnchorPos)
        {
            anchorPos = newAnchorPos;
        }

        /// <summary>
        /// 设置旋转状态
        /// </summary>
        public void SetRotated(bool rotated)
        {
            if (isRotated == rotated) return;

            isRotated = rotated;
            dataSize = new Vector2Int(dataSize.y, dataSize.x);
        }

        /// <summary>
        /// 设置堆叠数量
        /// </summary>
        public void SetStackCount(int count)
        {
            curStackCount = Mathf.Max(0, count);
        }

        /// <summary>
        /// 设置当前所属背包容器ID
        /// </summary>
        public void SetContainer(int newContainerId)
        {
            containerId = newContainerId;
        }

        /// <summary>
        /// 拆出新堆实例 分配新的 InstancedItemId
        /// </summary>
        public ItemRtData Clone(int stackCount)
        {
            return new ItemRtData(
                excelItemId,
                dataSize,
                stackCount,
                isRotated,
                containerId,
                maxStackCount,
                itemStackType);
        }

        IItemRuntime IItemRuntime.Clone(int stackCount) => Clone(stackCount);
    }
}
