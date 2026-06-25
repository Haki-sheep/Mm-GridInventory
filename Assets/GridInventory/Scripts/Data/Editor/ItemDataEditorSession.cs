#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// 物品编辑器会话数据
    /// </summary>
    public static class ItemDataEditorSession
    {
        private const string EnumFilePathKey = "MmInventory.ItemDataEditor.EnumFilePath";
        private const string CreateSoFolderKey = "MmInventory.ItemDataEditor.CreateSoFolder";
        private const string ManagedSoGuidsKey = "MmInventory.ItemDataEditor.ManagedSoGuids";
        private const string ActiveSoGuidKey = "MmInventory.ItemDataEditor.ActiveSoGuid";

        public const string DefaultEnumFilePath = "Assets/GridInventory/Scripts/Data/EItemType.cs";
        public const string DefaultCreateSoFolder = "Assets/GridInventory/SoDatas";

        /// <summary>
        /// 枚举脚本路径
        /// </summary>
        public static string EnumFilePath
        {
            get => EditorPrefs.GetString(EnumFilePathKey, DefaultEnumFilePath);
            set => EditorPrefs.SetString(EnumFilePathKey, value);
        }

        /// <summary>
        /// 新 SO 默认创建目录
        /// </summary>
        public static string CreateSoFolder
        {
            get => EditorPrefs.GetString(CreateSoFolderKey, DefaultCreateSoFolder);
            set => EditorPrefs.SetString(CreateSoFolderKey, value);
        }

        /// <summary>
        /// 当前选中 SO 的 GUID
        /// </summary>
        public static string ActiveSoGuid
        {
            get => EditorPrefs.GetString(ActiveSoGuidKey, string.Empty);
            set => EditorPrefs.SetString(ActiveSoGuidKey, value);
        }

        /// <summary>
        /// 读取已管理的 SO 列表
        /// </summary>
        public static List<ItemBaseDataListSo> LoadManagedSoList()
        {
            var soList = new List<ItemBaseDataListSo>();
            string raw = EditorPrefs.GetString(ManagedSoGuidsKey, string.Empty);
            if (string.IsNullOrEmpty(raw)) return soList;

            string[] guidList = raw.Split('|');
            for (int i = 0; i < guidList.Length; i++)
            {
                string guid = guidList[i];
                if (string.IsNullOrEmpty(guid)) continue;

                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                if (string.IsNullOrEmpty(assetPath)) continue;

                var so = AssetDatabase.LoadAssetAtPath<ItemBaseDataListSo>(assetPath);
                if (so != null)
                    soList.Add(so);
            }

            return soList;
        }

        /// <summary>
        /// 保存已管理的 SO 列表
        /// </summary>
        public static void SaveManagedSoList(IReadOnlyList<ItemBaseDataListSo> soList)
        {
            if (soList == null || soList.Count == 0)
            {
                EditorPrefs.SetString(ManagedSoGuidsKey, string.Empty);
                return;
            }

            var guidList = new List<string>();
            for (int i = 0; i < soList.Count; i++)
            {
                var so = soList[i];
                if (so == null) continue;

                string path = AssetDatabase.GetAssetPath(so);
                if (string.IsNullOrEmpty(path)) continue;

                string guid = AssetDatabase.AssetPathToGUID(path);
                if (!string.IsNullOrEmpty(guid))
                    guidList.Add(guid);
            }

            EditorPrefs.SetString(ManagedSoGuidsKey, string.Join("|", guidList));
        }

        /// <summary>
        /// 根据 GUID 加载 SO
        /// </summary>
        public static ItemBaseDataListSo LoadActiveSo()
        {
            string guid = ActiveSoGuid;
            if (string.IsNullOrEmpty(guid)) return null;

            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (string.IsNullOrEmpty(path)) return null;

            return AssetDatabase.LoadAssetAtPath<ItemBaseDataListSo>(path);
        }
    }
}
#endif
