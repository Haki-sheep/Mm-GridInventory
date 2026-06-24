using System;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品配置数据
    /// </summary>
    [Serializable]
    public class ItemBaseData : IItemRootData
    {
        /// <summary> 物品ID </summary>
        [SerializeField]
        private int itemId;

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
        private EItemStackType itemStackType = EItemStackType.Single;

        /// <summary> 最大堆叠数 </summary>
        [SerializeField]
        private int maxStackCount = 1;

        public int ItemId { get => itemId; internal set => itemId = value; }
        public string Name { get => name; internal set => name = value; }
        public string IconPath { get => iconPath; internal set => iconPath = value; }
        public Vector2Int DataSize { get => dataSize; internal set => dataSize = value; }
        public EItemType ItemType { get => itemType; internal set => itemType = value; }
        public EItemStackType ItemStackType { get => itemStackType; internal set => itemStackType = value; }
        public int MaxStackCount { get => maxStackCount; internal set => maxStackCount = value; }
    }
}
