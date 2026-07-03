using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 背包持久化服务
    /// </summary>
    public partial class InventoryState
    {
        private static readonly InventoryPersisService PersisServiceInstance = new();

        /// <summary>
        /// 持久化服务类
        /// </summary>
        private sealed class InventoryPersisService
        {
            /// <summary>
            /// 默认文件名
            /// </summary>
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
                var saveData = JsonConvert.DeserializeObject<InventoryStateSaveData>(json);
                if (saveData == null)
                    return null;

                return RestoreInventoryState(saveData);
            }

            /// <summary>
            /// 构建存档数据
            /// InventoryState -> InventoryStateSaveData
            /// </summary>
            /// <param name="owner">背包持有者</param>
            /// <param name="containerId">容器ID</param>
            /// <returns>存档数据</returns>
            private static InventoryStateSaveData BuildSaveData(InventoryState owner, int containerId)
            {
                var saveData = new InventoryStateSaveData
                {
                    containerId = containerId,
                    gridSize = owner.inventorySize
                };

                var idHashList = new HashSet<string>();
                // 遍历背包持有者的锚点数组，将每个物品的存档数据添加到存档数据列表中
                for (int i = 0; i < owner.itemAnchorArray.Length; i++)
                {
                    var item = owner.itemAnchorArray[i];
                    if (item is null || !idHashList.Add(item.InstancedItemId))
                        continue;

                    saveData.itemSaveDataList.Add(ItemSaveData.ItemRtToItemSaveData(item));
                }

                return saveData;
            }

            /// <summary>
            /// 从存档重建背包
            /// InventoryStateSaveData -> InventoryState
            /// </summary>
            /// <param name="saveData">存档数据</param>
            /// <returns>背包状态</returns>
            private static InventoryState RestoreInventoryState(InventoryStateSaveData saveData)
            {
                var state = new InventoryState(saveData.gridSize);
                if (saveData.itemSaveDataList == null || saveData.itemSaveDataList.Count == 0)
                    return state;

                // 遍历存档数据列表，将每个物品的存档数据添加到背包状态中
                for (int i = 0; i < saveData.itemSaveDataList.Count; i++)
                {
                    var itemSaveData = saveData.itemSaveDataList[i];
                    if (itemSaveData == null) continue;

                    // 将物品存档数据转换为运行时数据
                    var item = ItemRtData.ItemSaveData2ItemRtData(itemSaveData);
                    // 将运行时数据放置到背包状态中
                    if (!state.SetAt(itemSaveData.anchorPos, item))
                        Debug.LogWarning($"存档物品放置失败 {itemSaveData.instancedItemId} @ {itemSaveData.anchorPos}");
                }

                return state;
            }

            /// <summary>
            /// 解析存档路径
            /// </summary>
            /// <param name="containerId">容器ID</param>
            /// <param name="filePath">文件路径</param>
            /// <returns>存档路径</returns>
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
