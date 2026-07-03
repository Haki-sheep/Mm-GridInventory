using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MmInventory
{
    public partial class InventoryState
    {
        private static readonly InventoryPersisService PersisServiceInstance = new();

        private sealed class InventoryPersisService
        {
            private const string DefaultFileName = "inventory_{0}.json";

            /// <summary>
            /// 保存背包状态
            /// </summary>
            public void Save(InventoryState owner, int containerId, string filePath)
            {
                var saveData = BuildSaveData(owner, containerId);
                string json = JsonConvert.SerializeObject(saveData, Formatting.Indented);
                string path = ResolveFilePath(containerId, filePath);
                File.WriteAllText(path, json);
            }

            /// <summary>
            /// 读取背包状态
            /// </summary>
            public InventoryState Load(int containerId, string filePath)
            {
                string path = ResolveFilePath(containerId, filePath);
                if (!File.Exists(path))
                    return null;

                string json = File.ReadAllText(path);
                var saveData = JsonConvert.DeserializeObject<InventorySaveData>(json);
                if (saveData == null)
                    return null;

                return RestoreInventoryState(saveData);
            }

            /// <summary>
            /// 构建存档数据
            /// </summary>
            private static InventorySaveData BuildSaveData(InventoryState owner, int containerId)
            {
                var saveData = new InventorySaveData
                {
                    containerId = containerId,
                    gridSize = owner.gridInventorySize
                };

                var idHashList = new HashSet<string>();
                for (int i = 0; i < owner.itemAnchorArray.Length; i++)
                {
                    var item = owner.itemAnchorArray[i];
                    if (item is null || !idHashList.Add(item.InstancedItemId))
                        continue;

                    saveData.itemSaveDataList.Add(ItemSaveData.FromItemRt(item));
                }

                return saveData;
            }

            /// <summary>
            /// 从存档重建背包
            /// </summary>
            private static InventoryState RestoreInventoryState(InventorySaveData saveData)
            {
                var state = new InventoryState(saveData.gridSize);
                if (saveData.itemSaveDataList == null || saveData.itemSaveDataList.Count == 0)
                    return state;

                for (int i = 0; i < saveData.itemSaveDataList.Count; i++)
                {
                    var entry = saveData.itemSaveDataList[i];
                    if (entry == null) continue;

                    var item = ItemRtData.FromSave(entry);
                    if (!state.SetAt(entry.anchorPos, item))
                        Debug.LogWarning($"存档物品放置失败 {entry.instancedItemId} @ {entry.anchorPos}");
                }

                return state;
            }

            /// <summary>
            /// 解析存档路径
            /// </summary>
            private static string ResolveFilePath(int containerId, string filePath)
            {
                if (!string.IsNullOrEmpty(filePath))
                    return filePath;

                return Path.Combine(
                    Application.persistentDataPath,
                    string.Format(DefaultFileName, containerId));
            }
        }
    }
}
