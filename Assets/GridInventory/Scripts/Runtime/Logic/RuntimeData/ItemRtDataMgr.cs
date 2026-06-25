using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MmInventory
{
    public class ItemRtDataMgr : MonoBehaviour
    {
        public static ItemRtDataMgr Instance { get; private set; }

        private Dictionary<int, IItemBaseData> itemDataDict = new();
        public IReadOnlyDictionary<int, IItemBaseData> ItemDataDict => itemDataDict;

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
            const string configLabel = "ConfigSo";
            var handle = Addressables.LoadAssetsAsync<ItemBaseDataListSo>(configLabel, null);
            handle.WaitForCompletion();

            if (handle.Status != AsyncOperationStatus.Succeeded)
            {
                Debug.LogError("加载物品配置失败：" + handle.OperationException);
                Addressables.Release(handle);
                return;
            }

            itemDataDict.Clear();
            IList<ItemBaseDataListSo> dataListList = handle.Result;
            Debug.Log($"读取到 {dataListList.Count} 个物品配置表");

            for (int i = 0; i < dataListList.Count; i++)
            {
                var dataList = dataListList[i];
                foreach (var item in dataList.ItemDataList)
                    itemDataDict[item.ExcelItemId] = item;
            }

            Addressables.Release(handle);
            Debug.Log($"物品数据注册完成 总计加载 {itemDataDict.Count} 条物品");
        }

        /// <summary>
        /// 根据id获取物品数据
        /// </summary>
        public T GetItemData<T>(int id) where T : IItemBaseData
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
