#if UNITY_EDITOR
using System.Collections.Generic;
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

        private readonly List<GridMainContainerView> containerList = new();
        private readonly List<ItemBaseData> itemOptionList = new();

        private GridMainContainerView selectedContainer;
        private Vector2 containerScrollPos;
        private Vector2 itemScrollPos;
        private int selectedItemIndex;
        private int spawnAnchorX;
        private int spawnAnchorY;
        private bool spawnAtFirstEmpty;
        private string statusMessage = "就绪";

        private string[] itemLabels = System.Array.Empty<string>();

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
            RefreshContainers();
            RefreshItemOptions();
        }

        private void OnGUI()
        {
            DrawToolbar();

            using (new EditorGUILayout.HorizontalScope())
            {
                DrawContainerPanel();
                DrawGmPanel();
            }
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
                    if (container is null) continue;

                    bool isSelected = selectedContainer == container;
                    string label = $"{container.ContainerName}  {container.gridRowAndCloumns.x}x{container.gridRowAndCloumns.y}";
                    if (GUILayout.Toggle(isSelected, label, "Button"))
                    {
                        selectedContainer = container;
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
            using (new EditorGUILayout.VerticalScope())
            {
                if (selectedContainer is null)
                {
                    EditorGUILayout.HelpBox("请选择一个背包容器", MessageType.Info);
                    return;
                }

                EditorGUILayout.LabelField("容器信息", EditorStyles.boldLabel);
                EditorGUILayout.LabelField("名称", selectedContainer.ContainerName);
                EditorGUILayout.LabelField("行列", selectedContainer.gridRowAndCloumns.ToString());
                EditorGUILayout.LabelField("运行状态", Application.isPlaying ? "Play" : "Edit");

                EditorGUILayout.Space(8f);
                DrawSpawnPanel();
                EditorGUILayout.Space(8f);
                DrawItemListPanel();
            }
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
            itemScrollPos = EditorGUILayout.BeginScrollView(itemScrollPos, GUILayout.MinHeight(160f));

            if (itemViewList.Count == 0)
            {
                EditorGUILayout.LabelField("（空）");
            }
            else
            {
                for (int i = 0; i < itemViewList.Count; i++)
                {
                    var itemView = itemViewList[i];
                    if (itemView is null || itemView.ItemData is null) continue;

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

            EditorGUILayout.EndScrollView();
        }

        /// <summary>
        /// 投放选中物品
        /// </summary>
        private void SpawnSelectedItem()
        {
            if (selectedContainer is null || itemOptionList.Count == 0) return;

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

            var registryList = GridMainContainerRegistry.Containers;
            for (int i = 0; i < registryList.Count; i++)
            {
                if (registryList[i] is not null)
                    containerList.Add(registryList[i]);
            }

            if (containerList.Count == 0)
            {
                var foundList = Object.FindObjectsByType<GridMainContainerView>(FindObjectsInactive.Include, FindObjectsSortMode.None);
                containerList.AddRange(foundList);
            }

            if (selectedContainer is not null && !containerList.Contains(selectedContainer))
                selectedContainer = containerList.Count > 0 ? containerList[0] : null;
            else if (selectedContainer is null && containerList.Count > 0)
                selectedContainer = containerList[0];

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
