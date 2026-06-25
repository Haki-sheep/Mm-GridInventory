using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 网格占格物品最小契约
    /// Core 只依赖此接口
    /// </summary>
    public interface IItemRuntime
    {
        /// <summary> 配表物品ID </summary>
        int ExcelItemId { get; }

        /// <summary> 实例唯一ID </summary>
        string InstancedItemId { get; }

        /// <summary> 锚点 </summary>
        Vector2Int AnchorPos { get; }

        /// <summary> 占格尺寸 </summary>
        Vector2Int DataSize { get; }

        /// <summary> 是否旋转 </summary>
        bool IsRotated { get; }

        /// <summary> 是否可堆叠 </summary>
        EItemStackType ItemStackType { get; }

        /// <summary> 最大堆叠数量 </summary>
        int MaxStackCount { get; }

        int CurrStackCount { get; set; }

        /// <summary>
        /// 设置锚点
        /// </summary>
        void SetAnchorPos(Vector2Int anchorPos);

        /// <summary>
        /// 克隆一份用于放置
        /// </summary>
        /// <param name="stackCount"></param>
        /// <returns></returns>
        IItemRuntime Clone(int stackCount);
    }
}
