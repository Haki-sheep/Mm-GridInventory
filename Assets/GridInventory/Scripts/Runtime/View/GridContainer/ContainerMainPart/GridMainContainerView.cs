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
    public partial class GridMainContainerView : SerializedMonoBehaviour, IGridContainer
    {

        #region 字段与属性
        /// <summary>滚动内容壳层 ScrollRect.content </summary>
        private RectTransform scrollContentCache;
        private RectTransform scrollContent => scrollContentCache ??= ScrollRect.content;

        /// <summary>网格内容容器 用于坐标转换</summary>
        private RectTransform gridContentCache;
        private RectTransform gridContent => gridContentCache ??= FindChildRectTransform(scrollContent, "GridContent");

        /// <summary>网格内容布局组 用于提供格子Size等基础参数</summary>
        private GridLayoutGroup contentLayoutGroupCache;
        private GridLayoutGroup contentLayoutGroup => contentLayoutGroupCache ??= gridContent.GetComponent<GridLayoutGroup>();

        /// <summary>物品内容容器 用于承载物品</summary>
        private RectTransform itemContentCache;
        private RectTransform itemContent => itemContentCache ??= FindChildRectTransform(scrollContent, "ItemContent");

        [Header("自定义View组件")]
        private GridInventoryService gridInventoryService;

        private ItemView[] itemViewsCache;
        private ItemView[] itemViews => itemViewsCache ??= itemContent.GetComponentsInChildren<ItemView>();


        [Header("配置信息")]
        [SerializeField]
        private GameObject CellPrefab;
        public const int gridSize = 100;
        public const int spacing = 0;
        public Vector2Int gridRowAndCloumns = Vector2Int.zero;

        /// <summary> 可视高度 父容器显示区域高度 </summary>
        [SerializeField]
        private int visibleHeight = 700;

        // 下列组件皆为自动获取
        /// <summary>滚动区域 用于拖拽物品时临时禁用不然拖拽物品过程中会触发父级视图滚动</summary>
        private ScrollRect scrollRectCache;
        private ScrollRect ScrollRect => scrollRectCache ??= GetComponent<ScrollRect>();

        /// <summary>容器RectTransform 用于设置整体容器大小</summary>
        private RectTransform containertRectTransformCache;
        private RectTransform ContainertRectTransform => containertRectTransformCache ??= ScrollRect.content as RectTransform;

        /// <summary>Canvas 用于保证网格坐标计算的正确性</summary>
        private Canvas canvasCache;
        private Canvas Canvas => canvasCache ??= GetComponentInParent<Canvas>();

        private Camera canvasCameraCache;
        private Camera CanvasCamera => canvasCameraCache ??= Canvas.worldCamera;
 

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
