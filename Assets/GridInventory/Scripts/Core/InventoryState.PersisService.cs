using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 单件物品存档
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
                rotated = item.IsRotated
            };

            if (item is ItemRtData rtData)
                save.containerId = rtData.ContainerId;

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
