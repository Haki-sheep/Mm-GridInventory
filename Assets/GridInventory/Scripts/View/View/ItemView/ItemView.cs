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
                            IBeginDragHandler,
                            IDragHandler,
                            IEndDragHandler
    {
        public Image itemImage;
        public RectTransform ItemRectTransform;

        [SerializeField]

        /// <summary> 配置表物品ID </summary>
        private int excelItemId;
        /// <summary> 运行时物品数据 </summary>
        public ItemRtData ItemData;
        /// <summary> 所属背包容器 </summary>
        private GridMainContainerView ownerContainer;

        // TODO: 以后可以使用事件系统来替代这些委托
        /// <summary> 鼠标进入物品 </summary>
        public Action OnMouseEnter;
        /// <summary> 物品被拿起 </summary>
        public Action OnItemPickUp;
        /// <summary> 物品被放下 </summary>
        public Action OnItemPutDown;

        /// <summary>
        /// 绑定背包容器
        /// </summary>
        public void BindOwner(GridMainContainerView owner)
        {
            ownerContainer = owner;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        public void Init()
        {
            BindViewComponents();
            if (ItemData is not null) return;

            var excelItemData = ItemRtDataMgr.Instance?.GetItemData<IItemTableData>(excelItemId);
            ItemData = ItemRtData.ItemTableData2ItemRtData(excelItemData);
        }

        /// <summary>
        /// 使用运行时数据初始化
        /// </summary>
        public void InitWithData(ItemRtData itemRtData)
        {
            BindViewComponents();
            ItemData = itemRtData;
        }

        /// <summary>
        /// 绑定视图组件
        /// </summary>
        private void BindViewComponents()
        {
            ItemRectTransform = transform as RectTransform;
            itemImage = transform.Find("Icon").GetComponent<Image>();
        }

        /// <summary>
        /// 鼠标按下物品
        /// </summary>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
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
            ownerContainer?.OnEndDrag(eventData);
            OnItemPutDown?.Invoke();
        }
    }
}
