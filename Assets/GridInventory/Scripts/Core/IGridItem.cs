using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 网格占格物品最小契约
    /// Core 只依赖此接口
    /// </summary>
    public interface IGridItem
    {
        /// <summary> 实例唯一ID </summary>
        string InstancedItemId { get; }

        /// <summary> 锚点 </summary>
        Vector2Int AnchorPos { get; }

        /// <summary> 占格尺寸 </summary>
        Vector2Int DataSize { get; }

        /// <summary> 是否旋转 </summary>
        bool IsRotated { get; }

        /// <summary>
        /// 设置锚点
        /// </summary>
        void SetAnchorPos(Vector2Int anchorPos);
    }
}
