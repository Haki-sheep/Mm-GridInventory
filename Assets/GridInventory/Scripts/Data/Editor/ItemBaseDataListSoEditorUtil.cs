#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// ItemBaseDataListSo 编辑器工具
    /// </summary>
    public static class ItemBaseDataListSoEditorUtil
    {
        /// <summary>
        /// 在指定目录创建 SO
        /// </summary>
        public static ItemBaseDataListSo CreateAsset(string folderPath, string assetName)
        {
            string safeFolder = NormalizeAssetFolder(folderPath);
            string safeName = SanitizeAssetName(assetName);
            if (string.IsNullOrEmpty(safeName))
                safeName = "ItemBaseDataList";

            if (!AssetDatabase.IsValidFolder(safeFolder))
            {
                CreateFolderRecursive(safeFolder);
            }

            string assetPath = $"{safeFolder}/{safeName}.asset";
            assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

            var so = ScriptableObject.CreateInstance<ItemBaseDataListSo>();
            AssetDatabase.CreateAsset(so, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return so;
        }

        /// <summary>
        /// 绝对路径转 Assets 相对路径
        /// </summary>
        public static string ToAssetPath(string absolutePath)
        {
            if (string.IsNullOrEmpty(absolutePath)) return string.Empty;

            string dataPath = Application.dataPath.Replace('\\', '/');
            string normalized = absolutePath.Replace('\\', '/');
            if (!normalized.StartsWith(dataPath)) return string.Empty;

            return "Assets" + normalized.Substring(dataPath.Length);
        }

        /// <summary>
        /// 规范化资源目录
        /// </summary>
        public static string NormalizeAssetFolder(string folderPath)
        {
            if (string.IsNullOrWhiteSpace(folderPath))
                return ItemDataEditorSession.DefaultCreateSoFolder;

            string path = folderPath.Replace('\\', '/').Trim();
            if (!path.StartsWith("Assets/"))
                path = "Assets/" + path.TrimStart('/');

            return path.TrimEnd('/');
        }

        private static string SanitizeAssetName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName)) return string.Empty;

            var invalidList = Path.GetInvalidFileNameChars();
            var chars = rawName.Trim().ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < invalidList.Length; j++)
                {
                    if (chars[i] == invalidList[j])
                        chars[i] = '_';
                }
            }

            return new string(chars);
        }

        private static void CreateFolderRecursive(string assetFolder)
        {
            string[] partList = assetFolder.Split('/');
            if (partList.Length < 2 || partList[0] != "Assets") return;

            string current = partList[0];
            for (int i = 1; i < partList.Length; i++)
            {
                string next = current + "/" + partList[i];
                if (!AssetDatabase.IsValidFolder(next))
                    AssetDatabase.CreateFolder(current, partList[i]);
                current = next;
            }
        }
    }
}
#endif
