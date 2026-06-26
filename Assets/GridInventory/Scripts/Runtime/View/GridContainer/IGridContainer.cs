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
        /// 初始化组件
        /// </summary>
        void InitComponents();

        /// <summary>
        /// 视图更新
        /// </summary>
        void ViewUpdate();

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

        /// <summary>
        /// 尝试通过屏幕坐标获取网格坐标
        /// </summary>
        /// <param name="mousePos">屏幕坐标</param>
        /// <param name="mouseOnGridPosInt">鼠标在网格中的二维信息</param>
        /// <param name="mouseOnGridIndex">该网格的一维信息</param>
        /// <returns>是否成功</returns>
        bool TryGetMouseInGridInfo(Vector2 mousePos,
                                     out Vector2Int mouseOnGridPosInt,
                                     out int mouseOnGridIndex);
    }
}