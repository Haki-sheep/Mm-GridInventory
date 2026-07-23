using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace MmInventory
{
    public enum EOnDragState
    {
        None,
        OnBeginDrag,
        OnDragging,
        OnEndDrag,
    }

    /// <summary>
    /// 此脚本放置视图工具方法 是View这边的辅助逻辑
    /// 大多数是UI显示和操作相关的逻辑
    /// </summary>
    public partial class GridContainerView
    {
        /// <summary>
        /// 拖拽滚动速度
        /// </summary>
        [SerializeField, LabelText("拖拽滚动速度")]
        private float dragScrollWheelSpeed = 100f;

        #region 拖拽预览

        /// <summary>
        /// 清除 footprint 预览高亮
        /// </summary>
        private void ClearFootprintCellPreview()
        {
            if (gridCellViewList is null || previewFootprintCellIndexList.Count == 0)
                return;

            for (int i = 0; i < previewFootprintCellIndexList.Count; i++)
            {
                int cellIndex = previewFootprintCellIndexList[i];
                if (cellIndex < 0 || cellIndex >= gridCellViewList.Length)
                    continue;

                gridCellViewList[cellIndex].SetPreviewState(ECellPreviewState.None);
            }

            previewFootprintCellIndexList.Clear();
        }

        /// <summary>
        /// 刷新 footprint 预览高亮
        /// </summary>
        private void UpdateFootprintCellPreview(ItemRtData itemDataA,
                                                Vector2Int previewAnchorPos,
                                                ESwapPlaceMode swapPlaceMode)
        {
            ClearFootprintCellPreview();

            if (itemDataA is null || gridCellViewList is null || gridCellViewList.Length == 0)
                return;

            var itemDataB = gridInventoryService.GetItemAt(previewAnchorPos);
            var previewResult = gridInventoryService.JudgeDragPreviewState(itemDataA,
                                                                           itemDataB,
                                                                           previewAnchorPos,
                                                                           swapPlaceMode);
            var previewState = previewResult == EDragPreviewState.CannotPlace
                ? ECellPreviewState.Invalid
                : ECellPreviewState.Valid;

            var dataSize = itemDataA.DataSize;
            for (int x = 0; x < dataSize.x; x++)
            {
                for (int y = 0; y < dataSize.y; y++)
                {
                    int cellIndex = ToCellIndex(new Vector2Int(previewAnchorPos.x + x, previewAnchorPos.y + y));
                    if (cellIndex < 0 || cellIndex >= gridCellViewList.Length)
                        continue;

                    gridCellViewList[cellIndex].SetPreviewState(previewState);
                    previewFootprintCellIndexList.Add(cellIndex);
                }
            }
        }

        /// <summary>
        /// 处理拖拽预览表现
        /// </summary>
        private void HandlerDragPreview(EOnDragState state,
                                        ESwapPlaceMode swapPlaceMode = ESwapPlaceMode.SameContainer)
        {
            switch (state)
            {
                case EOnDragState.None:
                case EOnDragState.OnEndDrag:
                    ClearFootprintCellPreview();
                    break;

                case EOnDragState.OnBeginDrag:
                case EOnDragState.OnDragging:
                    if (dragSession.DraggingItem is null || dragSession.DraggingItem.ItemData is null)
                        return;

                    UpdateFootprintCellPreview(dragSession.DraggingItem.ItemData,
                                               dragSession.PreviewAnchorPos,
                                               swapPlaceMode);
                    break;
            }
        }

        private void SetCellHighlight(int cellIndex)
        {
            if (cellIndex == -1) return;
            // 设置高亮格子 如果当前格子与上一帧格子不同 则设置高亮格子
            if (curHighLightCellIndex != cellIndex)
            {
                if (curHighLightCellIndex != -1)
                    gridCellViewList[curHighLightCellIndex].SetBkHighLight(false);
                gridCellViewList[cellIndex].SetBkHighLight(true);

                curHighLightCellIndex = cellIndex;
            }
        }
        /// <summary>
        /// 清除所有高亮格子
        /// </summary>
        private void ClearCellHighlight()
        {
            if (curHighLightCellIndex >= 0)
            {
                gridCellViewList[curHighLightCellIndex].SetBkHighLight(false);
                curHighLightCellIndex = -1;
                return;
            }

            foreach (var cellView in gridCellViewList)
            {
                cellView.SetBkHighLight(false);
            }
        }
        /// <summary>
        /// 设置拖拽过程中物品层级
        /// </summary>
        private void SetTransformSibingIndex(EOnDragState state)
        {
            switch (state)
            {
                case EOnDragState.OnBeginDrag:
                    dragSession.DraggingItem.ItemRectTransform.SetAsLastSibling();
                    break;

                case EOnDragState.OnEndDrag:
                    if (dragSession.DraggingItem is not null)
                    {
                        int maxIndex = Mathf.Max(0, dragSession.DraggingItem.ItemRectTransform.parent.childCount - 1);
                        int safeIndex = Mathf.Clamp(dragSession.StartSiblingIndex, 0, maxIndex);
                        dragSession.DraggingItem.ItemRectTransform.SetSiblingIndex(safeIndex);
                    }
                    break;
            }
        }

        /// <summary>
        /// 清除本容器拖拽预览
        /// </summary>
        public void ClearDragPreview()
        {
            ClearCellHighlight();
            ClearFootprintCellPreview();
        }


        /// <summary>
        /// 外部容器拖入时 在本容器显示预览
        /// </summary>
        /// <param name="itemView">拖拽物品</param>
        /// <param name="dragOffset">拖拽相对偏移</param>
        /// <param name="mouseOnGridPos">鼠标当前悬停的格子位置</param>
        /// <param name="gridIndex">当前高亮格子索引</param>
        public void HandleForeignDragPreview(ItemView itemView,
                                             Vector2Int dragOffset,
                                             Vector2Int mouseOnGridPos,
                                             int gridIndex,
                                             GridDragSession sourceDragSession)
        {
            if (itemView is null || itemView.ItemData is null)
                return;

            // 在本容器坐标系下算预览锚点
            var previewAnchor = GetPreviewAnchorPos(mouseOnGridPos, dragOffset, itemView.ItemData);

            // 当前格不可放且曾自动旋转 先还原拖起朝向
            TryRevertAutoRotationForPreview(itemView,
                                            ref previewAnchor,
                                            mouseOnGridPos,
                                            dragOffset,
                                            ESwapPlaceMode.CrossContainer,
                                            sourceDragSession);

            // 尝试自动旋转
            TryAutoRotateForPreview(itemView,
                                    ref previewAnchor,
                                    mouseOnGridPos,
                                    dragOffset,
                                    ESwapPlaceMode.CrossContainer,
                                    sourceDragSession);
                                    
            dragSession.PreviewAnchorPos = previewAnchor;
            dragSession.CachedPreviewAnchorPos = previewAnchor;

            UpdateFootprintCellPreview(itemView.ItemData,
                                       previewAnchor,
                                       ESwapPlaceMode.CrossContainer);
        }

        #endregion

        #region 滑动滚动

        /// <summary>
        /// 处理鼠标滚轮滑动
        /// </summary>
        private void HandleScrollWithMouseWheel()
        {
            if (!dragSession.IsActive || ScrollRect == null || scrollContent == null)
                return;

            float wheel = Input.mouseScrollDelta.y;
            if (Mathf.Approximately(wheel, 0f))
                return;

            float viewHeight = ScrollRect.viewport.rect.height;
            float maxScrollY = Mathf.Max(0f, scrollContent.rect.height - viewHeight);

            Vector2 pos = scrollContent.anchoredPosition;
            pos.y = Mathf.Clamp(pos.y - wheel * dragScrollWheelSpeed, 0f, maxScrollY);
            scrollContent.anchoredPosition = pos;

            HandleScrollWithScrollBar(pos.y, maxScrollY);
            HandleScrollWithItem(Input.mousePosition);
        }

        /// <summary>
        /// 同步拖拽滚动条位置
        /// </summary>
        private void HandleScrollWithScrollBar(float scrollY, float maxScrollY)
        {
            Scrollbar scrollbar = ScrollRect.verticalScrollbar;
            if (scrollbar == null)
                return;

            float normalized = maxScrollY > 0f ? 1f - scrollY / maxScrollY : 1f;
            scrollbar.SetValueWithoutNotify(normalized);
        }

        /// <summary>
        /// 同步拖拽表现与预览锚点位置
        /// 没有此函数会导致物品跟随鼠标时吸附框不及时
        /// </summary>
        private void HandleScrollWithItem(Vector2 screenPos)
        {
            if (dragSession.DraggingItem is null)
                return;

            // 物品跟随鼠标
            dragSession.DraggingItem.ItemRectTransform.position = screenPos;

            // 计算网格坐标
            if (!TryGetMouseInGridInfo(screenPos, out var mouseOnGridPos, out _))
            {
                RevertAutoRotationIfActive(dragSession.DraggingItem, dragSession);
                ClearDragPreview();
                return;
            }

            var previewAnchorPos = GetPreviewAnchorPos(mouseOnGridPos,
                                                       dragSession.StartOffset,
                                                       dragSession.DraggingItem.ItemData);
            TryRevertAutoRotationForPreview(dragSession.DraggingItem,
                                            ref previewAnchorPos,
                                            mouseOnGridPos,
                                            dragSession.StartOffset,
                                            ESwapPlaceMode.SameContainer,
                                            dragSession);
            TryAutoRotateForPreview(dragSession.DraggingItem,
                                    ref previewAnchorPos,
                                    mouseOnGridPos,
                                    dragSession.StartOffset,
                                    ESwapPlaceMode.SameContainer,
                                    dragSession);

            if (dragSession.CachedPreviewAnchorPos == previewAnchorPos)
                return;

            dragSession.PreviewAnchorPos = previewAnchorPos;
            dragSession.CachedPreviewAnchorPos = previewAnchorPos;
            HandlerDragPreview(EOnDragState.OnDragging);
        }

        #endregion


        #region 同步容器布局 做Container配置的

        /// <summary>
        /// 按名称查找子 RectTransform 包含未激活节点
        /// </summary>
        private static RectTransform FindChildRectTransform(Transform root, string childName)
        {
            if (root == null)
                return null;

            var rectList = root.GetComponentsInChildren<RectTransform>(true);
            for (int i = 0; i < rectList.Length; i++)
            {
                if (rectList[i].name == childName)
                    return rectList[i];
            }

            return null;
        }
#if UNITY_EDITOR
        /// <summary>
        /// 外部调用 创建物品格子并同步容器布局
        /// </summary>
        [Button]
        public void CreatItemCells()
        {
            gridContentCache = null;
            itemContentCache = null;
            contentLayoutGroupCache = null;
            scrollContentCache = null;
            scrollRectCache = null;

            if (gridRowAndCloumns.x <= 0 || gridRowAndCloumns.y <= 0)
            {
                Debug.LogWarning("GridMainContainerView: gridRowAndCloumns 无效");
                return;
            }

            if (visibleHeight <= 0)
            {
                Debug.LogWarning("GridMainContainerView: visibleHeight 无效");
                return;
            }

            if (ScrollRect == null)
            {
                Debug.LogWarning("GridMainContainerView: 未找到 ScrollRect 组件");
                return;
            }

            EnsureScrollContentHierarchy();

            if (scrollContent == null || gridContent == null || itemContent == null)
            {
                Debug.LogWarning("GridMainContainerView: 未找到 Content GridContent 或 ItemContent");
                return;
            }

            float contentHeight = gridSize * gridRowAndCloumns.y;
            var rootPixelSize = new Vector2(gridSize * gridRowAndCloumns.x, visibleHeight);
            int cellCount = gridRowAndCloumns.x * gridRowAndCloumns.y;

            SyncRootContainerSize(rootPixelSize);
            SyncTopStretchContentRect(scrollContent, contentHeight);
            SyncStretchFillRect(gridContent);
            SyncStretchFillRect(itemContent);
            SyncGridLayoutGroupSettings();
            RebuildGridCellInstances(cellCount);
            SyncVerticalScrollbarVisibility(contentHeight);
            NotifyNameBarHost();

            gridCellViewList = gridContent.GetComponentsInChildren<GridCellView>();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                EditorUtility.SetDirty(this);
#endif
        }

        /// <summary>
        /// 内容高度未超过可视高度时隐藏竖向滚动条
        /// 滚动条在容器外侧时禁止改 Visibility 否则会挤压 Viewport 挡住末列
        /// </summary>
        private void SyncVerticalScrollbarVisibility(float contentHeight)
        {
            var scrollRect = ScrollRect;
            if (scrollRect == null)
                return;

            // 内容装得下就不需要滚动条
            bool needScroll = contentHeight > visibleHeight + 0.01f;

            Scrollbar scrollbar = scrollRect.verticalScrollbar;
            if (scrollbar == null)
            {
                var scrollbarRect = FindChildRectTransform(transform, "Scrollbar Vertical");
                if (scrollbarRect != null)
                    scrollbar = scrollbarRect.GetComponent<Scrollbar>();
                if (scrollbar != null)
                    scrollRect.verticalScrollbar = scrollbar;
            }

            if (scrollbar != null)
                scrollbar.gameObject.SetActive(needScroll);

            // 外侧滚动条布局保持 Permanent 只切启用与显隐
            scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.Permanent;
            scrollRect.vertical = needScroll;

            // 外侧滚动条 用负 spacing 抵消 Permanent 对 Viewport 的右缩进
            if (scrollbar != null)
            {
                var sbRect = scrollbar.transform as RectTransform;
                float sbWidth = sbRect != null ? Mathf.Abs(sbRect.sizeDelta.x) : 20f;
                if (sbWidth < 1f)
                    sbWidth = 20f;
                scrollRect.verticalScrollbarSpacing = -sbWidth;
            }

            // 清掉 AutoHideAndExpandViewport 可能留下的右缩进
            RectTransform viewport = scrollRect.viewport;
            if (viewport != null)
            {
                viewport.anchorMin = Vector2.zero;
                viewport.anchorMax = Vector2.one;
                viewport.pivot = new Vector2(0f, 1f);
                viewport.offsetMin = Vector2.zero;
                viewport.offsetMax = Vector2.zero;
            }

            if (!needScroll && scrollContent != null)
            {
                Vector2 pos = scrollContent.anchoredPosition;
                pos.y = 0f;
                scrollContent.anchoredPosition = pos;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                EditorUtility.SetDirty(scrollRect);
                if (viewport != null)
                    EditorUtility.SetDirty(viewport);
                if (scrollbar != null)
                    EditorUtility.SetDirty(scrollbar.gameObject);
            }
#endif
        }

        /// <summary>
        /// 通知外层名称栏外壳同步布局
        /// </summary>
        private void NotifyNameBarHost()
        {
            var nameBar = GetComponentInParent<GridContainerNameBar>();
            nameBar?.SyncAfterContainerBuilt();
        }

        /// <summary>
        /// 确保 Viewport 下存在 Content 壳层并收纳 GridContent ItemContent
        /// </summary>
        private void EnsureScrollContentHierarchy()
        {
            RectTransform viewport = ScrollRect.viewport;
            RectTransform contentRoot = FindChildRectTransform(viewport, "Content");

            if (contentRoot == null)
            {
                var contentGo = new GameObject("Content", typeof(RectTransform));
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.RegisterCreatedObjectUndo(contentGo, "Ensure Scroll Content");
#endif
                contentRoot = contentGo.GetComponent<RectTransform>();
                contentRoot.SetParent(viewport, false);
            }

            RectTransform grid = FindChildRectTransform(viewport, "GridContent");
            if (grid == null)
                grid = FindChildRectTransform(contentRoot, "GridContent");

            RectTransform item = FindChildRectTransform(viewport, "ItemContent");
            if (item == null)
                item = FindChildRectTransform(contentRoot, "ItemContent");

            if (grid != null && grid.parent != contentRoot)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.SetTransformParent(grid, contentRoot, "Ensure Scroll Content");
                else
#endif
                    grid.SetParent(contentRoot, false);
            }

            if (item != null && item.parent != contentRoot)
            {
#if UNITY_EDITOR
                if (!Application.isPlaying)
                    Undo.SetTransformParent(item, contentRoot, "Ensure Scroll Content");
                else
#endif
                    item.SetParent(contentRoot, false);
            }

            if (grid != null)
                grid.SetSiblingIndex(0);
            if (item != null)
                item.SetSiblingIndex(1);

            ScrollRect.content = contentRoot;
            scrollContentCache = contentRoot;
        }

        /// <summary>
        /// 同步根容器宽高
        /// </summary>
        private void SyncRootContainerSize(Vector2 pixelSize)
        {
            var rootRect = transform as RectTransform;
            rootRect.sizeDelta = pixelSize;
        }

        /// <summary>
        /// 设置子层铺满 Content
        /// </summary>
        private static void SyncStretchFillRect(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0f, 1f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        /// <summary>
        /// 设置横向拉伸顶对齐 RectTransform
        /// </summary>
        private static void SyncTopStretchContentRect(RectTransform rect, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.SetInsetAndSizeFromParentEdge(RectTransform.Edge.Top, 0f, height);
            rect.offsetMin = new Vector2(0f, rect.offsetMin.y);
            rect.offsetMax = new Vector2(0f, 0f);
        }

        /// <summary>
        /// 同步 GridLayoutGroup 列数与格子尺寸
        /// </summary>
        private void SyncGridLayoutGroupSettings()
        {
            if (contentLayoutGroup == null) return;

            contentLayoutGroup.cellSize = new Vector2(gridSize, gridSize);
            contentLayoutGroup.spacing = new Vector2(spacing, spacing);
            contentLayoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            contentLayoutGroup.constraintCount = gridRowAndCloumns.x;

            var sizeFitter = gridContent.GetComponent<ContentSizeFitter>();
            if (sizeFitter != null)
                sizeFitter.verticalFit = ContentSizeFitter.FitMode.Unconstrained;
        }

        /// <summary>
        /// 按数量重建 GridContent 下格子实例
        /// </summary>
        private void RebuildGridCellInstances(int cellCount)
        {
            Transform cellRoot = contentLayoutGroup.transform;
            if (cellRoot.childCount == cellCount && cellCount > 0)
                return;

            for (int i = cellRoot.childCount - 1; i >= 0; i--)
                DestroyCellChild(cellRoot.GetChild(i).gameObject);

            if (cellCount <= 0) return;

            GameObject prefabSource = GetCellPrefabSource();
            if (prefabSource is null)
            {
                Debug.LogWarning("GridMainContainerView: CellPrefab 无效 请拖 Project 里的预制体资源");
                return;
            }

            for (int i = 0; i < cellCount; i++)
                CreateCellInstance(prefabSource, i);
        }

        /// <summary>
        /// 销毁格子子节点
        /// </summary>
        private static void DestroyCellChild(GameObject child)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
                GameObject.DestroyImmediate(child);
            else
#endif
                GameObject.Destroy(child);
        }

        /// <summary>
        /// 创建格子实例并保持预制体关联
        /// </summary>
        private void CreateCellInstance(GameObject prefabSource, int index)
        {
            GameObject cell;
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                cell = (GameObject)PrefabUtility.InstantiatePrefab(prefabSource, contentLayoutGroup.transform);
                Undo.RegisterCreatedObjectUndo(cell, "Creat Item Cells");
            }
            else
#endif
                cell = Instantiate(prefabSource, contentLayoutGroup.transform);

            cell.name = $"Cell_{index}";

            if (cell.GetComponent<GridCellView>() is null)
                cell.AddComponent<GridCellView>();
        }

        /// <summary>
        /// 解析可实例化的格子预制体资源
        /// </summary>
        private GameObject GetCellPrefabSource()
        {
            if (CellPrefab is null) return null;

#if UNITY_EDITOR
            if (PrefabUtility.IsPartOfPrefabAsset(CellPrefab))
                return CellPrefab;

            GameObject asset = PrefabUtility.GetCorrespondingObjectFromSource(CellPrefab) as GameObject;
            if (asset is not null)
                return asset;
#endif
            return CellPrefab;
        }
#endif
        #endregion
    }
}