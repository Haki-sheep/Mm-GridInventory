#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// 枚举管理主页签
    /// </summary>
    public sealed class ItemDataEditorHomePage
    {
        private const string LastJsonPathKey = "MmInventory.ItemDataEditor.LastJsonPath";
        private const string LastExcelPathKey = "MmInventory.ItemDataEditor.LastExcelPath";

        [HideInInspector]
        public ItemDataEditorWindow Window;

        public ItemDataEditorHomePage(ItemDataEditorWindow window)
        {
            Window = window;
        }

        [Title("总库")]
        [Required]
        [OnValueChanged(nameof(OnListSoChanged))]
        public ItemTableDataListSo ListSo;

        [Title("视图预制体列表")]
        [Required]
        [OnValueChanged(nameof(OnViewPrefabListSoChanged))]
        public ItemViewPrefabListSo ViewPrefabListSo;

        [Title("投放模拟配置")]
        [OnValueChanged(nameof(OnLootSimConfigSoChanged))]
        public LootSimConfigSo LootSimConfigSo;

        [Title("输出设置")]
        [Sirenix.OdinInspector.FilePath(Extensions = "cs")]
        [OnValueChanged(nameof(OnEnumPathChanged))]
        public string EnumFilePath;

        [Title("添加类型")]
        public string NewEnumName = "NewType";

        [Button("添加类型", ButtonSizes.Medium)]
        private void AddEnumName()
        {
            string safeName = EItemTypeCodeGenerator.SanitizeEnumName(NewEnumName);
            if (EnumNameList.Contains(safeName))
                return;

            EnumNameList.Add(safeName);
            NewEnumName = "NewType";
            Window?.MarkDirty();
            Window?.RequestTreeRebuild();
        }

        [Title("枚举列表")]
        [ListDrawerSettings(HideAddButton = true, ShowPaging = false, DraggableItems = true)]
        [OnCollectionChanged(nameof(OnEnumListChanged))]
        public List<string> EnumNameList = new();

        [Button("生成枚举脚本", ButtonSizes.Large), GUIColor(0.35f, 0.78f, 0.45f)]
        private void GenerateEnumFile()
        {
            EItemTypeCodeGenerator.WriteEnum(EnumNameList, EnumFilePath);
            ItemDataEditorSession.EnumFilePath = EnumFilePath;
            Window?.RequestTreeRebuild();
        }

        [PropertySpace(8)]
        [OnInspectorGUI]
        private void DrawIoButtonSections()
        {
            DrawLabeledButtonRow("Excel", () =>
            {
                // 延后弹窗 避免打断 OnInspectorGUI 布局组
                if (GUILayout.Button("导出 Excel", GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                    EditorApplication.delayCall += ExportExcel;

                if (GUILayout.Button("导入 Excel", GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                    EditorApplication.delayCall += ImportExcel;
            });

            GUILayout.Space(6f);

            DrawLabeledButtonRow("JSON", () =>
            {
                if (GUILayout.Button("导出 JSON", GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                    EditorApplication.delayCall += ExportJson;

                if (GUILayout.Button("导入 JSON", GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                    EditorApplication.delayCall += ImportJson;
            });
        }

        private void ExportExcel()
        {
            string defaultPath = EditorPrefs.GetString(LastExcelPathKey, ItemDataExcelIO.GetDefaultAbsolutePath());
            string filePath = EditorUtility.SaveFilePanel(
                "导出物品 Excel",
                Path.GetDirectoryName(defaultPath),
                Path.GetFileNameWithoutExtension(defaultPath),
                "xlsx");

            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                ItemDataExcelIO.ExportToFile(ListSo, EnumNameList, filePath);
                EditorPrefs.SetString(LastExcelPathKey, filePath);
                AssetDatabase.Refresh();
                Debug.Log($"Excel 导出完成 {filePath}");
            }
            catch (IOException ex)
            {
                EditorUtility.DisplayDialog("导出失败", $"文件可能被 Excel 占用\n{ex.Message}", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("导出失败", ex.Message, "确定");
            }
        }

        private void ImportExcel()
        {
            string defaultPath = EditorPrefs.GetString(LastExcelPathKey, ItemDataExcelIO.GetDefaultAbsolutePath());
            string filePath = EditorUtility.OpenFilePanel("导入物品 Excel", Path.GetDirectoryName(defaultPath), "xlsx");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var exportFile = ItemDataExcelIO.ImportFromFile(filePath);
                EditorPrefs.SetString(LastExcelPathKey, filePath);
                ApplyImportFile(exportFile);
                Debug.Log($"Excel 导入完成 {filePath}");
            }
            catch (IOException ex)
            {
                EditorUtility.DisplayDialog("导入失败", $"文件可能被 Excel 占用\n{ex.Message}", "确定");
            }
            catch (Exception ex)
            {
                EditorUtility.DisplayDialog("导入失败", ex.Message, "确定");
            }
        }

        /// <summary>
        /// 将导入结果写入总库与枚举
        /// </summary>
        private void ApplyImportFile(ItemDataExportFile exportFile)
        {
            if (exportFile == null) return;

            if (exportFile.itemTypes != null && exportFile.itemTypes.Count > 0)
            {
                EnumNameList.Clear();
                EnumNameList.AddRange(exportFile.itemTypes);
                EItemTypeCodeGenerator.WriteEnum(EnumNameList, EnumFilePath);
            }

            if (ListSo != null && exportFile.items != null)
            {
                var itemList = ItemDataJsonIO.ToItemBaseDataList(exportFile.items);
                ListSo.EditorReplaceItems(itemList);
                ListSo.EditorSave();
            }

            Window?.ClearDirty();
            Window?.RequestTreeRebuild();
        }

        private static void DrawLabeledButtonRow(string title, Action drawButtonRow)
        {
            EditorGUILayout.LabelField(title, EditorStyles.boldLabel);
            using (new EditorGUILayout.HorizontalScope())
            {
                drawButtonRow();
            }
        }

        private void ExportJson()
        {
            string defaultPath = EditorPrefs.GetString(LastJsonPathKey, Application.dataPath);
            string filePath = EditorUtility.SaveFilePanel(
                "导出物品 JSON",
                Path.GetDirectoryName(defaultPath),
                "ItemDataExport",
                "json");

            if (string.IsNullOrEmpty(filePath)) return;

            ItemDataJsonIO.ExportToFile(ListSo, EnumNameList, filePath);
            EditorPrefs.SetString(LastJsonPathKey, filePath);
        }

        private void ImportJson()
        {
            string defaultPath = EditorPrefs.GetString(LastJsonPathKey, Application.dataPath);
            string filePath = EditorUtility.OpenFilePanel("导入物品 JSON", defaultPath, "json");
            if (string.IsNullOrEmpty(filePath)) return;

            var exportFile = ItemDataJsonIO.ImportFromFile(filePath);
            EditorPrefs.SetString(LastJsonPathKey, filePath);
            ApplyImportFile(exportFile);
        }

        /// <summary>
        /// 从会话加载
        /// </summary>
        public void LoadFromSession()
        {
            ListSo = ItemDataEditorSession.LoadListSo();
            ViewPrefabListSo = ItemDataEditorSession.LoadViewPrefabListSo();
            LootSimConfigSo = ItemDataEditorSession.LoadLootSimConfigSo();
            EnumFilePath = ItemDataEditorSession.EnumFilePath;
            EnumNameList = EItemTypeCodeGenerator.ReadEnumNames(EnumFilePath);
        }

        private void OnListSoChanged()
        {
            ItemDataEditorSession.SaveListSo(ListSo);
            Window?.RequestTreeRebuild();
        }

        private void OnViewPrefabListSoChanged()
        {
            ItemDataEditorSession.SaveViewPrefabListSo(ViewPrefabListSo);
        }

        private void OnLootSimConfigSoChanged()
        {
            ItemDataEditorSession.SaveLootSimConfigSo(LootSimConfigSo);
        }

        private void OnEnumPathChanged()
        {
            ItemDataEditorSession.EnumFilePath = EnumFilePath;
            EnumNameList = EItemTypeCodeGenerator.ReadEnumNames(EnumFilePath);
            Window?.RequestTreeRebuild();
        }

        private void OnEnumListChanged()
        {
            Window?.MarkDirty();
            Window?.RequestTreeRebuild();
        }
    }

    /// <summary>
    /// 分类目录页签
    /// </summary>
    public sealed class ItemDataCategoryPage
    {
        [HideInInspector]
        public ItemDataEditorWindow Window;

        [HideInInspector]
        public ItemDataEditorHomePage HomePage;

        [HideInInspector]
        public string TypeName;

        public ItemDataCategoryPage(ItemDataEditorWindow window, ItemDataEditorHomePage homePage, string typeName)
        {
            Window = window;
            HomePage = homePage;
            TypeName = typeName;
        }

        [ShowInInspector, ReadOnly]
        [LabelText("分类")]
        private string DisplayTypeName => TypeName;

        [ShowInInspector, ReadOnly]
        [LabelText("物品数量")]
        private int ItemCount => CountItemsInCategory();

        [Button("添加物品到此分类", ButtonSizes.Medium), GUIColor(0.35f, 0.78f, 0.45f)]
        private void AddItemToCategory()
        {
            var registry = HomePage.ListSo;
            if (registry == null) return;

            if (!Enum.TryParse(TypeName, out EItemType itemType))
                itemType = EItemType.Equipment;

            int nextId = FindNextExcelItemId(registry);
            registry.EditorAddItem(ItemTableData.Create(
                nextId,
                "新物品",
                string.Empty,
                Vector2Int.one,
                itemType,
                EItemStackType.NoStackable,
                1));

            Window?.MarkDirty();
            Window?.RequestTreeRebuild();
        }

        private int CountItemsInCategory()
        {
            var registry = HomePage?.ListSo;
            if (registry == null) return 0;

            int count = 0;
            var itemList = registry.ItemDataList;
            for (int i = 0; i < itemList.Count; i++)
            {
                if (string.Equals(itemList[i].ItemType.ToString(), TypeName, StringComparison.Ordinal))
                    count++;
            }

            return count;
        }

        private static int FindNextExcelItemId(ItemTableDataListSo listSo)
        {
            int maxId = -1;
            var itemList = listSo.ItemDataList;
            for (int i = 0; i < itemList.Count; i++)
                maxId = Math.Max(maxId, itemList[i].ExcelItemId);

            return maxId + 1;
        }
    }

    /// <summary>
    /// 单条物品页签
    /// </summary>
    public sealed class ItemDataEntryPage
    {
        [HideInInspector]
        public ItemDataEditorWindow Window;

        [HideInInspector]
        public ItemDataEditorHomePage HomePage;

        [HideInInspector]
        public int ItemIndex;

        public ItemDataEntryPage(ItemDataEditorWindow window, ItemDataEditorHomePage homePage, int itemIndex)
        {
            Window = window;
            HomePage = homePage;
            ItemIndex = itemIndex;
        }

        [OnInspectorGUI]
        private void DrawItemInspector()
        {
            var registry = HomePage?.ListSo;
            if (registry == null) return;

            var itemList = registry.ItemDataList;
            if (ItemIndex < 0 || ItemIndex >= itemList.Count) return;

            var serializedObject = new SerializedObject(registry);
            var listProperty = serializedObject.FindProperty("itemDataList");
            var elementProperty = listProperty.GetArrayElementAtIndex(ItemIndex);

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(elementProperty, GUIContent.none, true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(registry);
                Window?.MarkDirty();
            }

            DrawViewPrefabPreview(itemList[ItemIndex]);
        }

        /// <summary>
        /// 绘制视图预制体只读预览
        /// </summary>
        private void DrawViewPrefabPreview(ItemTableData item)
        {
            var viewRegistry = HomePage?.ViewPrefabListSo;

            EditorGUILayout.Space(10f);
            EditorGUILayout.LabelField("视图预览（只读）", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("占用尺寸", $"{item.DataSize.x} × {item.DataSize.y}");

            if (viewRegistry == null)
            {
                EditorGUILayout.HelpBox("请先在「枚举管理器」中指定视图预制体列表", MessageType.Warning);
                return;
            }

            GameObject prefab = viewRegistry.GetPrefab(item.DataSize);
            if (prefab != null)
            {
                EditorGUILayout.LabelField("对应预制体", prefab.name);
                EditorGUILayout.HelpBox("已配置", MessageType.Info);

                if (GUILayout.Button("定位预制体", GUILayout.Height(22f)))
                    EditorGUIUtility.PingObject(prefab);
            }
            else
            {
                EditorGUILayout.HelpBox(
                    $"视图预制体列表中未配置 {item.DataSize.x}×{item.DataSize.y}",
                    MessageType.Warning);
            }
        }

        [Button("删除此条目", ButtonSizes.Medium), GUIColor(0.92f, 0.35f, 0.35f)]
        private void DeleteItem()
        {
            var registry = HomePage?.ListSo;
            if (registry == null) return;

            registry.EditorRemoveAt(ItemIndex);
            Window?.MarkDirty();
            Window?.RequestTreeRebuild();
        }
    }

    /// <summary>
    /// 视图预制体尺寸表页签
    /// </summary>
    public sealed class ItemViewPrefabPage
    {
        [HideInInspector]
        public ItemDataEditorWindow Window;

        [HideInInspector]
        public ItemDataEditorHomePage HomePage;

        public ItemViewPrefabPage(ItemDataEditorWindow window, ItemDataEditorHomePage homePage)
        {
            Window = window;
            HomePage = homePage;
        }

        [ShowInInspector, ReadOnly]
        [LabelText("当前资产")]
        private string RegistryAssetLabel
        {
            get
            {
                var registry = HomePage?.ViewPrefabListSo;
                return registry != null ? registry.name : "(未指定)";
            }
        }

        [ShowInInspector, ReadOnly]
        [LabelText("映射数量")]
        private int EntryCount => HomePage?.ViewPrefabListSo?.EntryList.Count ?? 0;

        [OnInspectorGUI]
        private void DrawEntryList()
        {
            var viewRegistry = HomePage?.ViewPrefabListSo;
            if (viewRegistry == null)
            {
                EditorGUILayout.HelpBox("请先在「枚举管理器」中指定视图预制体列表", MessageType.Warning);
                return;
            }

            var serializedObject = new SerializedObject(viewRegistry);
            SerializedProperty listProperty = serializedObject.FindProperty("entryList");

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(listProperty, new GUIContent("尺寸预制体映射"), true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(viewRegistry);
                Window?.MarkDirty();
            }
        }
    }

    /// <summary>
    /// 容器投放模拟页签
    /// 配置密度档与 content 候选池 执行在 View Editor
    /// </summary>
    public sealed class ItemLootSimPage
    {
        private static readonly string[] GradeNameList = { "A", "B", "C", "D" };

        [HideInInspector]
        public ItemDataEditorWindow Window;

        [HideInInspector]
        public ItemDataEditorHomePage HomePage;

        /// <summary> 期望预览用缓存 </summary>
        private readonly Dictionary<EItemRarity, float> expectedRarityDict = new();

        private static readonly string[] RarityNameList = { "白", "蓝", "紫", "金", "红" };

        public ItemLootSimPage(ItemDataEditorWindow window, ItemDataEditorHomePage homePage)
        {
            Window = window;
            HomePage = homePage;
        }

        [ShowInInspector, ReadOnly]
        [LabelText("当前配置资产")]
        private string ConfigAssetLabel
        {
            get
            {
                var configSo = HomePage?.LootSimConfigSo;
                return configSo != null ? configSo.name : "(未指定)";
            }
        }

        [OnInspectorGUI]
        private void DrawLootSimGuide()
        {
            EditorGUILayout.HelpBox(
                "名词解释\n" +
                "· A~D 只管空箱率与稀有度权重\n" +
                "· 内容方向配最少/最多物品个数 以及允许类型\n" +
                "· 实际投放 先按档位判空箱 再从内容区间随机 N 然后每轮 roll 稀有度并从总表抽一件 放不下跳过",
                MessageType.Info);

            var configSo = HomePage?.LootSimConfigSo;
            if (configSo is null)
            {
                EditorGUILayout.HelpBox("请先在「枚举管理器」中指定投放模拟配置", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6f);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("定位配置资产", GUILayout.Height(24f)))
                    EditorGUIUtility.PingObject(configSo);

                if (GUILayout.Button("打开 View Editor", GUILayout.Height(24f)))
                    EditorApplication.ExecuteMenuItem("Tools/MmInventory/View Editor");

                if (GUILayout.Button("保存配置", GUILayout.Height(24f)))
                {
                    configSo.EditorSave();
                    Window?.MarkDirty();
                }
            }

            EditorGUILayout.Space(8f);
            DrawDensitySection(configSo);
            EditorGUILayout.Space(8f);
            DrawContentSection(configSo);
        }

        /// <summary>
        /// 绘制 A~D 密度档
        /// </summary>
        private void DrawDensitySection(LootSimConfigSo configSo)
        {
            EditorGUILayout.LabelField("密度档 A~D", EditorStyles.boldLabel);

            var serializedObject = new SerializedObject(configSo);
            serializedObject.Update();
            var densityProperty = serializedObject.FindProperty("densityProfiles");
            if (densityProperty is null || !densityProperty.isArray)
            {
                EditorGUILayout.HelpBox("densityProfiles 字段缺失", MessageType.Error);
                return;
            }

            while (densityProperty.arraySize < 4)
                densityProperty.InsertArrayElementAtIndex(densityProperty.arraySize);

            if (densityProperty.arraySize > 4)
                densityProperty.arraySize = 4;

            EditorGUI.BeginChangeCheck();
            for (int i = 0; i < 4; i++)
            {
                var elementProperty = densityProperty.GetArrayElementAtIndex(i);
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                EditorGUILayout.LabelField($"等级 {GradeNameList[i]}", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(elementProperty, GUIContent.none, true);
                DrawExpectedPreview(configSo, (ELootGrade)i);
                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(4f);
            }

            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(configSo);
                Window?.MarkDirty();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }
        }

        /// <summary>
        /// 绘制 content 候选表
        /// </summary>
        private void DrawContentSection(LootSimConfigSo configSo)
        {
            EditorGUILayout.LabelField("内容方向候选池", EditorStyles.boldLabel);

            var serializedObject = new SerializedObject(configSo);
            serializedObject.Update();
            var contentProperty = serializedObject.FindProperty("contentTableList");
            if (contentProperty is null)
            {
                EditorGUILayout.HelpBox("contentTableList 字段缺失", MessageType.Error);
                return;
            }

            EditorGUI.BeginChangeCheck();
            EditorGUILayout.PropertyField(contentProperty, new GUIContent("内容方向表"), true);
            if (EditorGUI.EndChangeCheck())
            {
                serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(configSo);
                Window?.MarkDirty();
            }
            else
            {
                serializedObject.ApplyModifiedProperties();
            }

            DrawContentPoolWarnings(configSo);
        }

        /// <summary>
        /// 期望预览 空箱与稀有度占比
        /// </summary>
        private void DrawExpectedPreview(LootSimConfigSo configSo, ELootGrade grade)
        {
            if (!configSo.TryGetDensity(grade, out var density) || density is null)
                return;

            EditorGUILayout.LabelField($"空箱率 {density.EmptyChance:P0}", EditorStyles.miniLabel);

            LootRuntime.CalcRarityShareDict(density, expectedRarityDict);
            if (expectedRarityDict.Count == 0)
            {
                EditorGUILayout.HelpBox("稀有度权重未配置", MessageType.Warning);
                return;
            }

            foreach (var pair in expectedRarityDict)
            {
                EditorGUILayout.LabelField(
                    $"· {GetRarityLabel(pair.Key)} 占比 {pair.Value:P0}",
                    EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 候选类型覆盖预警
        /// </summary>
        private void DrawContentPoolWarnings(LootSimConfigSo configSo)
        {
            var contentTableList = configSo.ContentTableList;
            if (contentTableList is null || contentTableList.Count == 0)
            {
                EditorGUILayout.HelpBox("尚未配置任何内容方向", MessageType.Warning);
                return;
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("总表覆盖检查", EditorStyles.boldLabel);

            for (int i = 0; i < contentTableList.Count; i++)
            {
                var table = contentTableList[i];
                if (table is null)
                    continue;

                if (string.IsNullOrEmpty(table.ContentId))
                {
                    EditorGUILayout.HelpBox($"第 {i} 张表 contentId 为空", MessageType.Warning);
                    continue;
                }

                var allowedList = table.AllowedItemTypeList;
                if (allowedList is null || allowedList.Count == 0)
                {
                    EditorGUILayout.HelpBox($"[{table.ContentId}] 未勾选任何允许类型", MessageType.Warning);
                    continue;
                }

                int countMin = Mathf.Max(0, table.ItemCountMin);
                int countMax = Mathf.Max(countMin, table.ItemCountMax);
                EditorGUILayout.LabelField(
                    $"[{table.ContentId}] 件数 {countMin}~{countMax} 期望约 {LootRuntime.CalcExpectedRollCount(table):0.##}",
                    EditorStyles.miniBoldLabel);
                for (int t = 0; t < allowedList.Count; t++)
                {
                    var eItemType = allowedList[t];
                    for (int rarityIndex = 0; rarityIndex <= (int)EItemRarity.Red; rarityIndex++)
                    {
                        var eItemRarity = (EItemRarity)rarityIndex;
                        int count = LootRuntime.CountTableItems(eItemType, eItemRarity);
                        if (count > 0)
                            continue;

                        EditorGUILayout.HelpBox(
                            $"[{table.ContentId}] 类型 {eItemType} + {GetRarityLabel(eItemRarity)} 总表无条目 抽中将降级",
                            MessageType.Warning);
                    }
                }
            }
        }

        /// <summary>
        /// 稀有度中文名
        /// </summary>
        private static string GetRarityLabel(EItemRarity eItemRarity)
        {
            int index = (int)eItemRarity;
            if (index < 0 || index >= RarityNameList.Length)
                return eItemRarity.ToString();

            return RarityNameList[index];
        }
    }
}
#endif
