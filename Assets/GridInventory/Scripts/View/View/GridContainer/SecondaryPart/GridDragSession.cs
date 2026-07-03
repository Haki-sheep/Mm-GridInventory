using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 单次拖拽会话状态
    /// 只记录拖拽生命周期数据 不处理 UI 与数据层
    /// </summary>
    public sealed class GridDragSession
    {
        /// <summary> 是否正在拖拽 </summary>
        public bool IsActive { get; private set; }

        /// <summary> 拖拽中的物品 </summary>
        public ItemView DraggingItem { get; private set; }

        /// <summary> 拖拽起始锚点 </summary>
        public Vector2Int StartAnchorPos { get; private set; }

        /// <summary> 预览锚点 </summary>
        public Vector2Int PreviewAnchorPos { get; set; }

        /// <summary> 预览锚点缓存 避免重复刷新 </summary>
        public Vector2Int CachedPreviewAnchorPos { get; set; }

        /// <summary> 拖拽物起始层级 </summary>
        public int StartSiblingIndex { get; private set; }

        /// <summary> 拖拽物起始旋转状态 </summary>
        public bool StartIsRotated { get; private set; }

        /// <summary> 抓取相对偏移 </summary>
        public Vector2Int StartOffset { get; private set; }

        /// <summary> 拖拽起始容器 </summary>
        public GridContainerView SourceContainer { get; private set; }

        /// <summary> 当前悬停容器 </summary>
        public GridContainerView HoverContainer { get; set; }

        /// <summary> 玩家手动旋转后锁定自动旋转 </summary>
        public bool ManualRotationLocked { get; set; }

        /// <summary> 预览阶段是否由自动旋转改过朝向 </summary>
        public bool AutoRotatedForPreview { get; set; }

        /// <summary>
        /// 开始拖拽会话
        /// </summary>
        public void Begin(ItemView itemView,
                          Vector2Int startAnchorPos,
                          bool startIsRotated,
                          Vector2Int startOffset,
                          int startSiblingIndex,
                          GridContainerView sourceContainer)
        {
            IsActive = true;
            DraggingItem = itemView;
            StartAnchorPos = startAnchorPos;
            StartIsRotated = startIsRotated;
            StartOffset = startOffset;
            StartSiblingIndex = startSiblingIndex;
            SourceContainer = sourceContainer;
            HoverContainer = null;
            PreviewAnchorPos = startAnchorPos;
            CachedPreviewAnchorPos = Vector2Int.zero;
            ManualRotationLocked = false;
            AutoRotatedForPreview = false;
        }

        /// <summary>
        /// 结束拖拽会话并清空状态
        /// </summary>
        public void Clear()
        {
            IsActive = false;
            DraggingItem = null;
            StartAnchorPos = Vector2Int.zero;
            PreviewAnchorPos = Vector2Int.zero;
            CachedPreviewAnchorPos = Vector2Int.zero;
            StartSiblingIndex = -1;
            StartIsRotated = false;
            StartOffset = Vector2Int.zero;
            SourceContainer = null;
            HoverContainer = null;
            ManualRotationLocked = false;
            AutoRotatedForPreview = false;
        }

        /// <summary>
        /// 强制刷新预览缓存
        /// </summary>
        public void InvalidatePreviewCache()
        {
            CachedPreviewAnchorPos = new Vector2Int(int.MinValue, int.MinValue);
        }
    }
}
