#if UNITY_EDITOR
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
        private const string ListSoGuidKey = "MmInventory.ItemDataEditor.ListSoGuid";
        private const string LegacyRegistrySoGuidKey = "MmInventory.ItemDataEditor.RegistrySoGuid";
        private const string ViewPrefabListSoGuidKey = "MmInventory.ItemDataEditor.ViewPrefabListSoGuid";
        private const string LegacyViewPrefabRegistrySoGuidKey = "MmInventory.ItemDataEditor.ViewPrefabRegistrySoGuid";
        private const string LootSimConfigSoGuidKey = "MmInventory.ItemDataEditor.LootSimConfigSoGuid";

        public const string DefaultEnumFilePath = "Assets/GridInventory/Scripts/Data/TableData/EItemType.cs";
        public const string DefaultCreateSoFolder = "Assets/GridInventory/SoDatas";

        /// <summary>
        /// 总库 SO 的 GUID
        /// </summary>
        public static string ListSoGuid
        {
            get => EditorPrefs.GetString(ListSoGuidKey, string.Empty);
            set => EditorPrefs.SetString(ListSoGuidKey, value);
        }

        /// <summary>
        /// 加载总库 SO
        /// </summary>
        public static ItemTableDataListSo LoadListSo()
        {
            string guid = ListSoGuid;
            if (string.IsNullOrEmpty(guid))
                guid = EditorPrefs.GetString(LegacyRegistrySoGuidKey, string.Empty);

            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ItemTableDataListSo>(path);
                if (so != null)
                {
                    SaveListSo(so);
                    return so;
                }
            }

            return ItemBaseDataListSoEditorUtil.EnsureListAsset();
        }

        /// <summary>
        /// 保存总库 SO 引用
        /// </summary>
        public static void SaveListSo(ItemTableDataListSo listSo)
        {
            if (listSo == null)
            {
                ListSoGuid = string.Empty;
                return;
            }

            string path = AssetDatabase.GetAssetPath(listSo);
            ListSoGuid = AssetDatabase.AssetPathToGUID(path);
        }

        /// <summary>
        /// 视图预制体列表 SO 的 GUID
        /// </summary>
        public static string ViewPrefabListSoGuid
        {
            get => EditorPrefs.GetString(ViewPrefabListSoGuidKey, string.Empty);
            set => EditorPrefs.SetString(ViewPrefabListSoGuidKey, value);
        }

        /// <summary>
        /// 加载视图预制体列表 SO
        /// </summary>
        public static ItemViewPrefabListSo LoadViewPrefabListSo()
        {
            string guid = ViewPrefabListSoGuid;
            if (string.IsNullOrEmpty(guid))
                guid = EditorPrefs.GetString(LegacyViewPrefabRegistrySoGuidKey, string.Empty);

            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<ItemViewPrefabListSo>(path);
                if (so != null)
                {
                    SaveViewPrefabListSo(so);
                    return so;
                }
            }

            return ItemViewPrefabListSoEditorUtil.EnsureListAsset();
        }

        /// <summary>
        /// 保存视图预制体列表 SO 引用
        /// </summary>
        public static void SaveViewPrefabListSo(ItemViewPrefabListSo listSo)
        {
            if (listSo == null)
            {
                ViewPrefabListSoGuid = string.Empty;
                return;
            }

            string path = AssetDatabase.GetAssetPath(listSo);
            ViewPrefabListSoGuid = AssetDatabase.AssetPathToGUID(path);
        }

        /// <summary>
        /// 投放模拟配置 SO 的 GUID
        /// </summary>
        public static string LootSimConfigSoGuid
        {
            get => EditorPrefs.GetString(LootSimConfigSoGuidKey, string.Empty);
            set => EditorPrefs.SetString(LootSimConfigSoGuidKey, value);
        }

        /// <summary>
        /// 加载投放模拟配置 SO
        /// </summary>
        public static LootSimConfigSo LoadLootSimConfigSo()
        {
            string guid = LootSimConfigSoGuid;
            if (!string.IsNullOrEmpty(guid))
            {
                string path = AssetDatabase.GUIDToAssetPath(guid);
                var so = AssetDatabase.LoadAssetAtPath<LootSimConfigSo>(path);
                if (so != null)
                {
                    SaveLootSimConfigSo(so);
                    return so;
                }
            }

            return LootSimConfigSoEditorUtil.EnsureConfigAsset();
        }

        /// <summary>
        /// 保存投放模拟配置 SO 引用
        /// </summary>
        public static void SaveLootSimConfigSo(LootSimConfigSo configSo)
        {
            if (configSo == null)
            {
                LootSimConfigSoGuid = string.Empty;
                return;
            }

            string path = AssetDatabase.GetAssetPath(configSo);
            LootSimConfigSoGuid = AssetDatabase.AssetPathToGUID(path);
        }

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
    }
}
#endif
