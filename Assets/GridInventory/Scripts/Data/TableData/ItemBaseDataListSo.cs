using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 物品配表总库
    /// 运行时唯一模版数据源
    /// </summary>
    [CreateAssetMenu(fileName = "ItemBaseDataList", menuName = "MmInventory/Data/Item Base Data List")]
    public class ItemBaseDataListSo : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/GridInventory/SoDatas/ItemBaseDataList.asset";

        private static ItemBaseDataListSo instance;

        /// <summary> 运行时单例 </summary>
        public static ItemBaseDataListSo Instance => instance;

        [SerializeField]
        private List<ItemBaseData> itemDataList = new();

        public IReadOnlyList<ItemBaseData> ItemDataList => itemDataList;

        private void OnEnable()
        {
            instance = this;
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器替换全部物品
        /// </summary>
        public void EditorReplaceItems(List<ItemBaseData> items)
        {
            itemDataList.Clear();
            if (items != null)
                itemDataList.AddRange(items);
        }

        /// <summary>
        /// 编辑器添加物品
        /// </summary>
        public void EditorAddItem(ItemBaseData item)
        {
            if (item == null) return;
            itemDataList.Add(item);
        }

        /// <summary>
        /// 编辑器删除物品
        /// </summary>
        public void EditorRemoveAt(int index)
        {
            if (index < 0 || index >= itemDataList.Count) return;
            itemDataList.RemoveAt(index);
        }

        /// <summary>
        /// 编辑器保存资产
        /// </summary>
        public void EditorSave()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}
