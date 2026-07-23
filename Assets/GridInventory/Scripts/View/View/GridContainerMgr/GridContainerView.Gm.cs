using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 此脚本放置了GM相关的API 用于编辑器下GM投放物品
    /// </summary>
    public partial class GridContainerView
    {
        /// <summary> 逻辑服务是否已初始化 </summary>
        public bool IsInventoryReady => gridInventoryService is not null;

        /// <summary> 容器显示名 有名称栏外壳时取其 Name </summary>
        public string ContainerName
        {
            get
            {
                var nameBar = GetComponentInParent<GridContainerNameBar>();
                if (nameBar != null)
                    return nameBar.DisplayName;
                return gameObject.name;
            }
        }

        void OnEnable()
        {
            GridMainContainerManager.Register(this);
        }

        void OnDisable()
        {
            GridMainContainerManager.Unregister(this);
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
            return CreatItemUIAtFirstEmpty(excelItemId, 1);
        }

        /// <summary>
        /// 投放到首个可放置空位 指定堆叠数
        /// </summary>
        public ItemView CreatItemUIAtFirstEmpty(int excelItemId, int stackCount)
        {
            var itemRtData = gridInventoryService.CreatItemAtFirstEmpty(excelItemId, stackCount);
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
        /// 投放到随机可放置空位 指定堆叠数
        /// </summary>
        public ItemView CreatItemUIAtRandomEmpty(int excelItemId, int stackCount)
        {
            var itemRtData = gridInventoryService.CreatItemAtRandomEmpty(excelItemId, stackCount);
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
