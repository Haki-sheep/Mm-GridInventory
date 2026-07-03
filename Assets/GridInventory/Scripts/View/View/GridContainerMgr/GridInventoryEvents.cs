using System;

namespace MmInventory
{
    /// <summary>
    /// 背包 UI 事件
    /// </summary>
    public static class GridInventoryEvents
    {
        /// <summary> 快捷互转失败 </summary>
        public static event Action<ItemRtData, EQuickMoveFailReason> OnQuickTransferFailed;

        /// <summary>
        /// 触发快捷互转失败事件
        /// </summary>
        public static void RaiseQuickTransferFailed(ItemRtData itemData, EQuickMoveFailReason reason)
        {
            OnQuickTransferFailed?.Invoke(itemData, reason);
        }
    }
}
