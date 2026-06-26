#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// LootSimConfigSo 编辑器工具
    /// </summary>
    public static class LootSimConfigSoEditorUtil
    {
        /// <summary>
        /// 确保投放模拟配置资产存在
        /// </summary>
        public static LootSimConfigSo EnsureConfigAsset()
        {
            var configSo = AssetDatabase.LoadAssetAtPath<LootSimConfigSo>(
                LootSimConfigSo.DefaultAssetPath);
            if (configSo != null)
                return configSo;

            string folder = Path.GetDirectoryName(LootSimConfigSo.DefaultAssetPath);
            if (!string.IsNullOrEmpty(folder) && !AssetDatabase.IsValidFolder(folder))
                ItemBaseDataListSoEditorUtil.CreateFolderRecursive(folder);

            configSo = ScriptableObject.CreateInstance<LootSimConfigSo>();
            AssetDatabase.CreateAsset(configSo, LootSimConfigSo.DefaultAssetPath);
            AssetDatabase.SaveAssets();
            return configSo;
        }
    }
}
#endif
