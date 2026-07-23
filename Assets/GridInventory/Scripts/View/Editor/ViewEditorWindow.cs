#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace MmInventory.Editor
{
    /// <summary>
    /// 背包容器 GM 编辑器
    /// </summary>
    public sealed class ViewEditorWindow : EditorWindow
    {
        private const float LeftPanelWidth = 220f;

        private readonly List<GridContainerView> containerList = new();
        private readonly List<ItemTableData> itemOptionList = new();

        private GridContainerView selectedContainer;
        private Vector2 containerScrollPos;
        private Vector2 gmPanelScrollPos;
        private int selectedItemIndex;
        private int spawnAnchorX;
        private int spawnAnchorY;
        private bool spawnAtFirstEmpty;
        private bool syncSelectionToInspector;
        private string statusMessage = "就绪";

        private string[] itemLabels = System.Array.Empty<string>();

        private const string SyncSelectionPrefKey = "MmInventory.ViewEditor.SyncSelectionToInspector";

        /// <summary>
        /// 打开窗口
        /// </summary>
        [MenuItem("Tools/MmInventory/View Editor")]
        private static void Open()
        {
            var window = GetWindow<ViewEditorWindow>("View Editor");
            window.minSize = new Vector2(720f, 420f);
            window.Show();
        }

        private void OnEnable()
        {
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
            syncSelectionToInspector = EditorPrefs.GetBool(SyncSelectionPrefKey, false);
            RefreshContainers();
            RefreshItemOptions();
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            if (state != PlayModeStateChange.EnteredPlayMode
                && state != PlayModeStateChange.EnteredEditMode)
                return;

            EditorApplication.delayCall += RefreshContainersDelayed;
        }

        private void RefreshContainersDelayed()
        {
            if (this == null)
                return;

            RefreshContainers();
            Repaint();
        }

        private void OnGUI()
        {
            SanitizeContainerRefs();
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope(GUILayout.ExpandHeight(true)))
            {
                DrawContainerPanel();
                DrawGmPanel();
            }
        }

        /// <summary>
        /// 清理已销毁容器引用
        /// Unity 销毁对象后 is null 无效 需用 ==
        /// </summary>
        private void SanitizeContainerRefs()
        {
            for (int i = containerList.Count - 1; i >= 0; i--)
            {
                if (containerList[i] == null)
                    containerList.RemoveAt(i);
            }

            if (selectedContainer == null && containerList.Count > 0)
                selectedContainer = containerList[0];
        }

        /// <summary>
        /// 容器引用是否仍有效
        /// </summary>
        private static bool IsContainerAlive(GridContainerView container)
        {
            return container != null;
        }

        /// <summary>
        /// 顶部工具栏
        /// </summary>
        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                if (GUILayout.Button("刷新容器", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    RefreshContainers();

                if (GUILayout.Button("刷新配表", EditorStyles.toolbarButton, GUILayout.Width(72f)))
                    RefreshItemOptions();

                EditorGUI.BeginChangeCheck();
                syncSelectionToInspector = GUILayout.Toggle(
                    syncSelectionToInspector,
                    "同步Inspector",
                    EditorStyles.toolbarButton,
                    GUILayout.Width(96f));
                if (EditorGUI.EndChangeCheck())
                    EditorPrefs.SetBool(SyncSelectionPrefKey, syncSelectionToInspector);

                GUILayout.FlexibleSpace();
                GUILayout.Label(statusMessage, EditorStyles.miniLabel);
            }
        }

        /// <summary>
        /// 左侧容器列表
        /// </summary>
        private void DrawContainerPanel()
        {
            using (new EditorGUILayout.VerticalScope(GUILayout.Width(LeftPanelWidth)))
            {
                EditorGUILayout.LabelField("背包容器", EditorStyles.boldLabel);

                containerScrollPos = EditorGUILayout.BeginScrollView(containerScrollPos);
                for (int i = 0; i < containerList.Count; i++)
                {
                    var container = containerList[i];
                    if (!IsContainerAlive(container))
                        continue;

                    bool isSelected = selectedContainer == container;
                    string label = $"{container.ContainerName}  {container.gridRowAndCloumns.x}x{container.gridRowAndCloumns.y}";
                    if (GUILayout.Toggle(isSelected, label, "Button"))
                    {
                        selectedContainer = container;
                        if (syncSelectionToInspector)
                            Selection.activeGameObject = container.gameObject;
                    }
                }
                EditorGUILayout.EndScrollView();

                if (containerList.Count == 0)
                    EditorGUILayout.HelpBox("场景中没有 GridMainContainerView", MessageType.Info);
            }
        }

        /// <summary>
        /// 右侧 GM 面板
        /// </summary>
        private void DrawGmPanel()
        {
            gmPanelScrollPos = EditorGUILayout.BeginScrollView(
                gmPanelScrollPos,
                GUILayout.ExpandWidth(true),
                GUILayout.ExpandHeight(true));

            if (!IsContainerAlive(selectedContainer))
            {
                EditorGUILayout.HelpBox("请选择一个背包容器", MessageType.Info);
                EditorGUILayout.EndScrollView();
                return;
            }

            EditorGUILayout.LabelField("容器信息", EditorStyles.boldLabel);
            EditorGUILayout.LabelField("名称", selectedContainer.ContainerName);
            EditorGUILayout.LabelField("行列", selectedContainer.gridRowAndCloumns.ToString());
            EditorGUILayout.LabelField("存档 ID", selectedContainer.ContainerId.ToString());
            EditorGUILayout.LabelField("自动存档", selectedContainer.EnablePersistence ? "开" : "关");
            EditorGUILayout.LabelField("运行状态", Application.isPlaying ? "Play" : "Edit");

            EditorGUILayout.Space(8f);
            DrawSpawnPanel();
            EditorGUILayout.Space(8f);
            DrawLootPanel();
            EditorGUILayout.Space(8f);
            DrawPersistPanel();
            EditorGUILayout.Space(8f);
            DrawItemListPanel();

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// GM 投放区
        /// </summary>
        private void DrawSpawnPanel()
        {
            EditorGUILayout.LabelField("GM 投放", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("投放功能需在 Play 模式下使用", MessageType.Warning);
                return;
            }

            if (!selectedContainer.IsInventoryReady)
            {
                EditorGUILayout.HelpBox("容器逻辑尚未初始化 请确认已进入 Play", MessageType.Warning);
                return;
            }

            if (itemLabels.Length == 0)
                EditorGUILayout.HelpBox("配表为空 请先在 Item Data Editor 配置物品", MessageType.Warning);

            selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, Mathf.Max(0, itemLabels.Length - 1));
            selectedItemIndex = EditorGUILayout.Popup("物品", selectedItemIndex, itemLabels);

            spawnAtFirstEmpty = EditorGUILayout.Toggle("投放到首个空位", spawnAtFirstEmpty);
            using (new EditorGUI.DisabledScope(spawnAtFirstEmpty))
            {
                spawnAnchorX = EditorGUILayout.IntField("锚点 X", spawnAnchorX);
                spawnAnchorY = EditorGUILayout.IntField("锚点 Y", spawnAnchorY);
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("投放"))
                    SpawnSelectedItem();

                if (GUILayout.Button("清空容器"))
                {
                    selectedContainer.ClearAllItems();
                    statusMessage = $"已清空 {selectedContainer.ContainerName}";
                }
            }
        }

        /// <summary>
        /// Loot 配置投放区
        /// </summary>
        private void DrawLootPanel()
        {
            EditorGUILayout.LabelField("随机投放", EditorStyles.boldLabel);

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("随机投放需在 Play 模式下使用", MessageType.Warning);
                return;
            }

            if (!selectedContainer.IsInventoryReady)
            {
                EditorGUILayout.HelpBox("容器逻辑尚未初始化", MessageType.Warning);
                return;
            }

            var binder = selectedContainer.GetComponent<ContainerLootBinder>();
            if (binder is null)
            {
                EditorGUILayout.HelpBox(
                    "选中容器缺少 ContainerLootBinder 可一键挂载后在此配置",
                    MessageType.Warning);
                if (GUILayout.Button("挂载 ContainerLootBinder"))
                {
                    Undo.AddComponent<ContainerLootBinder>(selectedContainer.gameObject);
                    statusMessage = $"已挂载 ContainerLootBinder {selectedContainer.ContainerName}";
                }

                return;
            }

            var binderSo = new SerializedObject(binder);
            binderSo.Update();
            EditorGUI.BeginChangeCheck();
            DrawBinderContentIdField(binderSo);
            EditorGUILayout.PropertyField(binderSo.FindProperty("grade"), new GUIContent("密度等级"));
            EditorGUILayout.PropertyField(binderSo.FindProperty("alreadyLooted"), new GUIContent("已搜过"));
            if (EditorGUI.EndChangeCheck())
            {
                binderSo.ApplyModifiedProperties();
                EditorUtility.SetDirty(binder);
            }
            else
            {
                binderSo.ApplyModifiedProperties();
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("确保已投放"))
                {
                    bool ok = binder.EnsureLooted();
                    statusMessage = ok
                        ? $"确保已投放完成 {selectedContainer.ContainerName} 已搜过={binder.AlreadyLooted}"
                        : $"确保已投放失败 {selectedContainer.ContainerName}";
                }

                if (GUILayout.Button("按配置强制重投"))
                {
                    try
                    {
                        var result = binder.ForceRefill();
                        statusMessage =
                            $"强制重投并打开揭幕 {selectedContainer.ContainerName} " +
                            $"空箱={result.WasEmptyRoll} 候选={result.CandidateCount} " +
                            $"放入={result.PlacedCount} 跳过={result.SkippedCount}";
                    }
                    catch (System.Exception ex)
                    {
                        statusMessage = $"强制重投异常 {ex.Message}";
                        Debug.LogException(ex);
                    }
                }
            }

            if (GUILayout.Button("重置已搜过标记"))
            {
                binder.ResetLootedFlag();
                statusMessage = $"已重置标记 {selectedContainer.ContainerName}";
            }
        }

        /// <summary>
        /// 内容方向 优先从配置表下拉
        /// </summary>
        private static void DrawBinderContentIdField(SerializedObject binderSo)
        {
            var contentIdProperty = binderSo.FindProperty("contentId");
            if (contentIdProperty is null)
                return;

            var configSo = LootSimConfigSo.EnsureLoaded();
            var contentTableList = configSo != null ? configSo.ContentTableList : null;
            if (contentTableList is null || contentTableList.Count == 0)
            {
                EditorGUILayout.PropertyField(contentIdProperty, new GUIContent("内容方向"));
                return;
            }

            var labelList = new List<string>(contentTableList.Count);
            int selectedIndex = 0;
            string currentId = contentIdProperty.stringValue;
            for (int i = 0; i < contentTableList.Count; i++)
            {
                var table = contentTableList[i];
                string contentId = table != null ? table.ContentId : string.Empty;
                labelList.Add(string.IsNullOrEmpty(contentId) ? $"(空_{i})" : contentId);
                if (contentId == currentId)
                    selectedIndex = i;
            }

            int newIndex = EditorGUILayout.Popup("内容方向", selectedIndex, labelList.ToArray());
            if (newIndex >= 0 && newIndex < contentTableList.Count)
            {
                var table = contentTableList[newIndex];
                contentIdProperty.stringValue = table != null ? table.ContentId : string.Empty;
            }
        }

        /// <summary>
        /// GM 存读档区
        /// </summary>
        private void DrawPersistPanel()
        {
            EditorGUILayout.LabelField("存档", EditorStyles.boldLabel);

            int containerId = selectedContainer.ContainerId;
            string savePath = ResolveSaveFilePath(containerId);
            bool hasSaveFile = containerId > 0 && GridInventoryService.HasSaveFile(containerId);

            EditorGUILayout.LabelField("路径", savePath, EditorStyles.wordWrappedMiniLabel);
            EditorGUILayout.LabelField("状态", hasSaveFile ? "已有存档" : "无存档");

            if (containerId <= 0)
            {
                EditorGUILayout.HelpBox("containerId 需大于 0 才能存读档", MessageType.Warning);
                return;
            }

            if (!Application.isPlaying)
            {
                EditorGUILayout.HelpBox("存读档需在 Play 模式下使用", MessageType.Warning);

                using (new EditorGUI.DisabledScope(!hasSaveFile))
                {
                    if (GUILayout.Button("删除存档文件"))
                        DeleteSaveFile(savePath);
                }

                return;
            }

            if (!selectedContainer.IsInventoryReady)
            {
                EditorGUILayout.HelpBox("容器逻辑尚未初始化", MessageType.Warning);
                return;
            }

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("保存"))
                    SaveSelectedContainer();

                using (new EditorGUI.DisabledScope(!hasSaveFile))
                {
                    if (GUILayout.Button("读取"))
                        LoadSelectedContainer();
                }

                using (new EditorGUI.DisabledScope(!hasSaveFile))
                {
                    if (GUILayout.Button("删除存档"))
                        DeleteSaveFile(savePath);
                }
            }
        }

        /// <summary>
        /// 保存选中容器
        /// </summary>
        private void SaveSelectedContainer()
        {
            if (!IsContainerAlive(selectedContainer))
                return;

            bool isSuccess = selectedContainer.TrySaveToDiskForce();
            statusMessage = isSuccess
                ? $"已保存 {selectedContainer.ContainerName} -> inventory_{selectedContainer.ContainerId}.json"
                : $"保存失败 {selectedContainer.ContainerName}";
            Repaint();
        }

        /// <summary>
        /// 读取选中容器
        /// </summary>
        private void LoadSelectedContainer()
        {
            if (!IsContainerAlive(selectedContainer))
                return;

            bool isSuccess = selectedContainer.TryLoadFromDisk();
            statusMessage = isSuccess
                ? $"已读取 {selectedContainer.ContainerName} <- inventory_{selectedContainer.ContainerId}.json"
                : $"读取失败 {selectedContainer.ContainerName}";
            Repaint();
        }

        /// <summary>
        /// 删除存档文件
        /// </summary>
        private void DeleteSaveFile(string savePath)
        {
            if (string.IsNullOrEmpty(savePath) || !File.Exists(savePath))
            {
                statusMessage = "无存档可删";
                return;
            }

            if (!EditorUtility.DisplayDialog("删除存档", $"确认删除\n{savePath}", "删除", "取消"))
                return;

            File.Delete(savePath);
            statusMessage = $"已删除 {savePath}";
            Repaint();
        }

        /// <summary>
        /// 解析存档路径
        /// </summary>
        private static string ResolveSaveFilePath(int containerId)
        {
            if (containerId <= 0)
                return "（未配置 containerId）";

            return Path.Combine(
                Application.persistentDataPath,
                $"inventory_{containerId}.json");
        }

        /// <summary>
        /// 容器内物品列表
        /// </summary>
        private void DrawItemListPanel()
        {
            EditorGUILayout.LabelField("容器物品", EditorStyles.boldLabel);

            if (!Application.isPlaying || !selectedContainer.IsInventoryReady)
            {
                EditorGUILayout.HelpBox("Play 模式下显示运行时物品列表", MessageType.None);
                return;
            }

            var itemViewList = selectedContainer.GetItemViewList();

            if (itemViewList.Count == 0)
            {
                EditorGUILayout.LabelField("（空）");
            }
            else
            {
                for (int i = 0; i < itemViewList.Count; i++)
                {
                    var itemView = itemViewList[i];
                    if (itemView == null || itemView.ItemData == null) continue;

                    var data = itemView.ItemData;
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        EditorGUILayout.LabelField(
                            $"[{data.ExcelItemId}] {data.AnchorPos}  {data.DataSize}  {data.InstancedItemId}");
                        if (GUILayout.Button("移除", GUILayout.Width(48f)))
                        {
                            selectedContainer.DestroyItemUI(itemView);
                            break;
                        }
                    }
                }
            }
        }

        /// <summary>
        /// 投放选中物品
        /// </summary>
        private void SpawnSelectedItem()
        {
            if (!IsContainerAlive(selectedContainer) || itemOptionList.Count == 0) return;

            int excelItemId = itemOptionList[selectedItemIndex].ExcelItemId;
            ItemView itemView;

            if (spawnAtFirstEmpty)
                itemView = selectedContainer.CreatItemUIAtFirstEmpty(excelItemId);
            else
                itemView = selectedContainer.CreatItemUI(excelItemId, new Vector2Int(spawnAnchorX, spawnAnchorY));

            statusMessage = itemView is null
                ? $"投放失败 ID:{excelItemId}"
                : $"投放成功 ID:{excelItemId} @ {itemView.ItemData.AnchorPos}";
        }

        /// <summary>
        /// 刷新容器列表
        /// </summary>
        private void RefreshContainers()
        {
            containerList.Clear();

            var registryList = GridMainContainerManager.ContainerList;
            for (int i = 0; i < registryList.Count; i++)
            {
                var container = registryList[i];
                if (IsContainerAlive(container))
                    containerList.Add(container);
            }

            if (containerList.Count == 0)
            {
                var foundList = Object.FindObjectsByType<GridContainerView>(
                    FindObjectsInactive.Include,
                    FindObjectsSortMode.None);

                for (int i = 0; i < foundList.Length; i++)
                {
                    if (IsContainerAlive(foundList[i]))
                        containerList.Add(foundList[i]);
                }
            }

            if (!IsContainerAlive(selectedContainer) || !containerList.Contains(selectedContainer))
                selectedContainer = containerList.Count > 0 ? containerList[0] : null;

            statusMessage = $"容器数量 {containerList.Count}";
        }

        /// <summary>
        /// 刷新配表选项
        /// </summary>
        private void RefreshItemOptions()
        {
            itemOptionList.Clear();

            var listSo = ItemDataEditorSession.LoadListSo();
            if (listSo is null)
            {
                itemLabels = System.Array.Empty<string>();
                return;
            }

            var dataList = listSo.ItemDataList;
            var labelList = new List<string>(dataList.Count);
            for (int i = 0; i < dataList.Count; i++)
            {
                var data = dataList[i];
                itemOptionList.Add(data);
                labelList.Add($"[{data.ExcelItemId}] {data.Name}  {data.DataSize}");
            }

            itemLabels = labelList.ToArray();
            selectedItemIndex = Mathf.Clamp(selectedItemIndex, 0, Mathf.Max(0, itemLabels.Length - 1));
        }
    }
}
#endif
