using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 双击快捷互转 AB 容器
    /// </summary>
    public partial class GridContainerView
    {
        /// <summary>
        /// 双击物品快捷转移到对面容器
        /// </summary>
        /// <param name="itemView">被双击物品</param>
        public void TryQuickTransferItem(ItemView itemView)
        {
            if (itemView is null || itemView.ItemData is null || !IsInventoryReady)
            {
                GridInventoryEvents.RaiseQuickTransferFailed(itemView?.ItemData, EQuickMoveFailReason.ItemInvalid);
                return;
            }

            if (containerRole == EGridContainerRole.Neutral)
            {
                GridInventoryEvents.RaiseQuickTransferFailed(itemView.ItemData, EQuickMoveFailReason.InvalidContainerRole);
                return;
            }

            if (!GridMainContainerManager.TryResolveQuickTransferTarget(this, out var targetContainer))
            {
                GridInventoryEvents.RaiseQuickTransferFailed(itemView.ItemData, EQuickMoveFailReason.NoTargetContainer);
                return;
            }

            var result = gridInventoryService.TryQuickMoveTo(targetContainer.InventoryService, itemView.ItemData);
            if (!result.IsSuccess)
            {
                GridInventoryEvents.RaiseQuickTransferFailed(itemView.ItemData, EQuickMoveFailReason.TargetFull);
                return;
            }

            ApplyQuickMoveView(itemView, targetContainer, result);
        }

        /// <summary>
        /// 快捷互转成功后同步 ItemView
        /// </summary>
        private void ApplyQuickMoveView(ItemView itemView,
                                        GridContainerView targetContainer,
                                        QuickMoveOpResult result)
        {
            switch (result.Kind)
            {
                case EQuickMoveResultKind.StackedFull:
                    // 数据层已在堆叠满时 RemoveAt 此处只清视图
                    itemViewDict.Remove(itemView.ItemData.InstancedItemId);
                    Destroy(itemView.gameObject);
                    break;

                case EQuickMoveResultKind.StackedPartial:
                    // 源堆仍在 数据层数量已变 视图暂保留在源容器
                    break;

                case EQuickMoveResultKind.Moved:
                    RemoveItemView(itemView);
                    targetContainer.ReceiveQuickMovedItemView(itemView);
                    break;
            }
        }

        /// <summary>
        /// 接收从对面容器快捷移入的物品视图
        /// </summary>
        /// <param name="itemView">物品视图</param>
        internal void ReceiveQuickMovedItemView(ItemView itemView)
        {
            if (itemView?.ItemData is null)
                return;

            AddItemView(itemView);
            var itemData = itemView.ItemData;
            itemView.ItemRectTransform.localRotation =
                Quaternion.Euler(0, 0, itemData.IsRotated ? 90f : 0f);
            itemView.ItemRectTransform.localPosition =
                GetItemUIPivotPos(itemData.AnchorPos, itemData.DataSize);
        }
    }
}
