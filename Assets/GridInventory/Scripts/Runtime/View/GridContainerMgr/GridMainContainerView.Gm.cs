using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    public partial class GridMainContainerView
    {
        /// <summary> 逻辑服务是否已初始化 </summary>
        public bool IsInventoryReady => gridInventoryService is not null;

        /// <summary> 容器显示名 </summary>
        public string ContainerName => gameObject.name;

        void OnEnable()
        {
            GridMainContainerRegistry.Register(this);
        }

        void OnDisable()
        {
            GridMainContainerRegistry.Unregister(this);
        }

        /// <summary>
        /// 获取当前容器内所有物品视图
        /// </summary>
        public List<ItemView> GetItemViewList()
        {
            return new List<ItemView>(itemViewDict.Values);
        }

        /// <summary>
        /// 投放到首个可放置空位
        /// </summary>
        public ItemView CreatItemUIAtFirstEmpty(int excelItemId)
        {
            var itemRtData = gridInventoryService.CreatItemAtFirstEmpty(excelItemId);
            if (itemRtData is null) return null;

            var itemView = SpawnItemView(itemRtData);
            if (itemView is null)
            {
                gridInventoryService.TryRemoveItem(itemRtData.AnchorPos);
                return null;
            }

            itemView.ItemRectTransform.localPosition =
                GetItemUIPivotPos(itemRtData.AnchorPos, itemRtData.DataSize);
            return itemView;
        }

        /// <summary>
        /// 清空容器内全部物品
        /// </summary>
        public void ClearAllItems()
        {
            var itemViewList = GetItemViewList();
            for (int i = 0; i < itemViewList.Count; i++)
                DestroyItemUI(itemViewList[i]);
        }
    }
}
