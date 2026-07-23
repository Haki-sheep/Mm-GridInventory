using System;

namespace MmInventory
{
    /// <summary>
    /// 物品表 ID 分段规则
    /// 每种类型占 1000 号段
    /// </summary>
    public static class ItemTableIdRange
    {
        /// <summary> 单类型号段容量 </summary>
        public const int RangeSize = 1000;

        /// <summary>
        /// 按类型下标取号段
        /// </summary>
        public static void GetRangeByTypeIndex(int typeIndex, out int minId, out int maxId)
        {
            int safeIndex = Math.Max(0, typeIndex);
            minId = safeIndex * RangeSize + 1;
            maxId = (safeIndex + 1) * RangeSize;
        }

        /// <summary>
        /// 按枚举取号段
        /// </summary>
        public static void GetRange(EItemType itemType, out int minId, out int maxId)
        {
            GetRangeByTypeIndex((int)itemType, out minId, out maxId);
        }

        /// <summary>
        /// 由物品 ID 反推类型下标
        /// </summary>
        public static bool TryGetTypeIndex(int excelItemId, out int typeIndex)
        {
            if (excelItemId <= 0)
            {
                typeIndex = 0;
                return false;
            }

            typeIndex = (excelItemId - 1) / RangeSize;
            return true;
        }

        /// <summary>
        /// 判断 ID 是否落在指定类型号段
        /// </summary>
        public static bool IsInRange(int excelItemId, EItemType itemType)
        {
            GetRange(itemType, out int minId, out int maxId);
            return excelItemId >= minId && excelItemId <= maxId;
        }
    }
}
