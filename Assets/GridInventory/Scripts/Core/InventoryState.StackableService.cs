using System;

namespace MmInventory
{
    public partial class InventoryState
    {
        private sealed class InventoryStackableService
        {
            /// <summary> 背包状态引用 </summary>
            private readonly InventoryState inventoryState;

            /// <summary>
            /// 初始化堆叠服务
            /// </summary>
            public InventoryStackableService(InventoryState inventoryState)
            {
                this.inventoryState = inventoryState;
            }

            /// <summary>
            /// 检查是否可堆叠
            /// </summary>
            public bool CanStack(IItemRuntime dragItem, IItemRuntime targetItem)
            {
                if (dragItem is null || targetItem is null) return false;
                if (dragItem.InstancedItemId == targetItem.InstancedItemId) return false;
                if (dragItem.ItemStackType != targetItem.ItemStackType) return false;
                if (dragItem.ExcelItemId != targetItem.ExcelItemId) return false;

                int space = targetItem.MaxStackCount - targetItem.CurrStackCount;
                return space > 0;
            }

            /// <summary>
            /// 尝试堆叠 拖动物合并到目标物
            /// </summary>
            public bool TryStack(IItemRuntime dragItem, IItemRuntime targetItem)
            {
                if (!CanStack(dragItem, targetItem)) return false;

                int space = targetItem.MaxStackCount - targetItem.CurrStackCount;
                int mergeCount = Math.Min(dragItem.CurrStackCount, space);

                targetItem.CurrStackCount += mergeCount;
                dragItem.CurrStackCount -= mergeCount;
                return true;
            }

            /// <summary>
            /// 尝试拆分
            /// </summary>
            public bool TrySplit(IItemRuntime itemData, int splitCount)
            {
                if (itemData is null || splitCount <= 0) return false;
                if (itemData.CurrStackCount <= splitCount) return false;

                itemData.CurrStackCount -= splitCount;
                var newItemData = itemData.Clone(splitCount);

                // SetAtFirst 内部已同步锚点
                if (!inventoryState.SetAtFirst(newItemData, out _))
                {
                    itemData.CurrStackCount += splitCount;
                    return false;
                }

                return true;
            }
        }
    }
}
