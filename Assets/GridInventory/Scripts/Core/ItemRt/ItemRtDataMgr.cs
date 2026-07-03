using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    public class ItemRtDataMgr : MonoBehaviour
    {
        public static ItemRtDataMgr Instance { get; private set; }

        [SerializeField]
        private ItemTableDataListSo listSo;

        /// <summary> 物品数据字典 key物品ExcelID value物品配置表数据 </summary>
        private Dictionary<int, IItemTableData> itemDataDict = new();
        public IReadOnlyDictionary<int, IItemTableData> ItemDataDict => itemDataDict;

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
            if (listSo == null)
                listSo = ItemTableDataListSo.Instance;

            itemDataDict.Clear();
            if (listSo == null)
            {
                Debug.LogError("ItemBaseDataListSo 未配置");
                return;
            }

            var itemList = listSo.ItemDataList;
            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                itemDataDict[item.ExcelItemId] = item;
            }

            Debug.Log($"物品数据注册完成 总计加载 {itemDataDict.Count} 条物品");
        }

        /// <summary>
        /// 根据id获取物品数据
        /// </summary>
        public T GetItemData<T>(int id) where T : IItemTableData
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
