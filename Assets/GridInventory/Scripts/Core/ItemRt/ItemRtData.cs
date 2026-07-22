using System;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 运行时物品数据
    /// 外部开发者可以更改此类的字段 但是一般别更改接口的属性 因为可能会影响算法层的使用
    /// </summary>
    public class ItemRtData : IItemRuntime
    {
        /// <summary> 物品实例ID 用于唯一标识一个物品 </summary>
        [SerializeField] private string instancedItemId;

        /// <summary> 配置表中的物品ID </summary>
        [SerializeField] private int excelItemId;

        /// <summary> 物品在背包中的锚点位置 </summary>
        [SerializeField] private Vector2Int anchorPos;

        /// <summary> 物品当前尺寸 </summary>
        [SerializeField] private Vector2Int dataSize;

        /// <summary> 当前堆叠数量 </summary>
        [SerializeField] private int curStackCount;

        /// <summary> 最大堆叠数量 </summary>
        [SerializeField] private int maxStackCount;

        /// <summary> 是否可堆叠 </summary>
        [SerializeField] private EItemStackType itemStackType;

        /// <summary> 稀有度 抽取与存档用 不做表现 </summary>
        [SerializeField] private EItemRarity itemRarity;

        /// <summary> 注意旋转只有两种情况 0 和 90 </summary>
        [SerializeField] private bool isRotated;

        public int ExcelItemId => excelItemId;
        public Vector2Int AnchorPos => anchorPos;
        public Vector2Int DataSize => dataSize;
        public bool IsRotated => isRotated;
        public string InstancedItemId => instancedItemId;
        public EItemStackType ItemStackType => itemStackType;
        public int MaxStackCount => maxStackCount;
        public EItemRarity ItemRarity => itemRarity;

        public int CurrStackCount
        {
            get => curStackCount;
            set => SetStackCount(value);
        }

        IItemRuntime IItemRuntime.Clone(int stackCount) => Clone(stackCount);

        /// <summary>
        /// 构造函数
        /// </summary>
        public ItemRtData(int excelItemId,
                          Vector2Int dataSize,
                          int curStackCount,
                          bool isRotated,
                          int maxStackCount = 1,
                          EItemStackType itemStackType = EItemStackType.NoStackable,
                          string instancedItemId = null,
                          EItemRarity itemRarity = EItemRarity.White)
        {
            this.excelItemId = excelItemId;
            this.dataSize = dataSize;
            this.curStackCount = curStackCount;
            this.isRotated = isRotated;
            this.maxStackCount = maxStackCount;
            this.itemStackType = itemStackType;
            this.itemRarity = itemRarity;
            this.instancedItemId = string.IsNullOrEmpty(instancedItemId)
                ? Guid.NewGuid().ToString()
                : instancedItemId;
        }

        /// <summary>
        /// 将存档数据转换为运行时数据
        /// </summary>
        public static ItemRtData ItemSaveData2ItemRtData(ItemSaveData save)
        {
            var item = new ItemRtData(
                save.excelItemId,
                save.dataSize,
                save.hasStackCount,
                save.rotated,
                save.maxStackCount,
                save.itemStackType,
                save.instancedItemId,
                save.itemRarity);

            item.SetAnchorPos(save.anchorPos);
            return item;
        }

        /// <summary>
        /// 将配置表数据转换为运行时数据
        /// </summary>
        public static ItemRtData ItemTableData2ItemRtData(IItemTableData config,
                                            int curStackCount = 1,
                                            bool isRotated = false)
        {
            return new ItemRtData(config.ExcelItemId,
                                    config.DataSize,
                                    curStackCount,
                                    isRotated,
                                    config.MaxStackCount,
                                    config.ItemStackType,
                                    null,
                                    config.ItemRarity);
        }

        /// <summary>
        /// 设置物品在背包中的锚点位置 
        /// 此方法用于算法层 不要让View层调用此方法
        /// </summary>
        public void SetAnchorPos(Vector2Int newAnchorPos)
        {
            anchorPos = newAnchorPos;
        }

        /// <summary>
        /// 设置旋转状态
        /// 此方法用于算法层 不要让View层调用此方法
        /// </summary>
        public void SetRotated(bool rotated)
        {
            if (isRotated == rotated) return;

            isRotated = rotated;
            // 掉换xy
            dataSize = new Vector2Int(dataSize.y, dataSize.x);
        }

        /// <summary>
        /// 设置堆叠数量
        /// 此方法用于算法层 不要让View层调用此方法
        /// </summary>
        public void SetStackCount(int count)
        {
            curStackCount = Mathf.Max(0, count);
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
                maxStackCount,
                itemStackType,
                null,
                itemRarity);
        }

    }
}
