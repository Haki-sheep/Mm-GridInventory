using System;
using UnityEngine;

namespace MmInventory
{

    /// <summary>
    /// 物品数据接口
    /// 用于定义所有物品都应该存在的基础数据
    /// </summary>

    public interface IItemBaseData
    {
        // ID
        public int ExcelItemId { get; }

        // 物品名称
        public string Name { get; }

        // 图片路径
        public string IconPath { get; }

        // 尺寸
        public Vector2Int DataSize { get; }

        // 类型
        public EItemType ItemType { get; }
        
        // 堆叠类型
        public EItemStackType ItemStackType { get; }
        
        // 最大堆叠数量
        public int MaxStackCount { get; }

    }

    /// <summary>
    /// 物品堆叠类型枚举
    /// </summary>
    [Serializable]
    public enum EItemStackType : byte
    {
        NoStackable,
        Stackable
    }

}
