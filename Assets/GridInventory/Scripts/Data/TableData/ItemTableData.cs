using System;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品配置数据
    /// </summary>
    [Serializable]
    public class ItemTableData : IItemTableData
    {
        /// <summary> 物品Excel ID </summary>
        [SerializeField]
        [ChineseLabel("物品ID")]
        private int excelItemId;

        /// <summary> 物品名称 </summary>
        [SerializeField]
        [ChineseLabel("名称")]
        private string name;

        /// <summary> 图标路径 </summary>
        [SerializeField]
        [ChineseLabel("图标路径")]
        private string iconPath;

        /// <summary> 网格占用尺寸 </summary>
        [SerializeField]
        [ChineseLabel("网格尺寸")]
        private Vector2Int dataSize;

        /// <summary> 物品类型 </summary>
        [SerializeField]
        [ChineseLabel("物品类型")]
        private EItemType itemType;

        /// <summary> 稀有度 </summary>
        [SerializeField]
        [ChineseLabel("稀有度")]
        private EItemRarity itemRarity = EItemRarity.White;

        /// <summary> 堆叠类型 </summary>
        [SerializeField]
        [ChineseLabel("堆叠类型")]
        private EItemStackType itemStackType = EItemStackType.NoStackable;

        /// <summary> 最大堆叠数 </summary>
        [SerializeField]
        [ChineseLabel("最大堆叠数")]
        private int maxStackCount = 1;

        public int ExcelItemId { get => excelItemId; internal set => excelItemId = value; }
        public string Name { get => name; internal set => name = value; }
        public string IconPath { get => iconPath; internal set => iconPath = value; }
        public Vector2Int DataSize { get => dataSize; internal set => dataSize = value; }
        public EItemType ItemType { get => itemType; internal set => itemType = value; }
        public EItemRarity ItemRarity { get => itemRarity; internal set => itemRarity = value; }
        public EItemStackType ItemStackType { get => itemStackType; internal set => itemStackType = value; }
        public int MaxStackCount { get => maxStackCount; internal set => maxStackCount = value; }

        /// <summary>
        /// 创建配置数据
        /// </summary>
        public static ItemTableData Create(int excelItemId,
                                          string name,
                                          string iconPath,
                                          Vector2Int dataSize,
                                          EItemType itemType,
                                          EItemStackType itemStackType,
                                          int maxStackCount,
                                          EItemRarity itemRarity = EItemRarity.White)
        {
            return new ItemTableData
            {
                excelItemId = excelItemId,
                name = name,
                iconPath = iconPath,
                dataSize = dataSize,
                itemType = itemType,
                itemRarity = itemRarity,
                itemStackType = itemStackType,
                maxStackCount = maxStackCount
            };
        }
    }
}
