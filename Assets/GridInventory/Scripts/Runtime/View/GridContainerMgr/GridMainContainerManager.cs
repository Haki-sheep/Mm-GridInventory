using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 场景内所有背包容器管理器
    /// </summary>
    public static class GridMainContainerManager
    {
        private static readonly List<GridMainContainerView> containerList = new();

        public static IReadOnlyList<GridMainContainerView> ContainerList => containerList;
        
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


        /// <summary>
        /// 尝试解析鼠标悬停的背包容器
        /// </summary>
        /// <param name="screenPos">屏幕坐标</param>
        /// <param name="hoverContainer">悬停的背包容器</param>
        /// <returns></returns>
        public static bool TryResolveHoverContainer(Vector2 screenPos,
                                                    out GridMainContainerView hoverContainer,
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
                if(!container.PointIsInViewprot(screenPos)) continue;
                
                // 判断是否是格子内
                if(!container.TryGetMouseInGridInfo(screenPos, out gridPos, out gridIndex)) continue;

                hoverContainer = container;
                return true;
            }
            return false;
        }
    }
}
