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
        [Header("组件")]
        public Image itemImage;
        public RectTransform ItemRectTransform;

        [Header("数据")]
        [SerializeField]
        private int itemId;
        public ItemRtData ItemData;

        /// <summary> 所属背包容器 </summary>
        private GridMainContainerView ownerContainer;

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
            if (ItemData is not null) return;

            var excelItemData = ItemRtDataMgr.Instance?.GetItemData<IItemBaseData>(itemId);
            ItemData = ItemRtData.FromConfig(excelItemData);

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
        }
    }
}
