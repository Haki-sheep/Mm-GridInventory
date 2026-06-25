using System;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品配置数据
    /// </summary>
    [Serializable]
    public class ItemBaseData : IItemBaseData
    {
        /// <summary> 物品Excel ID </summary>
        [SerializeField]
        private int excelItemId;

        /// <summary> 物品名称 </summary>
        [SerializeField]
        private string name;

        /// <summary> 图标路径 </summary>
        [SerializeField]
        private string iconPath;

        /// <summary> 网格占用尺寸 </summary>
        [SerializeField]
        private Vector2Int dataSize;

        /// <summary> 物品类型 </summary>
        [SerializeField]
        private EItemType itemType;

        /// <summary> 堆叠类型 </summary>
        [SerializeField]
        private EItemStackType itemStackType = EItemStackType.NoStackable;

        /// <summary> 最大堆叠数 </summary>
        [SerializeField]
        private int maxStackCount = 1;

        public int ExcelItemId { get => excelItemId; internal set => excelItemId = value; }
        public string Name { get => name; internal set => name = value; }
        public string IconPath { get => iconPath; internal set => iconPath = value; }
        public Vector2Int DataSize { get => dataSize; internal set => dataSize = value; }
        public EItemType ItemType { get => itemType; internal set => itemType = value; }
        public EItemStackType ItemStackType { get => itemStackType; internal set => itemStackType = value; }
        public int MaxStackCount { get => maxStackCount; internal set => maxStackCount = value; }

        /// <summary>
        /// 创建配置数据
        /// </summary>
        /// <param name="excelItemId"></param>
        /// <param name="name"></param>
        /// <param name="iconPath"></param>
        /// <param name="dataSize"></param>
        /// <param name="itemType"></param>
        /// <param name="itemStackType"></param>
        /// <param name="maxStackCount"></param>
        /// <returns></returns>
        public static ItemBaseData Create(int excelItemId,
                                          string name,
                                          string iconPath,
                                          Vector2Int dataSize,
                                          EItemType itemType,
                                          EItemStackType itemStackType,
                                          int maxStackCount)
        {
            return new ItemBaseData
            {
                excelItemId = excelItemId,
                name = name,
                iconPath = iconPath,
                dataSize = dataSize,
                itemType = itemType,
                itemStackType = itemStackType,
                maxStackCount = maxStackCount
            };
        }
    }
}
