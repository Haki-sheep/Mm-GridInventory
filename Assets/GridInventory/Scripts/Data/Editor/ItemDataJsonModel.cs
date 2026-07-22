using System;
using System.Collections.Generic;

namespace MmInventory.Editor
{
    /// <summary>
    /// JSON 导出根对象
    /// </summary>
    [Serializable]
    public sealed class ItemDataExportFile
    {
        public List<string> itemTypes = new();
        public List<ItemDataJsonEntry> items = new();
    }

    /// <summary>
    /// 单条物品 JSON
    /// </summary>
    [Serializable]
    public sealed class ItemDataJsonEntry
    {
        public int excelItemId;
        public string name;
        public string iconPath;
        public int dataSizeX;
        public int dataSizeY;
        public string itemType;
        public string itemRarity;
        public string itemStackType;
        public int maxStackCount;
    }
}
