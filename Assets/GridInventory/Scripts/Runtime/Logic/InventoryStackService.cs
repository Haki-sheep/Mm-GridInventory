namespace MmInventory
{
    /// <summary>
    /// 堆叠判定 依赖配置层 不放在 Core
    /// </summary>
    public static class InventoryStackService
    {
        /// <summary>
        /// 堆叠判定
        /// </summary>
        public static bool CanStack(ItemRtData aItemData,
                                    ItemRtData bItemData,
                                    out int remainingCount)
        {
            remainingCount = 0;

            if (aItemData is null || bItemData is null) return false;
            if (aItemData.PersistenceItemId != bItemData.PersistenceItemId)
                return false;

            var a = ItemRtDataMgr.Instance.GetItemData<IItemRootData>(aItemData.PersistenceItemId);
            var b = ItemRtDataMgr.Instance.GetItemData<IItemRootData>(bItemData.PersistenceItemId);
            if (a.ItemStackType is EItemStackType.Single || b.ItemStackType is EItemStackType.Single)
                return false;

            remainingCount = b.MaxStackCount - (aItemData.CurStackCount + bItemData.CurStackCount);
            return true;
        }
    }
}
