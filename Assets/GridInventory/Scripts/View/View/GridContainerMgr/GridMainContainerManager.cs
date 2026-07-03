using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 场景内所有背包容器管理器
    /// </summary>
    public static class GridMainContainerManager
    {
        private static readonly List<GridContainerView> containerList = new();

        /// <summary> 常驻玩家背包 </summary>
        private static GridContainerView persistentContainer;

        /// <summary> 当前活跃容器 </summary>
        private static GridContainerView activeContainer;

        public static IReadOnlyList<GridContainerView> ContainerList => containerList;

        /// <summary> 常驻容器 </summary>
        public static GridContainerView PersistentContainer => persistentContainer;

        /// <summary> 活跃容器 </summary>
        public static GridContainerView ActiveContainer => activeContainer;

        /// <summary>
        /// 注册背包容器
        /// </summary>
        public static void Register(GridContainerView container)
        {
            if (container is null || containerList.Contains(container))
                return;

            containerList.Add(container);

            if (container.ContainerRole == EGridContainerRole.Persistent)
                persistentContainer = container;

            if (container.ContainerRole == EGridContainerRole.Active)
                activeContainer = container;
        }

        /// <summary>
        /// 注销背包容器
        /// </summary>
        public static void Unregister(GridContainerView container)
        {
            if (container is null)
                return;

            containerList.Remove(container);

            if (persistentContainer == container)
                persistentContainer = null;

            if (activeContainer == container)
                activeContainer = null;
        }

        /// <summary>
        /// 设置当前活跃容器
        /// </summary>
        public static void SetActiveContainer(GridContainerView container)
        {
            activeContainer = container;
        }

        /// <summary>
        /// 清除当前活跃容器
        /// </summary>
        public static void ClearActiveContainer()
        {
            activeContainer = null;
        }

        /// <summary>
        /// 解析快捷互转目标容器
        /// </summary>
        public static bool TryResolveQuickTransferTarget(GridContainerView sourceContainer,
                                                         out GridContainerView targetContainer)
        {
            targetContainer = null;
            if (sourceContainer is null)
                return false;

            if (sourceContainer.ContainerRole == EGridContainerRole.Persistent)
                targetContainer = activeContainer;
            else if (sourceContainer.ContainerRole == EGridContainerRole.Active
                     || sourceContainer == activeContainer)
                targetContainer = persistentContainer;
            else
                return false;

            return targetContainer is not null && targetContainer.IsInventoryReady;
        }

        /// <summary>
        /// 尝试解析鼠标悬停的背包容器
        /// </summary>
        /// <param name="screenPos">屏幕坐标</param>
        /// <param name="hoverContainer">悬停的背包容器</param>
        /// <returns></returns>
        public static bool TryResolveHoverContainer(Vector2 screenPos,
                                                    out GridContainerView hoverContainer,
                                                    out Vector2Int gridPos,
                                                    out int gridIndex)
        {
            hoverContainer = null;
            gridPos = Vector2Int.zero;
            gridIndex = -1;

            // 遍历所有背包容器 谁响应谁就是悬停的背包容器
            for (int i = containerList.Count - 1; i >= 0; i--)
            {
                var container = containerList[i];
                if (container == null) continue;

                // 判断视窗
                if (!container.PointIsInViewprot(screenPos)) continue;

                // 判断是否是格子内
                if (!container.TryGetMouseInGridInfo(screenPos, out gridPos, out gridIndex)) continue;

                hoverContainer = container;
                return true;
            }
            return false;
        }
    }
}
