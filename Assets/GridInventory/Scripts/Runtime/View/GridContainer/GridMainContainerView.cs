using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif


namespace MmInventory
{
    /// <summary>
    /// 背包主容器视图 
    /// 用于初始化背包格子 提供View层组件给Operate脚本使用
    /// </summary>
    public partial class GridMainContainerView : SerializedMonoBehaviour, IGridContainer
    {

        #region 字段与属性
        /// <summary>网格内容容器 用于坐标转换</summary>
        private RectTransform gridContentCache;
        private RectTransform gridContent => gridContentCache ??= transform.Find("Viewport/GridContent") as RectTransform;

        /// <summary>网格内容布局组 用于提供格子Size等基础参数</summary>
        private GridLayoutGroup contentLayoutGroupCache;
        private GridLayoutGroup contentLayoutGroup => contentLayoutGroupCache ??= gridContent.GetComponent<GridLayoutGroup>();

        /// <summary>物品内容容器 用于承载物品</summary>
        private RectTransform itemContentCache;
        private RectTransform itemContent => itemContentCache ??= transform.Find("Viewport/ItemContent") as RectTransform;

        [Header("自定义View组件")]
        private GridInventoryService gridInventoryService;

        private FrameBoardView frameBoardViewCache;
        private FrameBoardView frameBoardView => frameBoardViewCache ??= itemContent.Find("frameBoard")?.GetComponent<FrameBoardView>();

        private ItemView[] itemViewsCache;
        private ItemView[] itemViews => itemViewsCache ??= itemContent.GetComponentsInChildren<ItemView>();


        [Header("配置信息")]
        [SerializeField]
        private GameObject CellPrefab;
        public const int gridSize = 100;
        public const int spacing = 0;
        public Vector2Int gridRowAndCloumns = Vector2Int.zero;

        // 下列组件皆为自动获取
        /// <summary>滚动区域 用于拖拽物品时临时禁用不然拖拽物品过程中会触发父级视图滚动</summary>
        private ScrollRect scrollRectCache;
        private ScrollRect scrollRect => scrollRectCache ??= GetComponentInParent<ScrollRect>();

        /// <summary>容器RectTransform 用于设置整体容器大小</summary>
        private RectTransform containertRectTransformCache;
        private RectTransform containertRectTransform => containertRectTransformCache ??= scrollRect.content as RectTransform;

        /// <summary>Canvas 用于保证网格坐标计算的正确性</summary>
        private Canvas canvasCache;
        private Canvas canvas => canvasCache ??= GetComponentInParent<Canvas>();

        private Camera canvasCameraCache;
        private Camera canvasCamera => canvasCameraCache ??= canvas.worldCamera;

        [Header("Debug")]
        [SerializeField, ReadOnly]
        /// <summary>网格格子视图 用于显示网格格子</summary>
        private GridCellView[] gridCellViews;

        [DictionaryDrawerSettings(KeyLabel = "物品实例ID", ValueLabel = "物品视图")]
        [SerializeField]
        private Dictionary<string, ItemView> itemViewDict = new();
        #endregion

        #region 生命周期

        void Start()
        {
            gridInventoryService = new GridInventoryService();
            gridInventoryService.Init(gridRowAndCloumns);
            RegisterSceneItemViews();
        }

        void Update()
        {
            HandleDraggingItemRotation();
        }

        public void OnBeginDrag(ItemView itemView, PointerEventData eventData)
        {
            isDragging = true;
            BeginDragHandler(itemView, eventData);
        }

        public void OnDragging(PointerEventData eventData)
        {
              if (!isDragging) return;
            DraggingHandler(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
              if (!isDragging) return;
            isDragging = false;
            EndDragHandler(eventData);
        }

        #endregion
    }
}
