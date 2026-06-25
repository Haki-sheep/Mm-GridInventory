using System;
using UnityEngine;using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 此脚本挂载于具体物品上 传递鼠标操作事件 
    /// </summary>
    public class ItemView : MonoBehaviour,
                            IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler
                        
    {
        [Header("组件")]
        public Image itemImage;
        public RectTransform ItemRectTransform;
        public IGridAudioAndAnimation IGridAudioAndAnimation;

        [Header("数据")]
        [SerializeField] private int itemId;
        public ItemRtData ItemData;

        public event Action<ItemView> OnPointerDownEvent;
        public event Action<ItemView> OnPointerEnterEvent;
        public event Action<ItemView> OnPointerExitEvent;

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="iGridAudioAndAnimation"></param>
        public void Init(IGridAudioAndAnimation iGridAudioAndAnimation)
        {
            if (ItemData is not null) return;

            // 数据
            var excelItemData = ItemRtDataMgr.Instance?.GetItemData<IItemBaseData>(itemId);
            ItemData = ItemRtData.FromConfig(excelItemData);

            // 组件
            ItemRectTransform = this.transform as RectTransform;
            itemImage = this.transform.Find("Icon").GetComponent<Image>();
            this.IGridAudioAndAnimation = iGridAudioAndAnimation;
        }

        /// <summary>
        /// 鼠标按下物品
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerDown(PointerEventData eventData)
        {
            if (eventData.button != PointerEventData.InputButton.Left) return;
            OnPointerDownEvent?.Invoke(this);   
        }

        /// <summary>
        /// 鼠标进入物品
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerEnter(PointerEventData eventData)
        {
            OnPointerEnterEvent?.Invoke(this);
            IGridAudioAndAnimation?.OnMouseEnterItem();
        }

        /// <summary>
        /// 鼠标离开物品
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerExit(PointerEventData eventData)
        {
            OnPointerExitEvent?.Invoke(this);
            IGridAudioAndAnimation?.OnMouseExitItem();
        }
    }
}