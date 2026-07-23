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
    /// 此脚本放置了基础组件和Unity生命周期以及公共接口
    /// 如果你不想知道View层的具体实现细节 直接看这个脚本提供给外部的API即可
    /// </summary>
    public partial class GridContainerView : SerializedMonoBehaviour, IGridContainer
    {

        #region 字段与属性
        /// <summary>滚动内容壳层 ScrollRect.content </summary>
        [System.NonSerialized]
        private RectTransform scrollContentCache;
        private RectTransform scrollContent
        {
            get
            {
                if (scrollContentCache == null)
                    scrollContentCache = ScrollRect != null ? ScrollRect.content : null;
                return scrollContentCache;
            }
        }

        /// <summary>网格内容容器 用于坐标转换</summary>
        [System.NonSerialized]
        private RectTransform gridContentCache;
        private RectTransform gridContent
        {
            get
            {
                if (gridContentCache == null)
                    gridContentCache = FindChildRectTransform(scrollContent, "GridContent");
                return gridContentCache;
            }
        }

        /// <summary>网格内容布局组 用于提供格子Size等基础参数</summary>
        [System.NonSerialized]
        private GridLayoutGroup contentLayoutGroupCache;
        private GridLayoutGroup contentLayoutGroup
        {
            get
            {
                if (contentLayoutGroupCache == null && gridContent != null)
                    contentLayoutGroupCache = gridContent.GetComponent<GridLayoutGroup>();
                return contentLayoutGroupCache;
            }
        }

        /// <summary>物品内容容器 用于承载物品</summary>
        [System.NonSerialized]
        private RectTransform itemContentCache;
        private RectTransform itemContent
        {
            get
            {
                if (itemContentCache == null)
                    itemContentCache = FindChildRectTransform(scrollContent, "ItemContent");
                return itemContentCache;
            }
        }

        /// <summary> 物品层 外部适配遮罩用 </summary>
        public RectTransform ItemContent => itemContent;

        [Header("自定义View组件")]
        [System.NonSerialized]
        private GridInventoryService gridInventoryService;

        /// <summary> 逻辑服务 </summary>
        internal GridInventoryService InventoryService => gridInventoryService;

        [System.NonSerialized]
        private ItemView[] itemViewsCache;
        private ItemView[] itemViews
        {
            get
            {
                if (itemViewsCache == null && itemContent != null)
                    itemViewsCache = itemContent.GetComponentsInChildren<ItemView>();
                return itemViewsCache;
            }
        }


        [Header("配置信息")]
        [SerializeField]
        private GameObject CellPrefab;

        /// <summary> 容器角色 常驻或活跃 </summary>
        [SerializeField]
        private EGridContainerRole containerRole = EGridContainerRole.Neutral;

        /// <summary> 容器角色 </summary>
        public EGridContainerRole ContainerRole => containerRole;

        /// <summary> 标准格子边长 </summary>
        [SerializeField]
        [LabelText("格子大小")]
        private int gridSize = 100;

        /// <summary> 格子间距 </summary>
        public const int spacing = 0;

        /// <summary> 标准格子边长 </summary>
        public int GridCellSize => gridSize;

        public Vector2Int gridRowAndCloumns = Vector2Int.zero;

        /// <summary> 可视高度 父容器显示区域高度 </summary>
        [SerializeField]
        private int visibleHeight = 700;

        /// <summary> 可视高度 </summary>
        public int VisibleHeight => visibleHeight;

        // 下列组件皆为自动获取
        /// <summary>滚动区域 用于拖拽物品时临时禁用不然拖拽物品过程中会触发父级视图滚动</summary>
        [System.NonSerialized]
        private ScrollRect scrollRectCache;
        private ScrollRect ScrollRect
        {
            get
            {
                if (scrollRectCache == null)
                    scrollRectCache = GetComponent<ScrollRect>();
                return scrollRectCache;
            }
        }

        /// <summary>容器RectTransform 用于设置整体容器大小</summary>
        [System.NonSerialized]
        private RectTransform containertRectTransformCache;
        private RectTransform ContainertRectTransform
        {
            get
            {
                if (containertRectTransformCache == null && ScrollRect != null)
                    containertRectTransformCache = ScrollRect.content as RectTransform;
                return containertRectTransformCache;
            }
        }

        /// <summary>Canvas 用于保证网格坐标计算的正确性</summary>
        [System.NonSerialized]
        private Canvas canvasCache;
        private Canvas Canvas
        {
            get
            {
                if (canvasCache == null)
                    canvasCache = GetComponentInParent<Canvas>();
                return canvasCache;
            }
        }

        [System.NonSerialized]
        private Camera canvasCameraCache;
        private Camera CanvasCamera
        {
            get
            {
                if (canvasCameraCache == null && Canvas != null)
                    canvasCameraCache = Canvas.worldCamera;
                return canvasCameraCache;
            }
        }
 

        #endregion

        #region 生命周期

        void Start()
        {
            gridInventoryService = new GridInventoryService();

            // 有存档则 Core 已 SetAt 直接重建 UI 否则走场景预摆物注册
            if (TryRestoreFromSaveOnStart())
                return;

            gridInventoryService.Init(gridRowAndCloumns);
            RegisterSceneItemViews();
        }

        void Update()
        {
            HandleScrollWithMouseWheel();
            HandleDraggingItemRotation();
        }

        public void OnBeginDrag(ItemView itemView, PointerEventData eventData)
        {
            if (!BeginDragHandler(itemView, eventData))
                return;
        }

        public void OnDragging(PointerEventData eventData)
        {
            if (!dragSession.IsActive) return;
            DraggingHandler(eventData);
        }

        public void OnEndDrag(PointerEventData eventData)
        {
            if (!dragSession.IsActive) return;
            EndDragHandler(eventData);
        }

        #endregion


        #region 公共接口

        /// <summary>
        /// 从配置表创建物品UI并且将其放置到网格指定锚点上
        /// </summary>
        /// <param name="excelItemId"></param>
        /// <param name="anchorPos"></param>
        public ItemView CreatItemUI(int excelItemId, Vector2Int anchorPos)
        {
            // 数据层创建并占格
            var itemRtData = gridInventoryService.CreatItem(excelItemId, anchorPos);
            if (itemRtData is null) return null;

            // 实例化物品UI并注册到字典
            var itemView = SpawnItemView(itemRtData);
            if (itemView is null)
            {
                gridInventoryService.TryRemoveItem(itemRtData.AnchorPos);
                return null;
            }

            // 设置物品UI的位置
            itemView.ItemRectTransform.localPosition =
                GetItemUIPivotPos(itemRtData.AnchorPos, itemRtData.DataSize);
            return itemView;
        }

        /// <summary>
        /// 销毁物品UI
        /// </summary>
        /// <param name="itemView"></param>
        public void DestroyItemUI(ItemView itemView)
        {
            if (itemView is null || itemView.ItemData is null) return;

            // 数据层移除
            var removeReport = gridInventoryService.TryRemoveItem(itemView.ItemData.AnchorPos);
            if (!removeReport.IsSuccess)
            {
                Debug.Log($"移除物品失败 物品ID:{itemView.ItemData.InstancedItemId}不存在");
                return;
            }

            // 移除物品UI信息到字典
            itemViewDict.Remove(itemView.ItemData.InstancedItemId);
            Destroy(itemView.gameObject);
        }

        #endregion
    }
}
