using System;
using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 尺寸与物品视图预制体映射条目
    /// </summary>
    [Serializable]
    public class ItemViewPrefabEntry
    {
        /// <summary> 网格占用宽 </summary>
        [SerializeField]
        private int width = 1;

        /// <summary> 网格占用高 </summary>
        [SerializeField]
        private int height = 1;

        /// <summary> 对应视图预制体 </summary>
        [SerializeField]
        private GameObject prefab;

        public Vector2Int Size => new Vector2Int(width, height);
        public GameObject Prefab => prefab;

#if UNITY_EDITOR
        public int Width
        {
            get => width;
            set => width = value;
        }

        public int Height
        {
            get => height;
            set => height = value;
        }

        public GameObject PrefabAsset
        {
            get => prefab;
            set => prefab = value;
        }
#endif
    }

    /// <summary>
    /// 物品视图预制体尺寸表
    /// 按 dataSize 解析背包格子上的 ItemView 预制体
    /// </summary>
    [CreateAssetMenu(fileName = "ItemViewPrefabList", menuName = "MmInventory/Data/Item View Prefab List")]
    public class ItemViewPrefabListSo : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/GridInventory/SoDatas/ItemViewPrefabList.asset";

        private static ItemViewPrefabListSo instance;

        /// <summary> 运行时单例 </summary>
        public static ItemViewPrefabListSo Instance => instance;

        [SerializeField]
        private List<ItemViewPrefabEntry> entryList = new();

        public IReadOnlyList<ItemViewPrefabEntry> EntryList => entryList;

        private void OnEnable()
        {
            instance = this;
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// 根据占用尺寸获取视图预制体
        /// </summary>
        public GameObject GetPrefab(Vector2Int size)
        {
            for (int i = 0; i < entryList.Count; i++)
            {
                var entry = entryList[i];
                if (entry.Size == size)
                    return entry.Prefab;
            }

            return null;
        }

        /// <summary>
        /// 尝试根据占用尺寸获取视图预制体
        /// </summary>
        public bool TryGetPrefab(Vector2Int size, out GameObject prefab)
        {
            prefab = GetPrefab(size);
            return prefab != null;
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器添加尺寸映射
        /// </summary>
        public void EditorAddEntry(Vector2Int size)
        {
            entryList.Add(new ItemViewPrefabEntry
            {
                Width = size.x,
                Height = size.y
            });
        }

        /// <summary>
        /// 编辑器删除映射
        /// </summary>
        public void EditorRemoveAt(int index)
        {
            if (index < 0 || index >= entryList.Count) return;
            entryList.RemoveAt(index);
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
