using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// 枚举脚本代码生成
    /// </summary>
    public static class EItemTypeCodeGenerator
    {
        public const string DefaultFilePath = "Assets/GridInventory/Scripts/Data/EItemType.cs";

        private static readonly Regex EnumTypeRegex = new(@"enum\s+([A-Za-z_][A-Za-z0-9_]*)");
        private static readonly Regex EnumMemberRegex = new(@"^\s*([A-Za-z_][A-Za-z0-9_]*)\s*,?\s*$");

        /// <summary>
        /// 读取当前枚举名
        /// </summary>
        public static List<string> ReadEnumNames(string filePath = null)
        {
            string path = ResolveFilePath(filePath);
            if (!File.Exists(path))
                return new List<string>();

            var nameList = new List<string>();
            var lines = File.ReadAllLines(path);

            bool inEnum = false;
            string enumTypeName = ResolveEnumTypeName(path);
            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i];
                if (line.Contains($"enum {enumTypeName}"))
                {
                    inEnum = true;
                    continue;
                }

                if (!inEnum) continue;
                if (line.Contains("}")) break;

                var match = EnumMemberRegex.Match(line);
                if (match.Success)
                    nameList.Add(match.Groups[1].Value);
            }

            return nameList;
        }

        /// <summary>
        /// 覆盖写入枚举文件
        /// </summary>
        public static void WriteEnum(IReadOnlyList<string> rawNameList, string filePath = null)
        {
            if (rawNameList == null || rawNameList.Count == 0)
            {
                Debug.LogError("枚举生成失败 名称列表为空");
                return;
            }

            string path = ResolveFilePath(filePath);
            string enumTypeName = ResolveEnumTypeName(path);

            var nameList = new List<string>();
            var usedHashList = new HashSet<string>(StringComparer.Ordinal);
            for (int i = 0; i < rawNameList.Count; i++)
            {
                string safeName = SanitizeEnumName(rawNameList[i]);
                if (usedHashList.Add(safeName))
                    nameList.Add(safeName);
            }

            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine();
            sb.AppendLine("namespace MmInventory");
            sb.AppendLine("{");
            sb.AppendLine("    /// <summary>");
            sb.AppendLine("    /// 物品分类枚举");
            sb.AppendLine("    /// </summary>");
            sb.AppendLine("    [Serializable]");
            sb.AppendLine($"    public enum {enumTypeName} : byte");
            sb.AppendLine("    {");

            for (int i = 0; i < nameList.Count; i++)
            {
                string suffix = i < nameList.Count - 1 ? "," : string.Empty;
                sb.AppendLine($"        {nameList[i]}{suffix}");
            }

            sb.AppendLine("    }");
            sb.AppendLine("}");

            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            UnityEditor.AssetDatabase.Refresh();
        }

        /// <summary>
        /// 解析枚举类型名
        /// </summary>
        public static string ResolveEnumTypeName(string filePath)
        {
            string path = ResolveFilePath(filePath);
            if (File.Exists(path))
            {
                var lines = File.ReadAllLines(path);
                for (int i = 0; i < lines.Length; i++)
                {
                    var match = EnumTypeRegex.Match(lines[i]);
                    if (match.Success)
                        return match.Groups[1].Value;
                }
            }

            string fileName = Path.GetFileNameWithoutExtension(path);
            return string.IsNullOrEmpty(fileName) ? "EItemType" : fileName;
        }

        /// <summary>
        /// 清洗枚举名
        /// </summary>
        public static string SanitizeEnumName(string rawName)
        {
            if (string.IsNullOrWhiteSpace(rawName))
                return "Unknown";

            var sb = new StringBuilder();
            string trimName = rawName.Trim();
            for (int i = 0; i < trimName.Length; i++)
            {
                char c = trimName[i];
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
            }

            if (sb.Length == 0)
                return "Unknown";

            if (char.IsDigit(sb[0]))
                sb.Insert(0, '_');

            return sb.ToString();
        }

        private static string ResolveFilePath(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return DefaultFilePath;

            return filePath.Replace('\\', '/').Trim();
        }
    }
}
