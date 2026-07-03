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
                EditorGUI.BeginDisabledGroup(true);
                if (GUILayout.Button("导出 Excel", GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                { }
                if (GUILayout.Button("导入 Excel", GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                { }
                EditorGUI.EndDisabledGroup();
            });

            GUILayout.Space(6f);

            DrawLabeledButtonRow("JSON", () =>
            {
                if (GUILayout.Button("导出 JSON", GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                    ExportJson();

                if (GUILayout.Button("导入 JSON", GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                    ImportJson();
            });
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
    /// 配置随机池与规则 执行在 View Editor
    /// </summary>
    public sealed class ItemLootSimPage
    {
        [HideInInspector]
        public ItemDataEditorWindow Window;

        [HideInInspector]
        public ItemDataEditorHomePage HomePage;

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
                "方案 A\n" +
                "· 此页：随机池 权重 容器筛选等配置（LootSimConfigSo）\n" +
                "· View Editor：Play 模式下向场景容器投放与模拟\n" +
                "· 后续随机算法读取同一份 LootSimConfigSo",
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
            }

            EditorGUILayout.Space(6f);
            EditorGUILayout.LabelField("待扩展", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("· 随机物品池条目");
            EditorGUILayout.LabelField("· 权重与数量范围");
            EditorGUILayout.LabelField("· 容器标签筛选");
            EditorGUILayout.LabelField("· 一键随机填充（调用 View Editor）");
        }
    }
}
#endif
