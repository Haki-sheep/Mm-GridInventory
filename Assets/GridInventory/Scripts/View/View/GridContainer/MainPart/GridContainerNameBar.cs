#if UNITY_EDITOR
using UnityEditor;
#endif
using Sirenix.OdinInspector;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 带名称栏的网格容器外壳
    /// 挂在 NameBk 与内层 GridContainer 的公共父节点上
    /// </summary>
    [DisallowMultipleComponent]
    public class GridContainerNameBar : MonoBehaviour
    {
        private const int DefaultNameBarHeight = 100;
        private const int DefaultNameIconSize = 100;

        /// <summary> 内层网格容器 </summary>
        [SerializeField]
        [LabelText("网格容器")]
        [Required]
        private GridContainerView gridContainerView;

        /// <summary> 名称栏根节点 </summary>
        [SerializeField]
        [LabelText("NameBk")]
        [Required]
        private RectTransform nameBkRect;

        /// <summary> 容器图标 </summary>
        [SerializeField]
        [LabelText("ContainerIcon")]
        private Image containerIconImage;

        /// <summary> 容器名称文本 </summary>
        [SerializeField]
        [LabelText("ContainerName")]
        private TextMeshProUGUI containerNameText;

        /// <summary> 容器显示名 </summary>
        [SerializeField]
        [LabelText("Name")]
        private string containerDisplayName;

        /// <summary> 名称栏高度 </summary>
        [SerializeField]
        [LabelText("名称栏高度")]
        private int nameBarHeight = DefaultNameBarHeight;

        /// <summary> 图标边长 </summary>
        [SerializeField]
        [LabelText("图标尺寸")]
        private int nameIconSize = DefaultNameIconSize;

        /// <summary> 显示名 </summary>
        public string DisplayName
        {
            get
            {
                if (!string.IsNullOrWhiteSpace(containerDisplayName))
                    return containerDisplayName.Trim();
                if (gridContainerView != null)
                    return gridContainerView.name;
                return gameObject.name;
            }
        }

        /// <summary> 绑定的网格容器 </summary>
        public GridContainerView GridContainerView => gridContainerView;

#if UNITY_EDITOR
        /// <summary>
        /// 先重建内层格子再同步外壳
        /// </summary>
        [Button("创建格子并同步外壳", ButtonSizes.Large)]
        [GUIColor(0.35f, 0.78f, 0.45f)]
        private void CreatItemCellsAndSync()
        {
            EnsureRefs();
            if (gridContainerView == null)
            {
                Debug.LogWarning("GridContainerNameBar: 未绑定 GridContainerView");
                return;
            }

            gridContainerView.CreatItemCells();
            SyncLayout();
        }
#endif

        /// <summary>
        /// 按内层容器尺寸同步名称栏与外壳宽高
        /// </summary>
        [Button("同步名称栏布局", ButtonSizes.Medium)]
        public void SyncLayout()
        {
            EnsureRefs();
            if (gridContainerView == null || nameBkRect == null)
            {
                Debug.LogWarning("GridContainerNameBar: 缺少 GridContainerView 或 NameBk");
                return;
            }

            int cellSize = Mathf.Max(1, gridContainerView.GridCellSize);
            var gridRowAndColumn = gridContainerView.gridRowAndCloumns;
            int visibleHeight = Mathf.Max(1, gridContainerView.VisibleHeight);
            int safeNameBarHeight = Mathf.Max(1, nameBarHeight);
            int safeIconSize = Mathf.Max(1, nameIconSize);

            float gridWidth = cellSize * Mathf.Max(1, gridRowAndColumn.x);
            float rootHeight = safeNameBarHeight + visibleHeight;

            var rootRect = transform as RectTransform;
            rootRect.sizeDelta = new Vector2(gridWidth, rootHeight);

            // NameBk 顶对齐
            nameBkRect.anchorMin = new Vector2(0.5f, 1f);
            nameBkRect.anchorMax = new Vector2(0.5f, 1f);
            nameBkRect.pivot = new Vector2(0.5f, 0.5f);
            nameBkRect.sizeDelta = new Vector2(gridWidth, safeNameBarHeight);
            nameBkRect.anchoredPosition = new Vector2(0f, -safeNameBarHeight * 0.5f);
            nameBkRect.gameObject.SetActive(true);

            if (containerIconImage != null)
            {
                var iconRect = containerIconImage.rectTransform;
                iconRect.anchorMin = new Vector2(0f, 0.5f);
                iconRect.anchorMax = new Vector2(0f, 0.5f);
                iconRect.pivot = new Vector2(0.5f, 0.5f);
                iconRect.sizeDelta = new Vector2(safeIconSize, safeIconSize);
                iconRect.anchoredPosition = new Vector2(safeIconSize * 0.5f, 0f);
            }

            if (containerNameText != null)
            {
                SyncNameTextRect(safeIconSize);
                containerNameText.text =
                    $"{DisplayName} {gridRowAndColumn.x} * {gridRowAndColumn.y}";
            }

            // 内层容器底对齐
            var containerRect = gridContainerView.transform as RectTransform;
            containerRect.anchorMin = new Vector2(0.5f, 0f);
            containerRect.anchorMax = new Vector2(0.5f, 0f);
            containerRect.pivot = new Vector2(0.5f, 0.5f);
            containerRect.sizeDelta = new Vector2(gridWidth, visibleHeight);
            containerRect.anchoredPosition = new Vector2(0f, visibleHeight * 0.5f);

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(this);
                EditorUtility.SetDirty(rootRect);
                EditorUtility.SetDirty(nameBkRect);
                EditorUtility.SetDirty(containerRect);
                if (containerIconImage != null)
                    EditorUtility.SetDirty(containerIconImage.rectTransform);
                if (containerNameText != null)
                    EditorUtility.SetDirty(containerNameText.rectTransform);
            }
#endif
        }

        /// <summary>
        /// 名称文本占满图标右侧剩余宽度
        /// </summary>
        private void SyncNameTextRect(int iconSize)
        {
            var nameRect = containerNameText.rectTransform;
            nameRect.anchorMin = Vector2.zero;
            nameRect.anchorMax = Vector2.one;
            nameRect.pivot = new Vector2(0.5f, 0.5f);
            // 左侧让出图标宽度 其余贴齐 NameBk
            nameRect.offsetMin = new Vector2(iconSize, 0f);
            nameRect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 内层 CreatItemCells 完成后回调
        /// </summary>
        public void SyncAfterContainerBuilt()
        {
            SyncLayout();
        }

        /// <summary>
        /// 自动补齐引用
        /// </summary>
        private void EnsureRefs()
        {
            if (gridContainerView == null)
                gridContainerView = GetComponentInChildren<GridContainerView>(true);

            if (nameBkRect == null)
            {
                var rectList = GetComponentsInChildren<RectTransform>(true);
                for (int i = 0; i < rectList.Length; i++)
                {
                    if (rectList[i].name == "NameBk")
                    {
                        nameBkRect = rectList[i];
                        break;
                    }
                }
            }

            if (nameBkRect == null)
                return;

            if (containerIconImage == null)
            {
                var iconTransform = nameBkRect.Find("ContainerIcon");
                if (iconTransform != null)
                    containerIconImage = iconTransform.GetComponent<Image>();
            }

            if (containerNameText == null)
            {
                var nameTransform = nameBkRect.Find("ContainerName");
                if (nameTransform != null)
                    containerNameText = nameTransform.GetComponent<TextMeshProUGUI>();
            }
        }

        private void OnValidate()
        {
            EnsureRefs();
        }
    }
}
