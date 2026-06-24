using System.Collections.Generic;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;


namespace MmInventory
{
    /// <summary>
    /// 背包主容器视图 
    /// 用于初始化背包格子 提供View层组件给Operate脚本使用
    /// </summary>
    public partial class GridMainContainerView : MonoBehaviour
    {

        [Header("Content系组件")]
        [SerializeField] private RectTransform gridContent;
        [SerializeField] private GridLayoutGroup contentLayoutGroup;
        [SerializeField] private RectTransform itemContent;
        [SerializeField, ReadOnly] private GridCellView[] gridCellViews;

        [Header("ScrollRect系组件")]
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform containertRectTransform;

        [Header("GridView系组件")]
        [SerializeField] private InventoryViewModel inventoryViewModel;
        [SerializeField] private FrameBoardView frameBoardView;
        [SerializeField] private InventoryItemView[] itemViews;

        [Header("Canvas")]
        [SerializeField] private Canvas canvas;
        private Camera canvasCamera;

        [Header("配置信息")]
        [SerializeField] private GameObject CellPrefab;
        public const int gridSize = 100;
        public const int spacing = 0;
        public Vector2Int gridRowAndCloumns = Vector2Int.zero;
        public TestAudioAndAnima IGridAudioAndAnimation;

        [Header("表现层物品管理容器")]
        private Dictionary<string, InventoryItemView> itemViewDict = new();

        [Header("测试信息")]
         public Test test;




        #region 表现

        #endregion

        #region 生命周期/逻辑周期
        void OnEnable()
        {
            // AddItemEventListener(testItem);
        }

        void Start()
        {
            canvas = this.transform.GetComponentInParent<Canvas>();
            canvasCamera = canvas.worldCamera;

            // Content系组件初始化
            gridContent = transform.Find("Viewport/GridContent") as RectTransform;
            if (gridContent is not null)
                contentLayoutGroup = gridContent.GetComponent<GridLayoutGroup>();
            frameBoardView = itemContent.Find("frameBoard").GetComponent<FrameBoardView>();
            if (frameBoardView is null)
            {
                Debug.LogError("GridMainContainerView: 未找到 frameBoardView，请挂在子节点或当前节点。");
                return;
            }
            gridCellViews = gridContent.GetComponentsInChildren<GridCellView>();

            // ViewModel组件初始化
            inventoryViewModel = new();
            if (inventoryViewModel is null)
            {
                Debug.LogError("GridMainContainerView: 未找到 InventoryViewModel，请挂在父节点或当前节点。");
                return;
            }
            inventoryViewModel.Init(gridRowAndCloumns);

            // ScrollRect系组件初始化
            scrollRect = GetComponentInParent<ScrollRect>();


            itemViews = itemContent.GetComponentsInChildren<InventoryItemView>();
            foreach (var itemView in itemViews)
            {
                AddItemEventListener(itemView);
            }


            // ----------------------测试---------------------------
            test.Init(this,inventoryViewModel,itemViewDict);
       
        }

        void Update()
        {
            HandleDraggingItemRotation();
        }

        void OnDisable()
        {
            // RemoveItemEventListener(testItem);
            foreach (var itemView in itemViews)
            {
                RemoveItemEventListener(itemView);
            }
        }

        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            isDragging = true;

            BeginDragHandler(eventData);
        }

        public void OnDrag(PointerEventData eventData)
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

        /// <summary>
        /// 外部调用，创建物品格子
        /// </summary>
        /// <param name="cellCount"></param>
        [Button]
        public void CreatItemCells()
        {
            // 同步格子数据
            var cellCount = gridRowAndCloumns.x * gridRowAndCloumns.y;

            // 同步ItemContent与GridContent数据
            containertRectTransform.sizeDelta = new Vector2(gridSize * gridRowAndCloumns.x, gridSize * gridRowAndCloumns.y);
            if (gridContent == null || itemContent == null) return;
            if (gridContent.parent != itemContent.parent) return;
            itemContent.anchorMin = gridContent.anchorMin;
            itemContent.anchorMax = gridContent.anchorMax;
            itemContent.pivot = gridContent.pivot;
            itemContent.localScale = gridContent.localScale;
            itemContent.localRotation = gridContent.localRotation;

            itemContent.offsetMin = gridContent.offsetMin;
            itemContent.offsetMax = gridContent.offsetMax;

            // 同步表现
            for (int i = contentLayoutGroup.transform.childCount - 1; i >= 0; i--)
            {
                GameObject.DestroyImmediate(contentLayoutGroup.transform.GetChild(i).gameObject);
            }

            if (cellCount <= 0) return;

            for (int i = 0; i < cellCount; i++)
            {
                GameObject cell = GameObject.Instantiate(CellPrefab, this.contentLayoutGroup.transform);
                cell.AddComponent<GridCellView>();
                cell.name = $"Cell_{i}";
            }
        }
    }
}
