using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 单件物品存档
    /// 这个类和ItemRtData是一一对应的
    /// </summary>
    public class ItemSaveData
    {
        /// <summary> 物品实例ID </summary>
        public string instancedItemId;

        /// <summary> 物品Excel ID </summary>
        public int excelItemId;

        /// <summary> 物品锚点位置 </summary>
        public Vector2Int anchorPos;

        /// <summary> 占格尺寸 </summary>
        public Vector2Int dataSize;

        /// <summary> 物品堆叠数量 </summary>
        public int hasStackCount;

        /// <summary> 最大堆叠数量 </summary>
        public int maxStackCount;

        /// <summary> 堆叠类型 </summary>
        public EItemStackType itemStackType;

        /// <summary> 物品是否旋转 </summary>
        public bool rotated;

        /// <summary> 物品所属背包ID </summary>
        public int containerId;

        /// <summary>
        /// 从运行时物品提取存档
        /// </summary>
        public static ItemSaveData FromItemRt(IItemRuntime item)
        {
            var save = new ItemSaveData
            {
                instancedItemId = item.InstancedItemId,
                excelItemId = item.ExcelItemId,
                anchorPos = item.AnchorPos,
                dataSize = item.DataSize,
                hasStackCount = item.CurrStackCount,
                maxStackCount = item.MaxStackCount,
                itemStackType = item.ItemStackType,
                rotated = item.IsRotated,
                containerId = item.ContainerId
            };

            return save;
        }
    }

    /// <summary>
    /// 单个背包存档
    /// </summary>
    public class InventorySaveData
    {
        /// <summary> 背包ID </summary>
        public int containerId;

        /// <summary> 背包尺寸 </summary>
        public Vector2Int gridSize;

        /// <summary> 物品列表 </summary>
        public List<ItemSaveData> itemSaveDataList = new();
    }
}
