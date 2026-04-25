using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace MmInventory
{
    public class RunTimeItemDataMgr : SingletonMono<RunTimeItemDataMgr>
    {
        private Dictionary<int, IItemRootData> itemDataDict = new();
        public IReadOnlyDictionary<int, IItemRootData> ItemDataDict => itemDataDict;

        protected override void Awake()
        {
            base.Awake();
            RegisterItemData();
        }
        
        /// <summary>
        /// 注册所有物品数据
        /// </summary>
        public void RegisterItemData()
        {
            //  使用资源上真实存在的标签 ConfigSo
            string configLabel = "ConfigSo";

            //  先加载4个列表容器本体
            var handle = Addressables.LoadAssetsAsync<ScriptableObject>(configLabel);
            handle.Completed += (handle) =>
            {
                if (handle.Status != AsyncOperationStatus.Succeeded)
                {
                    Debug.LogError("加载物品配置失败：" + handle.OperationException);
                    Addressables.Release(handle);
                    return;
                }

                itemDataDict.Clear();
                Debug.Log($"读取到 {handle.Result.Count} 个物品配置表");

                // 遍历容器，拆出内部所有单个物品数据
                foreach (var listAsset in handle.Result)
                {
                    switch (listAsset)
                    {
                        case ConsumableItemBaseDataList cList:
                            foreach (var item in cList.itemList) itemDataDict[item.ItemId] = item;
                            break;
                        
                        case MaterialItemBaseDataList mList:
                            foreach (var item in mList.itemList) itemDataDict[item.ItemId] = item;
                            break;
                        
                        case EquipmentItemBaseDataList eList:
                            foreach (var item in eList.itemList) itemDataDict[item.ItemId] = item;
                            break;
                        
                        case ContainerItemBaseDataList conList:
                            foreach (var item in conList.itemList) itemDataDict[item.ItemId] = item;
                            break;
                    }
                }

                Addressables.Release(handle);
                Debug.Log($" 物品数据注册完成，总计加载 {itemDataDict.Count} 条物品");
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
            {
                return (T)data;
            }
            
            Debug.LogWarning($"未找到ID:{id} 的物品");
            return default;
        }
    }
}