#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// 物品配置 SO 编辑器
    /// </summary>
    public sealed class ItemDataEditorWindow : EditorWindow
    {
        private const string LastJsonPathKey = "MmInventory.ItemDataEditor.LastJsonPath";
        private const string LeftPanelWidthKey = "MmInventory.ItemDataEditor.LeftPanelWidth";
        private const float LeftPanelMinWidth = 240f;
        private const float RightPanelMinWidth = 360f;
        private const float SplitterWidth = 4f;
        private const float StatusBarHeight = 40f;
        private const float LeftLabelWidth = 52f;
        private const float SoManagerMinHeight = 160f;

        private ItemBaseDataListSo targetListSo;
        private SerializedObject serializedListSo;
        private SerializedProperty itemListProperty;

        private readonly List<string> itemTypeNameList = new();
        private readonly List<ItemBaseDataListSo> managedSoList = new();

        private Vector2 enumScrollPos;
        private Vector2 soListScrollPos;
        private Vector2 rightScrollPos;
        private string statusMessage = "就绪";
        private string newEnumName = "NewType";
        private string enumFilePath;
        private string createSoFolder;
        private string newSoAssetName = "ItemBaseDataList";
        private string soSearchKeyword = string.Empty;
        private float leftPanelWidth = 300f;
        private bool isDraggingSplitter;

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/MmInventory/Item Data Editor")]
        private static void Open()
        {
            var window = GetWindow<ItemDataEditorWindow>("Item Data Editor");
            window.minSize = new Vector2(720f, 480f);
            window.Show();
        }

        private void OnEnable()
        {
            leftPanelWidth = EditorPrefs.GetFloat(LeftPanelWidthKey, 300f);
            enumFilePath = ItemDataEditorSession.EnumFilePath;
            createSoFolder = ItemDataEditorSession.CreateSoFolder;
            LoadManagedSoList();
            ReloadEnumNames();
            SelectActiveSo(ItemDataEditorSession.LoadActiveSo());
        }

        private void OnDisable()
        {
            ItemDataEditorSession.EnumFilePath = enumFilePath;
            ItemDataEditorSession.CreateSoFolder = createSoFolder;
            ItemDataEditorSession.SaveManagedSoList(managedSoList);
        }

        private void OnGUI()
        {
            float maxLeftWidth = Mathf.Max(LeftPanelMinWidth, position.width - RightPanelMinWidth - SplitterWidth);
            leftPanelWidth = Mathf.Clamp(leftPanelWidth, LeftPanelMinWidth, maxLeftWidth);

            float bodyHeight = position.height - StatusBarHeight;

            using (new EditorGUILayout.HorizontalScope(GUILayout.Height(bodyHeight)))
            {
                DrawLeftPanel(leftPanelWidth, bodyHeight);
                DrawSplitter(maxLeftWidth);
                DrawRightPanel(bodyHeight);
            }

            DrawStatusBar();
            HandleSplitterDrag(maxLeftWidth);
        }

        /// <summary>
        /// 左侧面板
        /// </summary>
        private void DrawLeftPanel(float width, float bodyHeight)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(width), GUILayout.ExpandHeight(true)))
            {
                float oldLabelWidth = EditorGUIUtility.labelWidth;
                EditorGUIUtility.labelWidth = LeftLabelWidth;

                DrawToolbar();
                EditorGUILayout.Space(6f);
                DrawEnumPathSection();
                EditorGUILayout.Space(4f);
                DrawEnumSection(bodyHeight);
                GUILayout.FlexibleSpace();
                EditorGUILayout.Space(6f);
                DrawJsonSection();

                EditorGUIUtility.labelWidth = oldLabelWidth;
            }
        }

        /// <summary>
        /// 右侧 SO 编辑
        /// </summary>
        private void DrawRightPanel(float bodyHeight)
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.ExpandWidth(true), GUILayout.Height(bodyHeight)))
            {
                float managerHeight = Mathf.Clamp(bodyHeight * 0.36f, SoManagerMinHeight, 260f);
                DrawSoManagerSection(managerHeight);

                EditorGUILayout.Space(4f);
                DrawSoEditorSection(bodyHeight - managerHeight - 8f);
            }
        }

        /// <summary>
        /// SO 列表管理
        /// </summary>
        private void DrawSoManagerSection(float sectionHeight)
        {
            EditorGUILayout.LabelField("SO 列表管理", EditorStyles.boldLabel);

            soSearchKeyword = EditorGUILayout.TextField("搜索", soSearchKeyword);

            float listHeight = sectionHeight - 108f;
            using (var scroll = new EditorGUILayout.ScrollViewScope(
                       soListScrollPos,
                       GUILayout.ExpandWidth(true),
                       GUILayout.Height(Mathf.Max(72f, listHeight))))
            {
                soListScrollPos = scroll.scrollPosition;

                for (int i = 0; i < managedSoList.Count; i++)
                {
                    var so = managedSoList[i];
                    if (so == null) continue;
                    if (!IsSoMatchSearch(so)) continue;

                    using (new EditorGUILayout.HorizontalScope())
                    {
                        bool isActive = targetListSo == so;
                        string label = $"{so.name} ({so.ItemDataList.Count})";

                        Color oldBg = GUI.backgroundColor;
                        GUI.backgroundColor = isActive
                            ? new Color(0.30f, 0.52f, 0.85f, 1f)
                            : new Color(0.32f, 0.32f, 0.32f, 1f);

                        if (GUILayout.Button(label, EditorStyles.miniButton, GUILayout.ExpandWidth(true), GUILayout.Height(20f)))
                            SelectActiveSo(so);

                        GUI.backgroundColor = oldBg;

                        if (GUILayout.Button("移除", GUILayout.Width(40f), GUILayout.Height(20f)))
                        {
                            RemoveManagedSo(so);
                            break;
                        }

                        if (GUILayout.Button("删资产", GUILayout.Width(48f), GUILayout.Height(20f)))
                        {
                            DeleteManagedSoAsset(so);
                            break;
                        }
                    }
                }
            }

            var dragSo = (ItemBaseDataListSo)EditorGUILayout.ObjectField(
                "拖入添加",
                null,
                typeof(ItemBaseDataListSo),
                false);
            if (dragSo != null)
                AddManagedSo(dragSo);

            using (new EditorGUILayout.HorizontalScope())
            {
                newSoAssetName = EditorGUILayout.TextField("新 SO 名", newSoAssetName, GUILayout.MinWidth(80f));
                if (GUILayout.Button("创建", GUILayout.Width(44f), GUILayout.Height(20f)))
                    CreateNewSoAsset();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                createSoFolder = EditorGUILayout.TextField("创建目录", createSoFolder, GUILayout.MinWidth(80f));
                if (GUILayout.Button("选目录", GUILayout.Width(48f), GUILayout.Height(20f)))
                    PickCreateSoFolder();
            }
        }

        /// <summary>
        /// 当前 SO 物品编辑
        /// </summary>
        private void DrawSoEditorSection(float sectionHeight)
        {
            EditorGUILayout.LabelField("物品编辑", EditorStyles.boldLabel);

            if (targetListSo == null)
            {
                EditorGUILayout.HelpBox("请从列表选择 SO 或拖入添加", MessageType.Info);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                EditorGUILayout.LabelField($"当前 {targetListSo.name}", EditorStyles.miniLabel, GUILayout.ExpandWidth(true));
                if (GUILayout.Button("定位", GUILayout.Width(40f), GUILayout.Height(18f)))
                {
                    EditorGUIUtility.PingObject(targetListSo);
                    Selection.activeObject = targetListSo;
                }
            }

            if (serializedListSo == null)
                BindSerializedObject();

            using (var scroll = new EditorGUILayout.ScrollViewScope(
                       rightScrollPos,
                       GUILayout.ExpandWidth(true),
                       GUILayout.Height(Mathf.Max(120f, sectionHeight - 42f))))
            {
                rightScrollPos = scroll.scrollPosition;

                serializedListSo.Update();
                EditorGUILayout.PropertyField(itemListProperty, true);
                if (serializedListSo.ApplyModifiedProperties())
                {
                    EditorUtility.SetDirty(targetListSo);
                    statusMessage = $"已修改 {targetListSo.name}";
                }
            }
        }

        /// <summary>
        /// 枚举路径区
        /// </summary>
        private void DrawEnumPathSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                enumFilePath = EditorGUILayout.TextField("枚举路径", enumFilePath, GUILayout.MinWidth(80f));
                if (GUILayout.Button("浏览", GUILayout.Width(44f), GUILayout.Height(18f)))
                    PickEnumFilePath();
            }
        }

        /// <summary>
        /// 分栏拖拽条
        /// </summary>
        private void DrawSplitter(float maxLeftWidth)
        {
            Rect splitterRect = GUILayoutUtility.GetRect(
                SplitterWidth,
                SplitterWidth,
                GUILayout.ExpandHeight(true),
                GUILayout.Width(SplitterWidth));

            EditorGUI.DrawRect(splitterRect, new Color(0.18f, 0.18f, 0.18f, 1f));
            EditorGUIUtility.AddCursorRect(splitterRect, MouseCursor.ResizeHorizontal);

            if (Event.current.type == EventType.MouseDown
                && Event.current.button == 0
                && splitterRect.Contains(Event.current.mousePosition))
            {
                isDraggingSplitter = true;
                Event.current.Use();
            }
        }

        /// <summary>
        /// 处理分栏拖拽
        /// </summary>
        private void HandleSplitterDrag(float maxLeftWidth)
        {
            if (!isDraggingSplitter) return;

            if (Event.current.type == EventType.MouseDrag)
            {
                leftPanelWidth = Mathf.Clamp(Event.current.mousePosition.x, LeftPanelMinWidth, maxLeftWidth);
                EditorPrefs.SetFloat(LeftPanelWidthKey, leftPanelWidth);
                Repaint();
                Event.current.Use();
            }

            if (Event.current.type == EventType.MouseUp)
                isDraggingSplitter = false;
        }

        /// <summary>
        /// 底部状态栏
        /// </summary>
        private void DrawStatusBar()
        {
            Rect statusRect = new Rect(0f, position.height - StatusBarHeight, position.width, StatusBarHeight);
            EditorGUI.DrawRect(statusRect, new Color(0.16f, 0.16f, 0.16f, 1f));
            Rect labelRect = new Rect(8f, statusRect.y + 10f, statusRect.width - 16f, 20f);
            EditorGUI.LabelField(labelRect, statusMessage, EditorStyles.miniLabel);
        }

        /// <summary>
        /// 顶部工具栏
        /// </summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存 SO", GUILayout.Height(22f), GUILayout.ExpandWidth(true)))
                    SaveTargetSo();

                if (GUILayout.Button("刷新枚举", GUILayout.Height(22f), GUILayout.ExpandWidth(true)))
                    ReloadEnumNames();
            }
        }

        /// <summary>
        /// 枚举编辑区
        /// </summary>
        private void DrawEnumSection(float bodyHeight)
        {
            string enumTypeName = EItemTypeCodeGenerator.ResolveEnumTypeName(enumFilePath);
            EditorGUILayout.LabelField($"枚举 {enumTypeName}", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                newEnumName = EditorGUILayout.TextField("新类型", newEnumName, GUILayout.MinWidth(60f));
                if (GUILayout.Button("添加", GUILayout.Width(44f), GUILayout.Height(20f)))
                    AddEnumName(newEnumName);
            }

            float enumListHeight = Mathf.Clamp(bodyHeight * 0.34f, 80f, 220f);
            using (var scroll = new EditorGUILayout.ScrollViewScope(
                       enumScrollPos,
                       GUILayout.ExpandWidth(true),
                       GUILayout.Height(enumListHeight)))
            {
                enumScrollPos = scroll.scrollPosition;

                for (int i = 0; i < itemTypeNameList.Count; i++)
                {
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        itemTypeNameList[i] = EditorGUILayout.TextField(
                            GUIContent.none,
                            itemTypeNameList[i],
                            GUILayout.MinWidth(60f),
                            GUILayout.ExpandWidth(true));

                        if (GUILayout.Button("删", GUILayout.Width(24f), GUILayout.Height(18f)))
                        {
                            itemTypeNameList.RemoveAt(i);
                            break;
                        }
                    }
                }
            }

            if (GUILayout.Button("生成枚举脚本", GUILayout.Height(24f), GUILayout.ExpandWidth(true)))
                GenerateEnumFile();
        }

        /// <summary>
        /// JSON 区
        /// </summary>
        private void DrawJsonSection()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存", GUILayout.Height(22f), GUILayout.ExpandWidth(true)))
                    SaveTargetSo();

                if (GUILayout.Button("加载", GUILayout.Height(22f), GUILayout.ExpandWidth(true)))
                    ReloadTargetSo();
            }

            EditorGUILayout.Space(4f);
            EditorGUILayout.LabelField("JSON", EditorStyles.boldLabel);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("导出 JSON", GUILayout.Height(22f), GUILayout.ExpandWidth(true)))
                    ExportJson();

                if (GUILayout.Button("导入 JSON", GUILayout.Height(22f), GUILayout.ExpandWidth(true)))
                    ImportJson();
            }
        }

        private void LoadManagedSoList()
        {
            managedSoList.Clear();
            managedSoList.AddRange(ItemDataEditorSession.LoadManagedSoList());
        }

        private void PersistManagedSoList()
        {
            ItemDataEditorSession.SaveManagedSoList(managedSoList);
        }

        private void SelectActiveSo(ItemBaseDataListSo so)
        {
            targetListSo = so;
            BindSerializedObject();

            if (so == null)
            {
                ItemDataEditorSession.ActiveSoGuid = string.Empty;
                return;
            }

            string path = AssetDatabase.GetAssetPath(so);
            ItemDataEditorSession.ActiveSoGuid = AssetDatabase.AssetPathToGUID(path);
            statusMessage = $"已选择 {so.name}";
        }

        private void AddManagedSo(ItemBaseDataListSo so)
        {
            if (so == null) return;
            if (managedSoList.Contains(so))
            {
                statusMessage = $"{so.name} 已在列表中";
                SelectActiveSo(so);
                return;
            }

            managedSoList.Add(so);
            PersistManagedSoList();
            SelectActiveSo(so);
            statusMessage = $"已添加 {so.name}";
        }

        private void RemoveManagedSo(ItemBaseDataListSo so)
        {
            if (so == null) return;

            managedSoList.Remove(so);
            PersistManagedSoList();

            if (targetListSo == so)
                SelectActiveSo(managedSoList.Count > 0 ? managedSoList[0] : null);

            statusMessage = $"已移除 {so.name}";
        }

        private void DeleteManagedSoAsset(ItemBaseDataListSo so)
        {
            if (so == null) return;

            string path = AssetDatabase.GetAssetPath(so);
            if (string.IsNullOrEmpty(path)) return;

            if (!EditorUtility.DisplayDialog("删除 SO 资产", $"确认删除 {so.name} ?", "删除", "取消"))
                return;

            RemoveManagedSo(so);
            AssetDatabase.DeleteAsset(path);
            AssetDatabase.SaveAssets();
            statusMessage = $"已删除资产 {so.name}";
        }

        private void CreateNewSoAsset()
        {
            var so = ItemBaseDataListSoEditorUtil.CreateAsset(createSoFolder, newSoAssetName);
            if (so == null)
            {
                statusMessage = "创建 SO 失败";
                return;
            }

            createSoFolder = ItemBaseDataListSoEditorUtil.NormalizeAssetFolder(createSoFolder);
            ItemDataEditorSession.CreateSoFolder = createSoFolder;
            AddManagedSo(so);
            statusMessage = $"已创建 {AssetDatabase.GetAssetPath(so)}";
        }

        private void PickCreateSoFolder()
        {
            string defaultFolder = createSoFolder;
            if (defaultFolder.StartsWith("Assets/"))
                defaultFolder = Path.Combine(Application.dataPath, defaultFolder.Substring("Assets/".Length));

            string picked = EditorUtility.OpenFolderPanel("选择 SO 创建目录", defaultFolder, string.Empty);
            if (string.IsNullOrEmpty(picked)) return;

            string assetPath = ItemBaseDataListSoEditorUtil.ToAssetPath(picked);
            if (string.IsNullOrEmpty(assetPath))
            {
                statusMessage = "目录必须在 Assets 下";
                return;
            }

            createSoFolder = assetPath;
            ItemDataEditorSession.CreateSoFolder = createSoFolder;
            statusMessage = $"创建目录 {createSoFolder}";
        }

        private void PickEnumFilePath()
        {
            string defaultFolder = Path.GetDirectoryName(enumFilePath);
            if (!string.IsNullOrEmpty(defaultFolder) && defaultFolder.StartsWith("Assets/"))
                defaultFolder = Path.Combine(Application.dataPath, defaultFolder.Substring("Assets/".Length));

            string picked = EditorUtility.SaveFilePanel("选择枚举脚本路径", defaultFolder ?? Application.dataPath, "EItemType", "cs");
            if (string.IsNullOrEmpty(picked)) return;

            string assetPath = ItemBaseDataListSoEditorUtil.ToAssetPath(picked);
            if (string.IsNullOrEmpty(assetPath))
            {
                statusMessage = "枚举脚本必须在 Assets 下";
                return;
            }

            enumFilePath = assetPath;
            ItemDataEditorSession.EnumFilePath = enumFilePath;
            ReloadEnumNames();
            statusMessage = $"枚举路径 {enumFilePath}";
        }

        private bool IsSoMatchSearch(ItemBaseDataListSo so)
        {
            if (string.IsNullOrWhiteSpace(soSearchKeyword)) return true;
            return so.name.IndexOf(soSearchKeyword, System.StringComparison.OrdinalIgnoreCase) >= 0;
        }

        private void BindSerializedObject()
        {
            if (targetListSo == null)
            {
                serializedListSo = null;
                itemListProperty = null;
                return;
            }

            serializedListSo = new SerializedObject(targetListSo);
            itemListProperty = serializedListSo.FindProperty("itemDataList");
        }

        private void ReloadEnumNames()
        {
            itemTypeNameList.Clear();
            itemTypeNameList.AddRange(EItemTypeCodeGenerator.ReadEnumNames(enumFilePath));
            statusMessage = $"已读取枚举 {itemTypeNameList.Count} 项";
        }

        private void AddEnumName(string rawName)
        {
            string safeName = EItemTypeCodeGenerator.SanitizeEnumName(rawName);
            if (itemTypeNameList.Contains(safeName))
            {
                statusMessage = $"枚举名 {safeName} 已存在";
                return;
            }

            itemTypeNameList.Add(safeName);
            newEnumName = "NewType";
            statusMessage = $"已添加 {safeName}";
        }

        private void GenerateEnumFile()
        {
            EItemTypeCodeGenerator.WriteEnum(itemTypeNameList, enumFilePath);
            ItemDataEditorSession.EnumFilePath = enumFilePath;
            statusMessage = $"已生成 {enumFilePath}";
            ReloadEnumNames();
        }

        private void SaveTargetSo()
        {
            if (targetListSo == null)
            {
                statusMessage = "未选择 SO";
                return;
            }

            serializedListSo?.ApplyModifiedProperties();
            EditorUtility.SetDirty(targetListSo);
            AssetDatabase.SaveAssets();
            statusMessage = $"已保存 {targetListSo.name}";
        }

        /// <summary>
        /// 从磁盘重新加载当前 SO
        /// </summary>
        private void ReloadTargetSo()
        {
            if (targetListSo == null)
            {
                statusMessage = "未选择 SO";
                return;
            }

            string path = AssetDatabase.GetAssetPath(targetListSo);
            if (string.IsNullOrEmpty(path))
            {
                statusMessage = "SO 路径无效";
                return;
            }

            AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
            var reloaded = AssetDatabase.LoadAssetAtPath<ItemBaseDataListSo>(path);
            if (reloaded == null)
            {
                statusMessage = "加载 SO 失败";
                return;
            }

            targetListSo = reloaded;
            BindSerializedObject();
            statusMessage = $"已加载 {reloaded.name}";
            Repaint();
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

            try
            {
                ItemDataJsonIO.ExportToFile(targetListSo, itemTypeNameList, filePath);
                EditorPrefs.SetString(LastJsonPathKey, filePath);
                statusMessage = $"已导出 {filePath}";
            }
            catch (JsonException ex)
            {
                statusMessage = $"导出失败 {ex.Message}";
            }
        }

        private void ImportJson()
        {
            string defaultPath = EditorPrefs.GetString(LastJsonPathKey, Application.dataPath);
            string filePath = EditorUtility.OpenFilePanel("导入物品 JSON", defaultPath, "json");
            if (string.IsNullOrEmpty(filePath)) return;

            try
            {
                var exportFile = ItemDataJsonIO.ImportFromFile(filePath);
                EditorPrefs.SetString(LastJsonPathKey, filePath);

                if (exportFile.itemTypes != null && exportFile.itemTypes.Count > 0)
                {
                    itemTypeNameList.Clear();
                    itemTypeNameList.AddRange(exportFile.itemTypes);
                    EItemTypeCodeGenerator.WriteEnum(itemTypeNameList, enumFilePath);
                }

                if (targetListSo != null && exportFile.items != null)
                {
                    var itemList = ItemDataJsonIO.ToItemBaseDataList(exportFile.items);
                    targetListSo.EditorReplaceItems(itemList);
                    EditorUtility.SetDirty(targetListSo);
                    BindSerializedObject();
                }

                statusMessage = $"已导入 {Path.GetFileName(filePath)}";
                Repaint();
            }
            catch (JsonException ex)
            {
                statusMessage = $"导入失败 {ex.Message}";
            }
        }
    }
}
#endif
