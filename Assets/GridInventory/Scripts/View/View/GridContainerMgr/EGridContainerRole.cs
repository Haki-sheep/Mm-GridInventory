namespace MmInventory
{
    /// <summary>
    /// 背包容器在 UI 会话中的角色
    /// </summary>
    public enum EGridContainerRole
    {
        /// <summary> 不参与 AB 快捷互转 </summary>
        Neutral,

        /// <summary> 玩家常驻背包 </summary>
        Persistent,

        /// <summary> 当前活跃容器 如战利品箱 </summary>
        Active,
    }
}
