using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

namespace MmInventory
{
    /// <summary>
    /// 自然容器搜刮揭幕
    /// 全部待搜物品先盖遮罩 放大镜逐个轮询 搜完外扩缩小揭开
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridContainerView))]
    public class ContainerLootRevealMask : MonoBehaviour
    {
        #region 引用

        /// <summary> 遮罩模板 运行时克隆到每个物品 </summary>
        [SerializeField]
        [ChineseLabel("遮罩模板")]
        private RectTransform maskRoot;

        /// <summary> 遮罩图模板 </summary>
        [SerializeField]
        [ChineseLabel("遮罩图")]
        private Image maskImage;

        /// <summary> 放大镜节点 全局唯一轮询 </summary>
        [SerializeField]
        [ChineseLabel("放大镜")]
        private RectTransform magnifierRect;

        /// <summary> 放大镜图 </summary>
        [SerializeField]
        [ChineseLabel("放大镜图")]
        private Image magnifierImage;

        /// <summary> 全容器操作阻挡 空则自动创建 </summary>
        [SerializeField]
        [ChineseLabel("操作阻挡层")]
        private Image inputBlocker;

        #endregion

        #region 参数

        /// <summary> 各稀有度单件搜索时长 </summary>
        [SerializeField]
        [ChineseLabel("稀有度搜索时长")]
        private List<LootRevealRarityDuration> rarityDurationList = CreateDefaultRarityDurationList();

        /// <summary> 缺省搜索时长 </summary>
        [SerializeField]
        [ChineseLabel("缺省搜索时长")]
        private float fallbackSearchDuration = 0.55f;

        /// <summary> 斜向浮动振幅 </summary>
        [SerializeField]
        [ChineseLabel("浮动振幅")]
        private float floatAmplitude = 6f;

        /// <summary> 浮动与呼吸周期 </summary>
        [SerializeField]
        [ChineseLabel("浮动周期")]
        private float floatCycle = 0.45f;

        /// <summary> 呼吸最暗透明度 </summary>
        [SerializeField]
        [ChineseLabel("呼吸最暗")]
        [Range(0.35f, 1f)]
        private float breatheAlphaMin = 0.55f;

        /// <summary> 呼吸最亮透明度 </summary>
        [SerializeField]
        [ChineseLabel("呼吸最亮")]
        [Range(0.35f, 1f)]
        private float breatheAlphaMax = 1f;

        /// <summary> 结束外扩倍率 </summary>
        [SerializeField]
        [ChineseLabel("外扩倍率")]
        private float expandScale = 1.18f;

        /// <summary> 外扩时长 </summary>
        [SerializeField]
        [ChineseLabel("外扩时长")]
        private float expandDuration = 0.14f;

        /// <summary> 缩小消失时长 </summary>
        [SerializeField]
        [ChineseLabel("缩小时长")]
        private float shrinkDuration = 0.22f;

        #endregion

        /// <summary> 单物品遮罩实例 </summary>
        private sealed class ItemMaskSlot
        {
            public ItemView ItemView;
            public RectTransform MaskRect;
            public Image MaskImage;
        }

        /// <summary> 容器视图 </summary>
        private GridContainerView containerView;

        /// <summary> 待揭幕队列 </summary>
        private readonly List<ItemView> pendingItemList = new();

        /// <summary> 与队列对齐的遮罩实例 </summary>
        private readonly List<ItemMaskSlot> maskSlotList = new();

        /// <summary> 当前揭幕下标 </summary>
        private int revealIndex;

        /// <summary> 全部完成回调 </summary>
        private Action completeCallback;

        /// <summary> 搜索循环序列 </summary>
        private Sequence searchLoopSequence;

        /// <summary> 结束序列 </summary>
        private Sequence outroSequence;

        /// <summary> 是否正在播放 </summary>
        private bool isPlaying;

        /// <summary> 放大镜基准缩放 </summary>
        private Vector3 magnifierBaseScale = Vector3.one;

        public bool IsPlaying => isPlaying;

        #region 生命周期

        private void Awake()
        {
            containerView = GetComponent<GridContainerView>();
            magnifierBaseScale = magnifierRect != null && magnifierRect.localScale != Vector3.zero
                ? magnifierRect.localScale
                : Vector3.one;

            // 放大镜从模板下拆出 避免克隆时复制多份
            DetachMagnifierFromTemplate();

            if (maskRoot != null)
                maskRoot.gameObject.SetActive(false);
            if (magnifierRect != null)
                magnifierRect.gameObject.SetActive(false);
            SetInputBlock(false);
        }

        private void OnDestroy()
        {
            KillAllTweens();
            ClearMaskSlots();
        }

        /// <summary>
        /// 放大镜独立于遮罩模板
        /// </summary>
        private void DetachMagnifierFromTemplate()
        {
            if (magnifierRect == null || maskRoot == null)
                return;

            if (magnifierRect.IsChildOf(maskRoot))
                magnifierRect.SetParent(transform, false);
        }

        #endregion

        #region 对外接口

        /// <summary>
        /// 静默关闭 已搜过打开时用
        /// </summary>
        public void HideImmediate()
        {
            KillAllTweens();
            isPlaying = false;
            revealIndex = 0;
            pendingItemList.Clear();
            completeCallback = null;
            ClearMaskSlots();
            if (maskRoot != null)
                maskRoot.gameObject.SetActive(false);
            if (magnifierRect != null)
                magnifierRect.gameObject.SetActive(false);
            SetInputBlock(false);
            ShowAllItems();
        }

        /// <summary>
        /// 先为全部物品盖遮罩 再放大镜逐个轮询
        /// </summary>
        public void PlayReveal(Action onComplete = null)
        {
            if (!ValidateRefs())
            {
                ShowAllItems();
                onComplete?.Invoke();
                return;
            }

            KillAllTweens();
            DetachMagnifierFromTemplate();
            EnsureInputBlocker();
            ClearMaskSlots();

            pendingItemList.Clear();
            CollectAndSortItems(pendingItemList);
            completeCallback = onComplete;
            revealIndex = 0;
            isPlaying = true;

            for (int i = 0; i < pendingItemList.Count; i++)
                SetItemVisible(pendingItemList[i], false);

            BuildMaskSlotsForPending();
            SetInputBlock(true);

            if (maskRoot != null)
                maskRoot.gameObject.SetActive(false);

            if (pendingItemList.Count == 0)
            {
                FinishAll();
                return;
            }

            if (magnifierRect != null)
                magnifierRect.gameObject.SetActive(true);

            PlayCurrentItem();
        }

        #endregion

        #region 队列与排序

        /// <summary>
        /// 收集并按锚点再按面积排序
        /// </summary>
        private void CollectAndSortItems(List<ItemView> outList)
        {
            if (containerView is null)
                containerView = GetComponent<GridContainerView>();
            if (containerView is null)
                return;

            var itemViewList = containerView.GetItemViewList();
            if (itemViewList is null)
                return;

            for (int i = 0; i < itemViewList.Count; i++)
            {
                var itemView = itemViewList[i];
                if (itemView != null && itemView.ItemData != null)
                    outList.Add(itemView);
            }

            outList.Sort(CompareItemRevealOrder);
        }

        /// <summary>
        /// 锚点 Y X 优先 再比占地面积
        /// </summary>
        private static int CompareItemRevealOrder(ItemView a, ItemView b)
        {
            Vector2Int anchorA = a.ItemData.AnchorPos;
            Vector2Int anchorB = b.ItemData.AnchorPos;
            int compareY = anchorA.y.CompareTo(anchorB.y);
            if (compareY != 0)
                return compareY;

            int compareX = anchorA.x.CompareTo(anchorB.x);
            if (compareX != 0)
                return compareX;

            int areaA = a.ItemData.DataSize.x * a.ItemData.DataSize.y;
            int areaB = b.ItemData.DataSize.x * b.ItemData.DataSize.y;
            return areaA.CompareTo(areaB);
        }

        #endregion

        #region 播放流程

        /// <summary>
        /// 播放当前队列物品
        /// </summary>
        private void PlayCurrentItem()
        {
            if (revealIndex >= pendingItemList.Count)
            {
                FinishAll();
                return;
            }

            var itemView = pendingItemList[revealIndex];
            var slot = revealIndex < maskSlotList.Count ? maskSlotList[revealIndex] : null;
            if (itemView == null || slot == null || slot.MaskRect == null)
            {
                revealIndex++;
                PlayCurrentItem();
                return;
            }

            AttachMagnifierToSlot(slot);
            PrepareMagnifierVisual();
            BringSearchLayerToFront(slot);
            PlaySearchLoop();

            float duration = ResolveSearchDuration(itemView.ItemData.ItemRarity);
            outroSequence = DOTween.Sequence().SetUpdate(true);
            outroSequence.AppendInterval(Mathf.Max(0.05f, duration));
            outroSequence.OnComplete(PlayOutroForCurrent);
        }

        /// <summary>
        /// 按稀有度取搜索时长
        /// </summary>
        private float ResolveSearchDuration(EItemRarity eItemRarity)
        {
            if (rarityDurationList != null)
            {
                for (int i = 0; i < rarityDurationList.Count; i++)
                {
                    var entry = rarityDurationList[i];
                    if (entry != null && entry.ItemRarity == eItemRarity)
                        return Mathf.Max(0.05f, entry.SearchDuration);
                }
            }

            return Mathf.Max(0.05f, fallbackSearchDuration);
        }

        /// <summary>
        /// 默认稀有度时长 越高越久
        /// </summary>
        private static List<LootRevealRarityDuration> CreateDefaultRarityDurationList()
        {
            return new List<LootRevealRarityDuration>
            {
                new LootRevealRarityDuration(EItemRarity.White, 0.35f),
                new LootRevealRarityDuration(EItemRarity.Blue, 0.5f),
                new LootRevealRarityDuration(EItemRarity.Purple, 0.7f),
                new LootRevealRarityDuration(EItemRarity.Gold, 0.95f),
                new LootRevealRarityDuration(EItemRarity.Red, 1.25f)
            };
        }

        /// <summary>
        /// 当前件结束 仅拆当前遮罩 其余遮罩保留
        /// </summary>
        private void PlayOutroForCurrent()
        {
            if (searchLoopSequence != null && searchLoopSequence.IsActive())
                searchLoopSequence.Kill();
            searchLoopSequence = null;

            if (outroSequence != null && outroSequence.IsActive())
                outroSequence.Kill();
            outroSequence = null;

            if (magnifierRect != null)
                magnifierRect.anchoredPosition = Vector2.zero;

            var slot = revealIndex < maskSlotList.Count ? maskSlotList[revealIndex] : null;
            var currentItem = slot != null ? slot.ItemView : null;
            var currentMaskRect = slot != null ? slot.MaskRect : null;
            var currentMaskImage = slot != null ? slot.MaskImage : null;

            if (currentMaskRect == null)
            {
                SetItemVisible(currentItem, true);
                revealIndex++;
                PlayCurrentItem();
                return;
            }

            outroSequence = DOTween.Sequence().SetUpdate(true);
            outroSequence.Append(currentMaskRect.DOScale(expandScale, expandDuration).SetEase(Ease.OutQuad));
            outroSequence.Join(magnifierRect.DOScale(magnifierBaseScale * expandScale, expandDuration).SetEase(Ease.OutQuad));
            outroSequence.Append(currentMaskRect.DOScale(0f, shrinkDuration).SetEase(Ease.InBack));
            outroSequence.Join(magnifierRect.DOScale(0f, shrinkDuration).SetEase(Ease.InBack));
            if (currentMaskImage != null)
                outroSequence.Join(currentMaskImage.DOFade(0f, shrinkDuration));
            outroSequence.Join(magnifierImage.DOFade(0f, shrinkDuration));
            outroSequence.OnComplete(() =>
            {
                SetItemVisible(currentItem, true);
                DestroyMaskSlotAt(revealIndex);
                RestoreMagnifierScale();
                revealIndex++;
                PlayCurrentItem();
            });
        }

        /// <summary>
        /// 全部揭幕完成
        /// </summary>
        private void FinishAll()
        {
            isPlaying = false;
            ClearMaskSlots();
            if (maskRoot != null)
                maskRoot.gameObject.SetActive(false);
            if (magnifierRect != null)
                magnifierRect.gameObject.SetActive(false);
            SetInputBlock(false);
            ShowAllItems();
            pendingItemList.Clear();

            var callback = completeCallback;
            completeCallback = null;
            callback?.Invoke();
        }

        /// <summary>
        /// 搜索循环 浮动与呼吸同周期
        /// </summary>
        private void PlaySearchLoop()
        {
            Vector2 floatOffset = new Vector2(floatAmplitude, floatAmplitude);

            searchLoopSequence = DOTween.Sequence().SetUpdate(true);
            searchLoopSequence.SetLoops(-1, LoopType.Restart);
            searchLoopSequence.Append(
                magnifierRect
                    .DOAnchorPos(floatOffset, floatCycle)
                    .From(Vector2.zero)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(2, LoopType.Yoyo));
            searchLoopSequence.Join(
                magnifierImage
                    .DOFade(breatheAlphaMin, floatCycle)
                    .From(breatheAlphaMax)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(2, LoopType.Yoyo));
        }

        #endregion

        #region 遮罩实例

        /// <summary>
        /// 为全部待搜物品生成遮罩
        /// </summary>
        private void BuildMaskSlotsForPending()
        {
            ClearMaskSlots();

            var content = containerView != null ? containerView.ItemContent : null;
            if (content == null || maskRoot == null)
                return;

            EnsureContentGroupVisible(content);

            for (int i = 0; i < pendingItemList.Count; i++)
            {
                var itemView = pendingItemList[i];
                if (itemView == null)
                {
                    maskSlotList.Add(null);
                    continue;
                }

                var cloneGo = Instantiate(maskRoot.gameObject, content, false);
                cloneGo.name = $"LootRevealMask_{i}";
                cloneGo.SetActive(true);

                var cloneRect = cloneGo.GetComponent<RectTransform>();
                var cloneImage = cloneGo.GetComponent<Image>();
                if (cloneImage == null)
                    cloneImage = cloneGo.GetComponentInChildren<Image>(true);

                // 克隆里若还带着放大镜子节点则删掉
                StripMagnifierCopies(cloneRect);

                FitRectToItem(cloneRect, itemView);
                if (cloneImage != null)
                {
                    cloneImage.enabled = true;
                    cloneImage.raycastTarget = false;
                    Color color = cloneImage.color;
                    color.a = 1f;
                    cloneImage.color = color;
                    if (maskImage != null && maskImage.sprite != null)
                        cloneImage.sprite = maskImage.sprite;
                }

                maskSlotList.Add(new ItemMaskSlot
                {
                    ItemView = itemView,
                    MaskRect = cloneRect,
                    MaskImage = cloneImage
                });
            }
        }

        /// <summary>
        /// 删除克隆里误带的放大镜
        /// </summary>
        private void StripMagnifierCopies(RectTransform cloneRect)
        {
            if (cloneRect == null || magnifierRect == null)
                return;

            for (int i = cloneRect.childCount - 1; i >= 0; i--)
            {
                var child = cloneRect.GetChild(i);
                if (child.name == magnifierRect.name || child.name.Contains("Magnifier"))
                    Destroy(child.gameObject);
            }
        }

        /// <summary>
        /// 放大镜挂到当前遮罩中心
        /// </summary>
        private void AttachMagnifierToSlot(ItemMaskSlot slot)
        {
            if (magnifierRect == null || slot == null || slot.MaskRect == null)
                return;

            magnifierRect.SetParent(slot.MaskRect, false);
            magnifierRect.localScale = magnifierBaseScale;
            magnifierRect.localRotation = Quaternion.identity;
            magnifierRect.anchorMin = new Vector2(0.5f, 0.5f);
            magnifierRect.anchorMax = new Vector2(0.5f, 0.5f);
            magnifierRect.pivot = new Vector2(0.5f, 0.5f);
            magnifierRect.anchoredPosition = Vector2.zero;
            magnifierRect.gameObject.SetActive(true);
            magnifierRect.SetAsLastSibling();
        }

        /// <summary>
        /// 搜索层提到最前 其它遮罩仍保留
        /// </summary>
        private void BringSearchLayerToFront(ItemMaskSlot slot)
        {
            if (inputBlocker != null)
                inputBlocker.transform.SetAsLastSibling();

            // 未搜遮罩保持原序 当前遮罩与放大镜置顶
            if (slot != null && slot.MaskRect != null)
                slot.MaskRect.SetAsLastSibling();
            if (magnifierRect != null)
                magnifierRect.SetAsLastSibling();
        }

        /// <summary>
        /// 销毁指定下标遮罩实例
        /// </summary>
        private void DestroyMaskSlotAt(int index)
        {
            if (index < 0 || index >= maskSlotList.Count)
                return;

            var slot = maskSlotList[index];
            maskSlotList[index] = null;
            if (slot == null || slot.MaskRect == null)
                return;

            // 放大镜先挪走再销毁遮罩
            if (magnifierRect != null && magnifierRect.IsChildOf(slot.MaskRect))
                magnifierRect.SetParent(transform, false);

            slot.MaskRect.DOKill();
            if (slot.MaskImage != null)
                slot.MaskImage.DOKill();
            Destroy(slot.MaskRect.gameObject);
        }

        /// <summary>
        /// 清空全部遮罩实例
        /// </summary>
        private void ClearMaskSlots()
        {
            if (magnifierRect != null)
                magnifierRect.SetParent(transform, false);

            for (int i = 0; i < maskSlotList.Count; i++)
            {
                var slot = maskSlotList[i];
                if (slot == null || slot.MaskRect == null)
                    continue;

                slot.MaskRect.DOKill();
                if (slot.MaskImage != null)
                    slot.MaskImage.DOKill();
                Destroy(slot.MaskRect.gameObject);
            }

            maskSlotList.Clear();
        }

        #endregion

        #region 布局与显隐

        /// <summary>
        /// 对齐到物品同父同位置同尺寸
        /// </summary>
        private void FitRectToItem(RectTransform targetRect, ItemView itemView)
        {
            var itemRect = itemView.ItemRectTransform;
            var content = containerView != null ? containerView.ItemContent : null;
            if (targetRect == null || itemRect == null || content == null)
                return;

            targetRect.SetParent(content, false);
            targetRect.localRotation = itemRect.localRotation;
            targetRect.localScale = Vector3.one;
            targetRect.anchorMin = itemRect.anchorMin;
            targetRect.anchorMax = itemRect.anchorMax;
            targetRect.pivot = itemRect.pivot;
            targetRect.sizeDelta = ResolveItemUiSize(itemView, itemRect);
            targetRect.anchoredPosition = itemRect.anchoredPosition;
            targetRect.localPosition = itemRect.localPosition;
        }

        /// <summary>
        /// 解析物品 UI 宽高
        /// </summary>
        private Vector2 ResolveItemUiSize(ItemView itemView, RectTransform itemRect)
        {
            Vector2 size = itemRect.rect.size;
            if (size.x > 1f && size.y > 1f)
                return size;

            int cellSize = containerView != null ? containerView.GridCellSize : 100;
            if (itemView.ItemData != null)
            {
                Vector2Int dataSize = itemView.ItemData.DataSize;
                return new Vector2(
                    Mathf.Max(1, dataSize.x) * cellSize,
                    Mathf.Max(1, dataSize.y) * cellSize);
            }

            return itemRect.sizeDelta.sqrMagnitude > 1f
                ? itemRect.sizeDelta
                : new Vector2(cellSize, cellSize);
        }

        /// <summary>
        /// 准备放大镜视觉
        /// </summary>
        private void PrepareMagnifierVisual()
        {
            RestoreMagnifierScale();
            if (magnifierImage == null)
                return;

            Color magColor = magnifierImage.color;
            magColor.a = breatheAlphaMax;
            magnifierImage.color = magColor;
            magnifierImage.enabled = true;
            magnifierImage.raycastTarget = false;
        }

        /// <summary>
        /// 恢复放大镜缩放
        /// </summary>
        private void RestoreMagnifierScale()
        {
            if (magnifierRect != null)
                magnifierRect.localScale = magnifierBaseScale;
        }

        /// <summary>
        /// 确保物品层 CanvasGroup 可见
        /// </summary>
        private static void EnsureContentGroupVisible(RectTransform content)
        {
            var contentGroup = content.GetComponent<CanvasGroup>();
            if (contentGroup == null)
                return;

            contentGroup.alpha = 1f;
            contentGroup.interactable = true;
            contentGroup.blocksRaycasts = true;
        }

        /// <summary>
        /// 显示全部物品
        /// </summary>
        private void ShowAllItems()
        {
            if (containerView is null)
                containerView = GetComponent<GridContainerView>();
            if (containerView is null)
                return;

            var itemViewList = containerView.GetItemViewList();
            if (itemViewList is null)
                return;

            for (int i = 0; i < itemViewList.Count; i++)
                SetItemVisible(itemViewList[i], true);
        }

        /// <summary>
        /// 单物品显隐
        /// </summary>
        private static void SetItemVisible(ItemView itemView, bool visible)
        {
            if (itemView == null)
                return;

            var canvasGroup = GetOrAddItemCanvasGroup(itemView);
            if (canvasGroup == null)
                return;

            canvasGroup.alpha = visible ? 1f : 0f;
            canvasGroup.interactable = visible;
            canvasGroup.blocksRaycasts = visible;
        }

        /// <summary>
        /// 获取或添加物品 CanvasGroup
        /// </summary>
        private static CanvasGroup GetOrAddItemCanvasGroup(ItemView itemView)
        {
            var canvasGroup = itemView.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
                canvasGroup = itemView.gameObject.AddComponent<CanvasGroup>();
            return canvasGroup;
        }

        /// <summary>
        /// 确保全容器阻挡层
        /// </summary>
        private void EnsureInputBlocker()
        {
            if (containerView is null)
                containerView = GetComponent<GridContainerView>();
            var content = containerView != null ? containerView.ItemContent : null;
            if (content == null)
                return;

            if (inputBlocker == null)
            {
                var blockerGo = new GameObject("LootRevealInputBlocker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                blockerGo.transform.SetParent(content, false);
                inputBlocker = blockerGo.GetComponent<Image>();
                inputBlocker.color = new Color(0f, 0f, 0f, 0.02f);
            }

            var rect = inputBlocker.rectTransform;
            rect.SetParent(content, false);
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            rect.localScale = Vector3.one;
            rect.SetAsLastSibling();
        }

        /// <summary>
        /// 开关操作阻挡
        /// </summary>
        private void SetInputBlock(bool block)
        {
            if (inputBlocker == null)
                return;

            inputBlocker.gameObject.SetActive(block);
            inputBlocker.raycastTarget = block;
            if (block)
                inputBlocker.transform.SetAsLastSibling();
        }

        #endregion

        #region 工具

        /// <summary>
        /// 停止全部 Tween
        /// </summary>
        private void KillAllTweens()
        {
            if (searchLoopSequence != null && searchLoopSequence.IsActive())
                searchLoopSequence.Kill();
            searchLoopSequence = null;

            if (outroSequence != null && outroSequence.IsActive())
                outroSequence.Kill();
            outroSequence = null;

            if (maskRoot != null)
                maskRoot.DOKill();
            if (magnifierRect != null)
                magnifierRect.DOKill();
            if (maskImage != null)
                maskImage.DOKill();
            if (magnifierImage != null)
                magnifierImage.DOKill();

            for (int i = 0; i < maskSlotList.Count; i++)
            {
                var slot = maskSlotList[i];
                if (slot == null)
                    continue;
                if (slot.MaskRect != null)
                    slot.MaskRect.DOKill();
                if (slot.MaskImage != null)
                    slot.MaskImage.DOKill();
            }
        }

        /// <summary>
        /// 校验必要引用
        /// </summary>
        private bool ValidateRefs()
        {
            if (maskRoot == null || maskImage == null || magnifierRect == null || magnifierImage == null)
            {
                Debug.LogWarning($"[{name}] ContainerLootRevealMask 引用未配齐", this);
                return false;
            }

            return true;
        }

        #endregion

#if UNITY_EDITOR
        #region 编辑器

        /// <summary>
        /// 快速生成遮罩层级 图需自行拖入
        /// </summary>
        [ContextMenu("生成遮罩层级")]
        private void EditorCreateHierarchy()
        {
            containerView = GetComponent<GridContainerView>();
            var content = containerView != null ? containerView.ItemContent : null;
            var parent = content != null ? content : transform as RectTransform;

            if (parent == null)
            {
                Debug.LogWarning("找不到适配父节点 请确认容器下有 ItemContent", this);
                return;
            }

            if (maskRoot == null)
            {
                var rootGo = new GameObject("LootRevealMaskTemplate", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                maskRoot = rootGo.GetComponent<RectTransform>();
                maskRoot.SetParent(parent, false);
                maskImage = rootGo.GetComponent<Image>();
                maskImage.color = new Color(0f, 0f, 0f, 0.75f);
                maskImage.raycastTarget = false;
            }

            if (magnifierRect == null)
            {
                var magGo = new GameObject("Magnifier", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                magnifierRect = magGo.GetComponent<RectTransform>();
                magnifierRect.SetParent(transform, false);
                magnifierRect.anchorMin = new Vector2(0.5f, 0.5f);
                magnifierRect.anchorMax = new Vector2(0.5f, 0.5f);
                magnifierRect.pivot = new Vector2(0.5f, 0.5f);
                magnifierRect.sizeDelta = new Vector2(96f, 96f);
                magnifierRect.anchoredPosition = Vector2.zero;
                magnifierImage = magGo.GetComponent<Image>();
                magnifierImage.raycastTarget = false;
            }
            else if (maskRoot != null && magnifierRect.IsChildOf(maskRoot))
            {
                magnifierRect.SetParent(transform, false);
            }

            maskRoot.gameObject.SetActive(false);
            magnifierRect.gameObject.SetActive(false);
            EnsureInputBlocker();
            SetInputBlock(false);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        #endregion
#endif
    }

    /// <summary>
    /// 稀有度对应揭幕搜索时长
    /// </summary>
    [Serializable]
    public class LootRevealRarityDuration
    {
        /// <summary> 稀有度 </summary>
        [SerializeField]
        [ChineseLabel("稀有度")]
        private EItemRarity itemRarity = EItemRarity.White;

        /// <summary> 搜索时长秒 </summary>
        [SerializeField]
        [ChineseLabel("搜索时长")]
        private float searchDuration = 0.55f;

        public EItemRarity ItemRarity => itemRarity;
        public float SearchDuration => searchDuration;

        public LootRevealRarityDuration()
        {
        }

        public LootRevealRarityDuration(EItemRarity eItemRarity, float duration)
        {
            itemRarity = eItemRarity;
            searchDuration = duration;
        }
    }
}
