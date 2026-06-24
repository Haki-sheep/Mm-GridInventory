using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MmInventory
{
    public class RunTimeItemDataMgr : MonoBehaviour
    {
        public static RunTimeItemDataMgr Instance { get; private set; }

        private Dictionary<int, IItemRootData> itemDataDict = new();
        public IReadOnlyDictionary<int, IItemRootData> ItemDataDict => itemDataDict;

        private void Awake()
        {
            Instance = this;
            RegisterItemData();
        }

        /// <summary>
        /// 注册所有物品数据
        /// </summary>
        public void RegisterItemData()
        {
            string configLabel = "ConfigSo";
            var handle = Addressables.LoadAssetsAsync<ScriptableObject>(configLabel);
            handle.Completed += (completedHandle) =>
            {
                if (completedHandle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError("加载物品配置失败：" + completedHandle.OperationException);
                    Addressables.Release(completedHandle);
                    return;
                }

                itemDataDict.Clear();
                Debug.Log($"读取到 {completedHandle.Result.Count} 个物品配置表");

                foreach (var listAsset in completedHandle.Result)
                {
                    if (listAsset is not ItemBaseDataList dataList) continue;

                    foreach (var item in dataList.ItemDataList)
                        itemDataDict[item.ItemId] = item;
                }

                Addressables.Release(completedHandle);
                Debug.Log($"物品数据注册完成 总计加载 {itemDataDict.Count} 条物品");
            };

            handle.WaitForCompletion();
        }

        /// <summary>
        /// 根据id获取物品数据
        /// </summary>
        public T GetItemData<T>(int id) where T : IItemRootData
        {
            if (itemDataDict.Count == 0)
            {
                Debug.LogError("物品数据未加载完成");
                return default;
            }

            if (itemDataDict.TryGetValue(id, out var data))
                return (T)data;

            Debug.LogWarning($"未找到ID:{id} 的物品");
            return default;
        }
    }
}
