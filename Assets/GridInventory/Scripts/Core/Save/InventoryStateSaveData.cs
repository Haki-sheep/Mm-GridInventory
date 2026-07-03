using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace MmInventory
{
     /// <summary>
    /// 单个背包存档
    /// </summary>
    public class InventoryStateSaveData
    {
        /// <summary> 背包ID </summary>
        public int containerId;

        /// <summary> 背包尺寸 </summary>
        public Vector2Int gridSize;

        /// <summary> 物品列表 </summary>
        public List<ItemSaveData> itemSaveDataList = new();
    }
}