#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// 物品配置 Excel 导入导出
    /// 每个分类一张 Sheet ID 千段 表内稀有度升序
    /// </summary>
    public static class ItemDataExcelIO
    {
        public const string DefaultAssetRelativePath = "Assets/GridInventory/Table/物品配置总表.xlsx";

        private const string ItemTypesSheetName = "ItemTypes";

        private static readonly XNamespace MainNs = "http://schemas.openxmlformats.org/spreadsheetml/2006/main";
        private static readonly XNamespace RelNs = "http://schemas.openxmlformats.org/package/2006/relationships";
        private static readonly XNamespace OfficeRelNs = "http://schemas.openxmlformats.org/officeDocument/2006/relationships";

        /// <summary> 字段名 Luban ##var </summary>
        private static readonly string[] FieldHeaderList =
        {
            "物品ID",
            "名称",
            "图标路径",
            "宽",
            "高",
            "稀有度",
            "是否可堆叠",
            "最大堆叠数"
        };

        /// <summary> 字段类型 Luban ##type </summary>
        private static readonly string[] FieldTypeList =
        {
            "int",
            "string",
            "string",
            "int",
            "int",
            "string",
            "bool",
            "int"
        };

        /// <summary>
        /// 默认表绝对路径
        /// </summary>
        public static string GetDefaultAbsolutePath()
        {
            return Path.GetFullPath(Path.Combine(Application.dataPath, "..", DefaultAssetRelativePath));
        }

        /// <summary>
        /// 导出到 xlsx
        /// </summary>
        public static void ExportToFile(ItemTableDataListSo listSo, IReadOnlyList<string> itemTypeNameList, string filePath)
        {
            var exportFile = ItemDataJsonIO.BuildExportFile(listSo, itemTypeNameList);
            WriteXlsx(exportFile, filePath);
        }

        /// <summary>
        /// 从 xlsx 导入
        /// </summary>
        public static ItemDataExportFile ImportFromFile(string filePath)
        {
            return ReadXlsx(filePath);
        }

        #region Write

        /// <summary>
        /// 写入 xlsx 每分类一表
        /// </summary>
        private static void WriteXlsx(ItemDataExportFile exportFile, string filePath)
        {
            string directory = Path.GetDirectoryName(filePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            if (File.Exists(filePath))
                File.Delete(filePath);

            var sharedStringList = new List<string>();
            var sharedIndexDict = new Dictionary<string, int>();
            int GetSharedIndex(string text)
            {
                string value = text ?? string.Empty;
                if (sharedIndexDict.TryGetValue(value, out int index))
                    return index;

                index = sharedStringList.Count;
                sharedStringList.Add(value);
                sharedIndexDict[value] = index;
                return index;
            }

            var typeNameList = ResolveTypeNameList(exportFile);
            var sheetItemList = BuildSortedSheetItems(exportFile, typeNameList);

            // sheet0 = ItemTypes 其后每个分类一张
            var sheetNameList = new List<string> { ItemTypesSheetName };
            var sheetRowList = new List<List<List<CellValue>>>
            {
                BuildItemTypeRows(typeNameList, GetSharedIndex)
            };

            for (int i = 0; i < typeNameList.Count; i++)
            {
                string sheetName = SanitizeSheetName(typeNameList[i], i);
                sheetNameList.Add(sheetName);
                sheetRowList.Add(BuildTypeItemSheetRows(typeNameList[i], i, sheetItemList[i], GetSharedIndex));
            }

            using var zip = ZipFile.Open(filePath, ZipArchiveMode.Create);
            WriteEntry(zip, "[Content_Types].xml", BuildContentTypesXml(sheetNameList.Count));
            WriteEntry(zip, "_rels/.rels", BuildPackageRelsXml());
            WriteEntry(zip, "xl/workbook.xml", BuildWorkbookXml(sheetNameList));
            WriteEntry(zip, "xl/_rels/workbook.xml.rels", BuildWorkbookRelsXml(sheetNameList.Count));
            WriteEntry(zip, "xl/styles.xml", BuildStylesXml());
            WriteEntry(zip, "xl/sharedStrings.xml", BuildSharedStringsXml(sharedStringList));

            for (int i = 0; i < sheetRowList.Count; i++)
                WriteEntry(zip, $"xl/worksheets/sheet{i + 1}.xml", BuildSheetXml(sheetRowList[i]));
        }

        /// <summary>
        /// 构建单个分类表
        /// </summary>
        private static List<List<CellValue>> BuildTypeItemSheetRows(
            string typeName,
            int typeIndex,
            IReadOnlyList<ItemDataJsonEntry> itemList,
            Func<string, int> getSharedIndex)
        {
            ItemTableIdRange.GetRangeByTypeIndex(typeIndex, out int minId, out int maxId);

            var rowList = new List<List<CellValue>>
            {
                // 说明行
                new List<CellValue>
                {
                    CellValue.Shared(getSharedIndex($"{typeName} {minId}-{maxId}"))
                },
                // ##var
                BuildSharedRow(FieldHeaderList, getSharedIndex),
                // ##type
                BuildSharedRow(FieldTypeList, getSharedIndex)
            };

            if (itemList == null)
                return rowList;

            for (int i = 0; i < itemList.Count; i++)
                rowList.Add(BuildItemDataRow(itemList[i], getSharedIndex));

            return rowList;
        }

        private static List<CellValue> BuildItemDataRow(ItemDataJsonEntry entry, Func<string, int> getSharedIndex)
        {
            return new List<CellValue>
            {
                CellValue.Number(entry.excelItemId),
                CellValue.Shared(getSharedIndex(entry.name ?? string.Empty)),
                CellValue.Shared(getSharedIndex(entry.iconPath ?? string.Empty)),
                CellValue.Number(entry.dataSizeX),
                CellValue.Number(entry.dataSizeY),
                CellValue.Shared(getSharedIndex(entry.itemRarity ?? string.Empty)),
                CellValue.Shared(getSharedIndex(IsStackableValue(entry.itemStackType) ? "true" : "false")),
                CellValue.Number(entry.maxStackCount)
            };
        }

        private static List<CellValue> BuildSharedRow(IReadOnlyList<string> textList, Func<string, int> getSharedIndex)
        {
            var cellList = new List<CellValue>(textList.Count);
            for (int i = 0; i < textList.Count; i++)
                cellList.Add(CellValue.Shared(getSharedIndex(textList[i])));
            return cellList;
        }

        /// <summary>
        /// 解析导出用类型顺序
        /// </summary>
        private static List<string> ResolveTypeNameList(ItemDataExportFile exportFile)
        {
            var typeNameList = new List<string>();
            if (exportFile?.itemTypes != null)
            {
                for (int i = 0; i < exportFile.itemTypes.Count; i++)
                {
                    string typeName = exportFile.itemTypes[i];
                    if (!string.IsNullOrWhiteSpace(typeName))
                        typeNameList.Add(typeName.Trim());
                }
            }

            if (typeNameList.Count > 0)
                return typeNameList;

            foreach (EItemType eItemType in Enum.GetValues(typeof(EItemType)))
                typeNameList.Add(eItemType.ToString());
            return typeNameList;
        }

        /// <summary>
        /// 按类型分表并稀有度升序
        /// </summary>
        private static List<List<ItemDataJsonEntry>> BuildSortedSheetItems(ItemDataExportFile exportFile, IReadOnlyList<string> typeNameList)
        {
            var sheetItemList = new List<List<ItemDataJsonEntry>>(typeNameList.Count);
            for (int i = 0; i < typeNameList.Count; i++)
                sheetItemList.Add(new List<ItemDataJsonEntry>());

            var typeIndexDict = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i < typeNameList.Count; i++)
                typeIndexDict[typeNameList[i]] = i;

            if (exportFile?.items != null)
            {
                for (int i = 0; i < exportFile.items.Count; i++)
                {
                    var entry = exportFile.items[i];
                    int typeIndex = ResolveExportTypeIndex(entry, typeIndexDict, typeNameList.Count);
                    sheetItemList[typeIndex].Add(entry);

                    ItemTableIdRange.GetRangeByTypeIndex(typeIndex, out int minId, out int maxId);
                    if (entry.excelItemId < minId || entry.excelItemId > maxId)
                    {
                        Debug.LogWarning(
                            $"物品 {entry.excelItemId} {entry.name} 类型 {typeNameList[typeIndex]} 建议落在号段 {minId}-{maxId}");
                    }
                }
            }

            for (int i = 0; i < sheetItemList.Count; i++)
            {
                sheetItemList[i].Sort((left, right) =>
                {
                    int rarityCompare = GetRaritySortValue(left.itemRarity).CompareTo(GetRaritySortValue(right.itemRarity));
                    if (rarityCompare != 0)
                        return rarityCompare;
                    return left.excelItemId.CompareTo(right.excelItemId);
                });
            }

            return sheetItemList;
        }

        private static int ResolveExportTypeIndex(ItemDataJsonEntry entry, Dictionary<string, int> typeIndexDict, int typeCount)
        {
            if (!string.IsNullOrWhiteSpace(entry.itemType)
                && typeIndexDict.TryGetValue(entry.itemType.Trim(), out int namedIndex))
                return namedIndex;

            if (ItemTableIdRange.TryGetTypeIndex(entry.excelItemId, out int rangeIndex)
                && rangeIndex >= 0
                && rangeIndex < typeCount)
                return rangeIndex;

            return 0;
        }

        /// <summary>
        /// 构建类型总览表
        /// </summary>
        private static List<List<CellValue>> BuildItemTypeRows(IReadOnlyList<string> typeNameList, Func<string, int> getSharedIndex)
        {
            var rowList = new List<List<CellValue>>
            {
                new List<CellValue>
                {
                    CellValue.Shared(getSharedIndex("物品类型")),
                    CellValue.Shared(getSharedIndex("ID起始")),
                    CellValue.Shared(getSharedIndex("ID结束")),
                    CellValue.Shared(getSharedIndex("Sheet名"))
                },
                new List<CellValue>
                {
                    CellValue.Shared(getSharedIndex("string")),
                    CellValue.Shared(getSharedIndex("int")),
                    CellValue.Shared(getSharedIndex("int")),
                    CellValue.Shared(getSharedIndex("string"))
                }
            };

            for (int i = 0; i < typeNameList.Count; i++)
            {
                ItemTableIdRange.GetRangeByTypeIndex(i, out int minId, out int maxId);
                string sheetName = SanitizeSheetName(typeNameList[i], i);
                rowList.Add(new List<CellValue>
                {
                    CellValue.Shared(getSharedIndex(typeNameList[i])),
                    CellValue.Number(minId),
                    CellValue.Number(maxId),
                    CellValue.Shared(getSharedIndex(sheetName))
                });
            }

            return rowList;
        }

        private static string SanitizeSheetName(string rawName, int typeIndex)
        {
            string name = string.IsNullOrWhiteSpace(rawName) ? $"Type{typeIndex}" : rawName.Trim();
            char[] invalidList = { '\\', '/', '?', '*', '[', ']', ':' };
            var chars = name.ToCharArray();
            for (int i = 0; i < chars.Length; i++)
            {
                for (int j = 0; j < invalidList.Length; j++)
                {
                    if (chars[i] == invalidList[j])
                        chars[i] = '_';
                }
            }

            name = new string(chars);
            if (name.Length > 31)
                name = name.Substring(0, 31);
            if (name.Equals(ItemTypesSheetName, StringComparison.OrdinalIgnoreCase))
                name = $"{name}_{typeIndex}";
            return name;
        }

        private static void WriteEntry(ZipArchive zip, string entryName, string content)
        {
            var entry = zip.CreateEntry(entryName, System.IO.Compression.CompressionLevel.Optimal);
            using var stream = entry.Open();
            using var writer = new StreamWriter(stream, new UTF8Encoding(false));
            writer.Write(content);
        }

        private static string BuildContentTypesXml(int sheetCount)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\">");
            sb.Append("<Default Extension=\"rels\" ContentType=\"application/vnd.openxmlformats-package.relationships+xml\"/>");
            sb.Append("<Default Extension=\"xml\" ContentType=\"application/xml\"/>");
            sb.Append("<Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/>");
            for (int i = 1; i <= sheetCount; i++)
                sb.Append($"<Override PartName=\"/xl/worksheets/sheet{i}.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>");
            sb.Append("<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>");
            sb.Append("<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>");
            sb.Append("</Types>");
            return sb.ToString();
        }

        private static string BuildPackageRelsXml()
        {
            return
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">" +
                "<Relationship Id=\"rId1\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/officeDocument\" Target=\"xl/workbook.xml\"/>" +
                "</Relationships>";
        }

        private static string BuildWorkbookXml(IReadOnlyList<string> sheetNameList)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" ");
            sb.Append("xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\">");
            sb.Append("<sheets>");
            for (int i = 0; i < sheetNameList.Count; i++)
            {
                string safeName = EscapeXml(sheetNameList[i]);
                sb.Append($"<sheet name=\"{safeName}\" sheetId=\"{i + 1}\" r:id=\"rId{i + 1}\"/>");
            }

            sb.Append("</sheets></workbook>");
            return sb.ToString();
        }

        private static string BuildWorkbookRelsXml(int sheetCount)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append("<Relationships xmlns=\"http://schemas.openxmlformats.org/package/2006/relationships\">");
            for (int i = 1; i <= sheetCount; i++)
            {
                sb.Append(
                    $"<Relationship Id=\"rId{i}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/worksheet\" Target=\"worksheets/sheet{i}.xml\"/>");
            }

            int stylesId = sheetCount + 1;
            int sharedId = sheetCount + 2;
            sb.Append(
                $"<Relationship Id=\"rId{stylesId}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/styles\" Target=\"styles.xml\"/>");
            sb.Append(
                $"<Relationship Id=\"rId{sharedId}\" Type=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships/sharedStrings\" Target=\"sharedStrings.xml\"/>");
            sb.Append("</Relationships>");
            return sb.ToString();
        }

        private static string BuildStylesXml()
        {
            return
                "<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>" +
                "<styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\">" +
                "<fonts count=\"1\"><font><sz val=\"11\"/><name val=\"Calibri\"/></font></fonts>" +
                "<fills count=\"1\"><fill><patternFill patternType=\"none\"/></fill></fills>" +
                "<borders count=\"1\"><border/></borders>" +
                "<cellStyleXfs count=\"1\"><xf/></cellStyleXfs>" +
                "<cellXfs count=\"1\"><xf xfId=\"0\"/></cellXfs>" +
                "</styleSheet>";
        }

        private static string BuildSharedStringsXml(IReadOnlyList<string> sharedStringList)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append($"<sst xmlns=\"{MainNs}\" count=\"{sharedStringList.Count}\" uniqueCount=\"{sharedStringList.Count}\">");
            for (int i = 0; i < sharedStringList.Count; i++)
            {
                sb.Append("<si><t xml:space=\"preserve\">");
                sb.Append(EscapeXml(sharedStringList[i]));
                sb.Append("</t></si>");
            }

            sb.Append("</sst>");
            return sb.ToString();
        }

        private static string BuildSheetXml(IReadOnlyList<List<CellValue>> rowList)
        {
            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\" standalone=\"yes\"?>");
            sb.Append($"<worksheet xmlns=\"{MainNs}\"><sheetData>");

            for (int rowIndex = 0; rowIndex < rowList.Count; rowIndex++)
            {
                int excelRow = rowIndex + 1;
                var cellList = rowList[rowIndex];
                sb.Append($"<row r=\"{excelRow}\">");
                for (int colIndex = 0; colIndex < cellList.Count; colIndex++)
                {
                    var cell = cellList[colIndex];
                    if (cell.IsEmpty)
                        continue;

                    string cellRef = ToCellRef(colIndex, excelRow);
                    if (cell.IsShared)
                        sb.Append($"<c r=\"{cellRef}\" t=\"s\"><v>{cell.SharedIndex}</v></c>");
                    else
                        sb.Append($"<c r=\"{cellRef}\"><v>{cell.NumberText}</v></c>");
                }

                sb.Append("</row>");
            }

            sb.Append("</sheetData></worksheet>");
            return sb.ToString();
        }

        #endregion

        #region Read

        /// <summary>
        /// 读取 xlsx 按分类 Sheet 合并
        /// </summary>
        private static ItemDataExportFile ReadXlsx(string filePath)
        {
            if (!File.Exists(filePath))
                throw new FileNotFoundException("Excel 文件不存在", filePath);

            using var zip = ZipFile.OpenRead(filePath);
            var sharedStringList = ReadSharedStrings(zip);
            var sheetPathDict = ReadSheetPathDict(zip);

            var exportFile = new ItemDataExportFile();
            if (sheetPathDict.TryGetValue(ItemTypesSheetName, out string typesPath))
                exportFile.itemTypes = ParseItemTypesSheet(ReadSheetRows(zip, typesPath, sharedStringList));

            var itemList = new List<ItemDataJsonEntry>();
            var typeNameList = exportFile.itemTypes;
            if (typeNameList == null || typeNameList.Count == 0)
                typeNameList = CollectTypeSheetNames(sheetPathDict);

            for (int i = 0; i < typeNameList.Count; i++)
            {
                string typeName = typeNameList[i];
                string sheetName = SanitizeSheetName(typeName, i);
                if (!sheetPathDict.TryGetValue(sheetName, out string sheetPath)
                    && !sheetPathDict.TryGetValue(typeName, out sheetPath))
                    continue;

                var sheetItems = ParseTypeItemSheet(
                    ReadSheetRows(zip, sheetPath, sharedStringList),
                    typeName);
                itemList.AddRange(sheetItems);
            }

            // 兼容旧总表 Items
            if (itemList.Count == 0 && sheetPathDict.TryGetValue("Items", out string legacyPath))
            {
                itemList = ParseTypeItemSheet(
                    ReadSheetRows(zip, legacyPath, sharedStringList),
                    typeNameList.Count > 0 ? typeNameList[0] : EItemType.Equipment.ToString());
            }

            exportFile.items = itemList;
            if (exportFile.itemTypes == null || exportFile.itemTypes.Count == 0)
                exportFile.itemTypes = typeNameList.ToList();

            return exportFile;
        }

        private static List<string> CollectTypeSheetNames(Dictionary<string, string> sheetPathDict)
        {
            var typeNameList = new List<string>();
            foreach (var pair in sheetPathDict)
            {
                if (pair.Key.Equals(ItemTypesSheetName, StringComparison.OrdinalIgnoreCase))
                    continue;
                if (pair.Key.Equals("Items", StringComparison.OrdinalIgnoreCase))
                    continue;
                typeNameList.Add(pair.Key);
            }

            return typeNameList;
        }

        private static Dictionary<string, string> ReadSheetPathDict(ZipArchive zip)
        {
            var workbookEntry = zip.GetEntry("xl/workbook.xml");
            if (workbookEntry == null)
                throw new InvalidDataException("xlsx 缺少 workbook.xml");

            XDocument workbookDoc;
            using (var stream = workbookEntry.Open())
                workbookDoc = XDocument.Load(stream);

            var relEntry = zip.GetEntry("xl/_rels/workbook.xml.rels");
            if (relEntry == null)
                throw new InvalidDataException("xlsx 缺少 workbook.xml.rels");

            XDocument relDoc;
            using (var stream = relEntry.Open())
                relDoc = XDocument.Load(stream);

            var relTargetDict = new Dictionary<string, string>(StringComparer.Ordinal);
            foreach (var rel in relDoc.Root.Elements(RelNs + "Relationship"))
            {
                string id = (string)rel.Attribute("Id");
                string target = (string)rel.Attribute("Target");
                if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(target))
                    continue;

                string normalized = target.Replace('\\', '/');
                if (!normalized.StartsWith("xl/", StringComparison.OrdinalIgnoreCase)
                    && !normalized.StartsWith("/xl/", StringComparison.OrdinalIgnoreCase))
                    normalized = "xl/" + normalized.TrimStart('/');
                else
                    normalized = normalized.TrimStart('/');

                relTargetDict[id] = normalized;
            }

            var sheetPathDict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var sheet in workbookDoc.Root.Element(MainNs + "sheets")?.Elements(MainNs + "sheet") ?? Enumerable.Empty<XElement>())
            {
                string name = (string)sheet.Attribute("name");
                string relId = (string)sheet.Attribute(OfficeRelNs + "id");
                if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(relId))
                    continue;

                if (relTargetDict.TryGetValue(relId, out string path))
                    sheetPathDict[name] = path;
            }

            return sheetPathDict;
        }

        private static List<string> ReadSharedStrings(ZipArchive zip)
        {
            var resultList = new List<string>();
            var entry = zip.GetEntry("xl/sharedStrings.xml");
            if (entry == null)
                return resultList;

            XDocument doc;
            using (var stream = entry.Open())
                doc = XDocument.Load(stream);

            foreach (var si in doc.Root.Elements(MainNs + "si"))
            {
                var textParts = si.Descendants(MainNs + "t").Select(t => t.Value);
                resultList.Add(string.Concat(textParts));
            }

            return resultList;
        }

        private static List<Dictionary<int, string>> ReadSheetRows(ZipArchive zip, string sheetPath, IReadOnlyList<string> sharedStringList)
        {
            var entry = zip.GetEntry(sheetPath);
            if (entry == null)
                throw new InvalidDataException($"xlsx 缺少工作表 {sheetPath}");

            XDocument doc;
            using (var stream = entry.Open())
                doc = XDocument.Load(stream);

            var rowDict = new SortedDictionary<int, Dictionary<int, string>>();
            foreach (var row in doc.Root.Element(MainNs + "sheetData")?.Elements(MainNs + "row") ?? Enumerable.Empty<XElement>())
            {
                int rowIndex = (int?)row.Attribute("r") ?? 0;
                if (rowIndex <= 0)
                    continue;

                var cellDict = new Dictionary<int, string>();
                foreach (var cell in row.Elements(MainNs + "c"))
                {
                    string cellRef = (string)cell.Attribute("r");
                    if (string.IsNullOrEmpty(cellRef))
                        continue;

                    int colIndex = ParseColumnIndex(cellRef);
                    string cellType = (string)cell.Attribute("t");
                    string rawValue = cell.Element(MainNs + "v")?.Value ?? string.Empty;

                    if (cellType == "s"
                        && int.TryParse(rawValue, NumberStyles.Integer, CultureInfo.InvariantCulture, out int sharedIndex)
                        && sharedIndex >= 0
                        && sharedIndex < sharedStringList.Count)
                    {
                        cellDict[colIndex] = sharedStringList[sharedIndex];
                    }
                    else if (cellType == "inlineStr")
                    {
                        cellDict[colIndex] = string.Concat(cell.Descendants(MainNs + "t").Select(t => t.Value));
                    }
                    else
                    {
                        cellDict[colIndex] = rawValue;
                    }
                }

                rowDict[rowIndex] = cellDict;
            }

            return rowDict.Values.ToList();
        }

        /// <summary>
        /// 解析单个分类 Sheet
        /// </summary>
        private static List<ItemDataJsonEntry> ParseTypeItemSheet(IReadOnlyList<Dictionary<int, string>> rowList, string typeName)
        {
            var itemList = new List<ItemDataJsonEntry>();
            if (rowList == null || rowList.Count == 0)
                return itemList;

            if (!TryFindHeaderRow(rowList, out int headerRowIndex))
                return itemList;

            var headerMap = BuildHeaderMap(rowList[headerRowIndex]);
            int dataStartIndex = GetDataStartRowIndex(rowList, headerRowIndex);

            for (int i = dataStartIndex; i < rowList.Count; i++)
            {
                var cellDict = rowList[i];
                string idText = GetCell(cellDict, headerMap, "物品ID", "excelItemId", "id");
                if (string.IsNullOrWhiteSpace(idText))
                    continue;

                if (!TryParseInt(idText, out int excelItemId))
                {
                    Debug.LogWarning($"Excel 表 {typeName} 第 {i + 1} 行物品ID无效 {idText}");
                    continue;
                }

                string stackRaw = GetCell(
                    cellDict,
                    headerMap,
                    "是否可堆叠",
                    "canStack",
                    "堆叠类型",
                    "itemStackType");

                itemList.Add(new ItemDataJsonEntry
                {
                    excelItemId = excelItemId,
                    name = GetCell(cellDict, headerMap, "名称", "name"),
                    iconPath = GetCell(cellDict, headerMap, "图标路径", "iconPath"),
                    dataSizeX = ParseIntOrDefault(GetCell(cellDict, headerMap, "宽", "dataSizeX", "网格宽"), 1),
                    dataSizeY = ParseIntOrDefault(GetCell(cellDict, headerMap, "高", "dataSizeY", "网格高"), 1),
                    itemType = typeName,
                    itemRarity = GetCell(cellDict, headerMap, "稀有度", "itemRarity"),
                    itemStackType = ToStackTypeName(ParseBool(stackRaw, false)),
                    maxStackCount = ParseIntOrDefault(GetCell(cellDict, headerMap, "最大堆叠数", "maxStackCount"), 1)
                });
            }

            return itemList;
        }

        private static bool TryFindHeaderRow(IReadOnlyList<Dictionary<int, string>> rowList, out int headerRowIndex)
        {
            headerRowIndex = -1;
            for (int i = 0; i < rowList.Count; i++)
            {
                foreach (var pair in rowList[i])
                {
                    string text = pair.Value?.Trim();
                    if (string.IsNullOrEmpty(text))
                        continue;

                    if (text.Equals("物品ID", StringComparison.OrdinalIgnoreCase)
                        || text.Equals("excelItemId", StringComparison.OrdinalIgnoreCase)
                        || text.Equals("id", StringComparison.OrdinalIgnoreCase))
                    {
                        headerRowIndex = i;
                        return true;
                    }
                }
            }

            return false;
        }

        private static int GetDataStartRowIndex(IReadOnlyList<Dictionary<int, string>> rowList, int headerRowIndex)
        {
            int nextIndex = headerRowIndex + 1;
            if (nextIndex < rowList.Count && IsTypeDefineRow(rowList[nextIndex]))
                return nextIndex + 1;
            return nextIndex;
        }

        private static bool IsTypeDefineRow(Dictionary<int, string> row)
        {
            if (row == null || row.Count == 0)
                return false;

            int typeTokenCount = 0;
            int valueCount = 0;
            foreach (var pair in row)
            {
                if (string.IsNullOrWhiteSpace(pair.Value))
                    continue;
                valueCount++;
                if (IsTypeToken(pair.Value))
                    typeTokenCount++;
            }

            return valueCount > 0 && typeTokenCount * 2 >= valueCount;
        }

        private static bool IsTypeToken(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            switch (text.Trim().ToLowerInvariant())
            {
                case "int":
                case "int32":
                case "int64":
                case "long":
                case "float":
                case "double":
                case "bool":
                case "boolean":
                case "string":
                case "str":
                case "text":
                    return true;
                default:
                    return false;
            }
        }

        private static List<string> ParseItemTypesSheet(IReadOnlyList<Dictionary<int, string>> rowList)
        {
            var typeList = new List<string>();
            if (rowList == null || rowList.Count == 0)
                return typeList;

            int startIndex = 0;
            string first = GetFirstCell(rowList[0]);
            if (IsItemTypeHeader(first))
                startIndex = 1;
            if (startIndex < rowList.Count && IsTypeDefineRow(rowList[startIndex]))
                startIndex++;

            for (int i = startIndex; i < rowList.Count; i++)
            {
                string typeName = GetFirstCell(rowList[i]);
                if (string.IsNullOrWhiteSpace(typeName))
                    continue;
                if (IsTypeToken(typeName))
                    continue;
                typeList.Add(typeName.Trim());
            }

            return typeList;
        }

        private static Dictionary<string, int> BuildHeaderMap(Dictionary<int, string> headerRow)
        {
            var headerMap = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            foreach (var pair in headerRow)
            {
                string key = pair.Value?.Trim();
                if (string.IsNullOrEmpty(key))
                    continue;
                headerMap[key] = pair.Key;
            }

            return headerMap;
        }

        private static string GetCell(Dictionary<int, string> cellDict, Dictionary<string, int> headerMap, params string[] aliasList)
        {
            for (int i = 0; i < aliasList.Length; i++)
            {
                if (headerMap.TryGetValue(aliasList[i], out int col)
                    && cellDict.TryGetValue(col, out string value))
                    return value?.Trim() ?? string.Empty;
            }

            return string.Empty;
        }

        private static string GetFirstCell(Dictionary<int, string> cellDict)
        {
            if (cellDict == null || cellDict.Count == 0)
                return string.Empty;

            int minCol = cellDict.Keys.Min();
            return cellDict.TryGetValue(minCol, out string value) ? value?.Trim() ?? string.Empty : string.Empty;
        }

        private static bool IsItemTypeHeader(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            return text.Equals("物品类型", StringComparison.OrdinalIgnoreCase)
                   || text.Equals("itemType", StringComparison.OrdinalIgnoreCase)
                   || text.Equals("itemTypes", StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Helpers

        private static int GetRaritySortValue(string rarityName)
        {
            if (string.IsNullOrWhiteSpace(rarityName))
                return (int)EItemRarity.White;

            if (Enum.TryParse(rarityName, true, out EItemRarity eItemRarity))
                return (int)eItemRarity;

            switch (rarityName.Trim())
            {
                case "白":
                case "Common":
                    return (int)EItemRarity.White;
                case "蓝":
                case "Uncommon":
                    return (int)EItemRarity.Blue;
                case "紫":
                case "Rare":
                    return (int)EItemRarity.Purple;
                case "金":
                case "Epic":
                    return (int)EItemRarity.Gold;
                case "红":
                case "Legendary":
                    return (int)EItemRarity.Red;
                default:
                    return (int)EItemRarity.White;
            }
        }

        private static bool IsStackableValue(string rawValue)
        {
            return ParseBool(rawValue, false);
        }

        private static bool ParseBool(string rawValue, bool defaultValue)
        {
            if (string.IsNullOrWhiteSpace(rawValue))
                return defaultValue;

            string text = rawValue.Trim();
            if (text == "1"
                || text.Equals("true", StringComparison.OrdinalIgnoreCase)
                || text.Equals("yes", StringComparison.OrdinalIgnoreCase)
                || text == "是"
                || text.Equals("y", StringComparison.OrdinalIgnoreCase)
                || text.Equals(nameof(EItemStackType.Stackable), StringComparison.OrdinalIgnoreCase))
                return true;

            if (text == "0"
                || text.Equals("false", StringComparison.OrdinalIgnoreCase)
                || text.Equals("no", StringComparison.OrdinalIgnoreCase)
                || text == "否"
                || text.Equals("n", StringComparison.OrdinalIgnoreCase)
                || text.Equals(nameof(EItemStackType.NoStackable), StringComparison.OrdinalIgnoreCase))
                return false;

            return defaultValue;
        }

        private static string ToStackTypeName(bool canStack)
        {
            return canStack
                ? nameof(EItemStackType.Stackable)
                : nameof(EItemStackType.NoStackable);
        }

        private static bool TryParseInt(string text, out int value)
        {
            if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out double number))
            {
                value = (int)Math.Round(number);
                return true;
            }

            value = 0;
            return false;
        }

        private static int ParseIntOrDefault(string text, int defaultValue)
        {
            return TryParseInt(text, out int value) ? value : defaultValue;
        }

        private static string ToCellRef(int zeroBasedCol, int oneBasedRow)
        {
            return $"{ToColumnName(zeroBasedCol)}{oneBasedRow}";
        }

        private static string ToColumnName(int zeroBasedCol)
        {
            int value = zeroBasedCol;
            var chars = new Stack<char>();
            do
            {
                chars.Push((char)('A' + value % 26));
                value = value / 26 - 1;
            } while (value >= 0);

            return new string(chars.ToArray());
        }

        private static int ParseColumnIndex(string cellRef)
        {
            int col = 0;
            for (int i = 0; i < cellRef.Length; i++)
            {
                char c = cellRef[i];
                if (c < 'A' || c > 'Z')
                    break;
                col = col * 26 + (c - 'A' + 1);
            }

            return col - 1;
        }

        private static string EscapeXml(string text)
        {
            if (string.IsNullOrEmpty(text))
                return string.Empty;

            return text
                .Replace("&", "&amp;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;")
                .Replace("\"", "&quot;")
                .Replace("'", "&apos;");
        }

        private readonly struct CellValue
        {
            public bool IsEmpty { get; }
            public bool IsShared { get; }
            public int SharedIndex { get; }
            public string NumberText { get; }

            private CellValue(bool isEmpty, bool isShared, int sharedIndex, string numberText)
            {
                IsEmpty = isEmpty;
                IsShared = isShared;
                SharedIndex = sharedIndex;
                NumberText = numberText;
            }

            public static CellValue Empty => new(true, false, 0, null);

            public static CellValue Shared(int sharedIndex) => new(false, true, sharedIndex, null);

            public static CellValue Number(int value) =>
                new(false, false, 0, value.ToString(CultureInfo.InvariantCulture));
        }

        #endregion
    }
}
#endif
