using System.Collections.Generic;
using UnityEngine;
using System;
namespace MmInventory
{
    /// <summary>
    /// 容器类物品的数据资产基类 
    /// 用于定义可以打开并持有子背包的物品 
    /// </summary>
    [Serializable]
    public class ContainerItemBaseData : IItemRootData
    {
        [SerializeField] private int itemId;
        [SerializeField] private string name;
        [SerializeField] private string iconPath;
        [SerializeField] private Vector2Int dataSize;
        [SerializeField] private int height;

        [SerializeField] private EItemType itemType;

        [SerializeField] private EItemStackType itemStackType = EItemStackType.Single;

        [SerializeField] private int maxStackCount = 1;
        public int ItemId { get => itemId; internal set => itemId = value; }
        public string Name { get => name; internal set => name = value; }
        public string IconPath { get => iconPath; internal set => iconPath = value; }
        public EItemType ItemType { get => itemType; internal set => itemType = value; }
        public Vector2Int DataSize { get => dataSize; internal set => dataSize = value; }
        public EItemStackType ItemStackType { get => itemStackType; internal set => itemStackType = value; }
        public int MaxStackCount { get => maxStackCount; internal set => maxStackCount = value; }
    }


}
