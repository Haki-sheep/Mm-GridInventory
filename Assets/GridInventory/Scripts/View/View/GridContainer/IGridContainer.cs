using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.EventSystems;

namespace MmInventory
{
    public interface IGridContainer
    {
        /// <summary>
        /// 开始拖拽
        /// </summary>
        void OnBeginDrag(ItemView itemView, PointerEventData eventData);

        /// <summary>
        /// 拖拽中
        /// </summary>
        void OnDragging(PointerEventData eventData);

        /// <summary>
        /// 结束拖拽
        /// </summary>
        void OnEndDrag(PointerEventData eventData);

    }
}