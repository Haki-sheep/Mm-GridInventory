using System;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品分类
    /// </summary>
    [Serializable]
    public enum EItemType :byte
    {
        Equipment,
        Material,
        Consumable,
        Container
    }


    /// <summary>
    /// 物品堆叠类型
    /// </summary>
    [Serializable]
    public enum EItemStackType :byte
    {
        Single,
        Stackable
    }

    /// <summary>
    /// 物品数据根基类
    /// </summary>
    
    public interface IItemRootData 
    {   
        // ID
        public int ItemId { get; }
        public string Name { get; }

        // 图片路径
        public string IconPath { get;}

        // 尺寸
        public Vector2Int DataSize { get;}

        // 类型
        public EItemType ItemType { get; }
        public EItemStackType ItemStackType { get;}
        public int MaxStackCount { get;}

        // 容器内部高度 非容器为0
        public int ContainerHeight { get; }

    }
}
