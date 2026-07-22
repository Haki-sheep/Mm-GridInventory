using System;

namespace MmInventory
{
    /// <summary>
    /// 物品分类枚举
    /// </summary>
    [Serializable]
    public enum EItemType : byte
    {
        Equipment,
        Weapon,
        Material,
        FoodOrWater,
        Medicine
    }
}
