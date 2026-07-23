using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 此脚本挂载于具体物品上 传递鼠标操作事件 
    /// </summary>
    public class ItemView : MonoBehaviour,
                            IPointerEnterHandler,
                            IPointerExitHandler,
                            IPointerDownHandler,
                            IPointerClickHandler,
                            IBeginDragHandler,
                            IDragHandler,
                            IEndDragHandler
    {
        [Header("表现配置")]
        [SerializeField]
        private Image itemImage;

        [SerializeField]
        private Image itemBackground;
        [SerializeField]
        private RectTransform itemRectTransform;

        [SerializeField]
        private float bkColorAlpha = 0.5f; 


        /// <summary> 配置表物品ID </summary>
        [SerializeField]
        [ChineseLabel("配置表物品ID")]
        private int excelItemId;

        /// <summary> 运行时物品数据 </summary>
        public ItemRtData ItemData;


        // 运行时状态
        private GridContainerView ownerContainer;
        private bool isDragging;

        // 属性
        public int ExcelItemId => ItemData != null ? ItemData.ExcelItemId : excelItemId;
        public Image ItemImage => itemImage;
        public Image ItemBackground => itemBackground;
        public RectTransform ItemRectTransform
        {
            get => itemRectTransform;
            set => itemRectTransform = value;
        }

        // TODO: 以后可以使用事件系统来替代这些委托
        /// <summary> 鼠标进入物品 </summary>
        public Action OnMouseEnter;
        /// <summary> 物品被拿起 </summary>
        public Action OnItemPickUp;
        /// <summary> 物品被放下 </summary>
        public Action OnItemPutDown;

        #region 初始化
        /// <summary>
        /// 绑定背包容器
        /// </summary>
        public void BindOwner(GridContainerView owner)
        {
            ownerContainer = owner;
        }

        /// <summary>
        /// 初始化 场景预先摆好的物品会调用此方法
        /// </summary>
        public void Init()
        {
            BindViewComponents();
            if (ItemData is not null) return;

            // 获取配置表物品数据
            var excelItemData = ItemRtDataMgr.Instance?.GetItemData<IItemTableData>(excelItemId);
            ItemData = ItemRtData.ItemTableData2ItemRtData(excelItemData);

            UpdateItemView();
        }

        /// <summary>
        /// 使用运行时数据初始化
        /// 投放物品时会调用此方法
        /// </summary>
        public void InitWithData(ItemRtData itemRtData)
        {
            BindViewComponents();
            ItemData = itemRtData;
            excelItemId = itemRtData != null ? itemRtData.ExcelItemId : 0;

            UpdateItemView();
        }


        #endregion

        #region 生命周期

        /// <summary>
        /// 鼠标按下物品
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
        }

        /// <summary>
        /// 鼠标点击物品 左键双击快捷互转 右键打开菜单
        /// </summary>
        public void OnPointerClick(PointerEventData eventData)
        {
            if (isDragging)
                return;

            if (eventData.button == PointerEventData.InputButton.Right)
            {
                ItemMenuPanel.Instance?.Show(this, eventData.position);
                return;
            }

            if (eventData.button != PointerEventData.InputButton.Left)
                return;

            if (eventData.clickCount != 2)
                return;

            ownerContainer?.TryQuickTransferItem(this);
        }

        /// <summary>
        /// 鼠标进入物品
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            OnMouseEnter?.Invoke();
        }

        /// <summary>
        /// 鼠标离开物品
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
        }

        /// <summary>
        /// 开始拖拽
        /// </summary>
        public void OnBeginDrag(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            ItemMenuPanel.Instance?.Hide();
            isDragging = true;
            ownerContainer?.OnBeginDrag(this, eventData);
            OnItemPickUp?.Invoke();
        }

        /// <summary>
        /// 拖拽中
        /// </summary>
        public void OnDrag(PointerEventData eventData)
        {
            ownerContainer?.OnDragging(eventData);
        }

        /// <summary>
        /// 结束拖拽
        /// </summary>
        public void OnEndDrag(PointerEventData eventData)
        {
            isDragging = false;
            ownerContainer?.OnEndDrag(eventData);
            OnItemPutDown?.Invoke();
        }

        #endregion

        #region 视图

        /// <summary>
        /// 绑定视图组件
        /// </summary>
        private void BindViewComponents()
        {
            itemRectTransform = transform as RectTransform;
            itemImage = transform.Find("Icon").GetComponent<Image>();
            itemBackground = transform.Find("Background").GetComponent<Image>();

            // 同步背景和Icon的尺寸 以icon为准
            itemBackground.rectTransform.sizeDelta = itemImage.rectTransform.sizeDelta;
        }

        private void UpdateItemView(){
            UpdateItemIconView();
            UpdateItemBkGroundView();
        }
        private void UpdateItemBkGroundView()
        {
            if (ItemData is null) return;

            switch (ItemData.ItemRarity)
            {
                case EItemRarity.White:
                    itemBackground.color = MakeBkColor(Color.white);
                    break;
                case EItemRarity.Blue:
                    itemBackground.color = MakeBkColor(Color.green);
                    break;
                case EItemRarity.Purple:
                    itemBackground.color = MakeBkColor(Color.purple);
                    break;
                case EItemRarity.Gold:
                    itemBackground.color = MakeBkColor(Color.orange);
                    break;
                case EItemRarity.Red:
                    itemBackground.color = MakeBkColor(Color.red);
                    break;
            }

           
        }

        private void UpdateItemIconView(){
            if (itemImage is null || ItemData is null) return;
            var tableData = ItemRtDataMgr.Instance?.GetItemData<IItemTableData>(ItemData.ExcelItemId);
            if (tableData is null) return;

            // TODO: 根据路径 使用资源加载器加载
            // itemImage.sprite = tableData.IconPath;
            // 比如下面这样子
            // itemImage.sprite = ResourceManager.Instance.Load<Sprite>(tableData.IconPath);
        }

        /// <summary>
        /// 带统一透明度的背景色
        /// </summary>
        private Color MakeBkColor(Color rgb)
        {
            rgb.a = bkColorAlpha;
            return rgb;
        }
        #endregion
    }
}
