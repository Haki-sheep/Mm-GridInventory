using System.Collections.Generic;

namespace MmInventory
{
    /// <summary>
    /// 场景背包容器注册表
    /// </summary>
    public static class GridMainContainerRegistry
    {
        private static readonly List<GridMainContainerView> containerList = new();

        public static IReadOnlyList<GridMainContainerView> Containers => containerList;

        /// <summary>
        /// 注册背包容器
        /// </summary>
        public static void Register(GridMainContainerView container)
        {
            if (container is null || containerList.Contains(container)) return;
            containerList.Add(container);
        }

        /// <summary>
        /// 注销背包容器
        /// </summary>
        public static void Unregister(GridMainContainerView container)
        {
            if (container is null) return;
            containerList.Remove(container);
        }
    }
}
