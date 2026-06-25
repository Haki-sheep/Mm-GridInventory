using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// 物品配置 JSON 导入导出
    /// </summary>
    public static class ItemDataJsonIO
    {
        private static readonly JsonSerializerSettings JsonSettings = new()
        {
            Formatting = Formatting.Indented,
            NullValueHandling = NullValueHandling.Ignore
        };

        /// <summary>
        /// 导出到 JSON 文件
        /// </summary>
        public static void ExportToFile(ItemBaseDataListSo listSo, IReadOnlyList<string> itemTypeNameList, string filePath)
        {
            var exportFile = BuildExportFile(listSo, itemTypeNameList);
            string json = JsonConvert.SerializeObject(exportFile, JsonSettings);
            File.WriteAllText(filePath, json);
        }

        /// <summary>
        /// 从 JSON 文件导入
        /// </summary>
        public static ItemDataExportFile ImportFromFile(string filePath)
        {
            string json = File.ReadAllText(filePath);
            return JsonConvert.DeserializeObject<ItemDataExportFile>(json, JsonSettings);
        }

        /// <summary>
        /// 构建导出对象
        /// </summary>
        public static ItemDataExportFile BuildExportFile(ItemBaseDataListSo listSo, IReadOnlyList<string> itemTypeNameList)
        {
            var exportFile = new ItemDataExportFile();
            if (itemTypeNameList != null)
            {
                for (int i = 0; i < itemTypeNameList.Count; i++)
                    exportFile.itemTypes.Add(itemTypeNameList[i]);
            }

            if (listSo == null) return exportFile;

            var itemList = listSo.ItemDataList;
            for (int i = 0; i < itemList.Count; i++)
                AppendItemEntry(exportFile, itemList[i]);

            return exportFile;
        }

        private static void AppendItemEntry(ItemDataExportFile exportFile, IItemBaseData item)
        {
            exportFile.items.Add(new ItemDataJsonEntry
            {
                excelItemId = item.ExcelItemId,
                name = item.Name,
                iconPath = item.IconPath,
                dataSizeX = item.DataSize.x,
                dataSizeY = item.DataSize.y,
                itemType = item.ItemType.ToString(),
                itemStackType = item.ItemStackType.ToString(),
                maxStackCount = item.MaxStackCount
            });
        }

        /// <summary>
        /// JSON 转运行时列表
        /// </summary>
        public static List<ItemBaseData> ToItemBaseDataList(IReadOnlyList<ItemDataJsonEntry> entryList)
        {
            var itemList = new List<ItemBaseData>();
            if (entryList == null) return itemList;

            for (int i = 0; i < entryList.Count; i++)
            {
                var entry = entryList[i];
                if (!Enum.TryParse(entry.itemType, out EItemType itemType))
                {
                    Debug.LogWarning($"未知 ItemType {entry.itemType} 已回退为 Equipment");
                    itemType = EItemType.Equipment;
                }

                if (!Enum.TryParse(entry.itemStackType, out EItemStackType stackType))
                {
                    Debug.LogWarning($"未知 ItemStackType {entry.itemStackType} 已回退为 NoStackable");
                    stackType = EItemStackType.NoStackable;
                }

                itemList.Add(ItemBaseData.Create(
                    entry.excelItemId,
                    entry.name,
                    entry.iconPath,
                    new Vector2Int(entry.dataSizeX, entry.dataSizeY),
                    itemType,
                    stackType,
                    entry.maxStackCount));
            }

            return itemList;
        }
    }
}
