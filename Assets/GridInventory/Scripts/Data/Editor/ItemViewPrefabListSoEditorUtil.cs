#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// ItemViewPrefabListSo 编辑器工具
    /// </summary>
    public static class ItemViewPrefabListSoEditorUtil
    {
        /// <summary>
        /// 确保视图预制体列表资产存在
        /// </summary>
        public static ItemViewPrefabListSo EnsureListAsset()
        {
            var listSo = AssetDatabase.LoadAssetAtPath<ItemViewPrefabListSo>(
                ItemViewPrefabListSo.DefaultAssetPath);
            if (listSo != null)
                return listSo;

            string folder = Path.GetDirectoryName(ItemViewPrefabListSo.DefaultAssetPath);
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                ItemBaseDataListSoEditorUtil.CreateFolderRecursive(folder);

            listSo = ScriptableObject.CreateInstance<ItemViewPrefabListSo>();
            AssetDatabase.CreateAsset(listSo, ItemViewPrefabListSo.DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return listSo;
        }
    }
}
#endif
