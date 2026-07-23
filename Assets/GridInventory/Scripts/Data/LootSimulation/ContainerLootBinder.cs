using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 场景容器投放绑定
    /// 打开时调用 OnContainerOpened
    /// </summary>
    [DisallowMultipleComponent]
    [RequireComponent(typeof(GridContainerView))]
    public class ContainerLootBinder : MonoBehaviour
    {
        /// <summary> 内容方向 ID </summary>
        [SerializeField]
        [ChineseLabel("内容方向ID")]
        private string contentId = "Default";

        /// <summary> 密度等级 </summary>
        [SerializeField]
        [ChineseLabel("密度等级")]
        private ELootGrade grade = ELootGrade.B;

        /// <summary> 已搜过标记 </summary>
        [SerializeField]
        [ChineseLabel("已搜过")]
        private bool alreadyLooted;

        /// <summary> 搜刮揭幕 可空 </summary>
        [SerializeField]
        [ChineseLabel("搜刮揭幕")]
        private ContainerLootRevealMask revealMask;

        /// <summary> 绑定的容器视图 </summary>
        private GridContainerView containerView;

        public string ContentId => contentId;
        public ELootGrade Grade => grade;
        public bool AlreadyLooted => alreadyLooted;
        public GridContainerView ContainerView => containerView;

        private void Awake()
        {
            containerView = GetComponent<GridContainerView>();
            EnsureRevealMask();
        }

        /// <summary>
        /// 容器被打开时调用 首次会投放并播揭幕
        /// </summary>
        public bool OnContainerOpened()
        {
            EnsureContainerView();
            EnsureRevealMask();
            if (containerView is null || !containerView.IsInventoryReady)
                return false;

            if (alreadyLooted)
            {
                revealMask?.HideImmediate();
                return true;
            }

            // 已有存档或已有物品视为搜过
            if (containerView.HasSaveFile || HasAnyItem())
            {
                alreadyLooted = true;
                revealMask?.HideImmediate();
                return true;
            }

            containerView.EnsureCoreGridMatchesView();
            var result = LootRuntime.TryFill(containerView, contentId, grade, false);
            alreadyLooted = true;

            if (containerView.EnablePersistence)
                containerView.TrySaveToDisk();

            TryPlayReveal(result, true);

            Debug.Log(
                $"[{name}] OnContainerOpened empty={result.WasEmptyRoll} " +
                $"candidates={result.CandidateCount} placed={result.PlacedCount} skipped={result.SkippedCount}");
            return true;
        }

        /// <summary>
        /// 确保已投放 等价于打开
        /// </summary>
        public bool EnsureLooted()
        {
            return OnContainerOpened();
        }

        /// <summary>
        /// GM 强制清空并重投 然后走打开揭幕
        /// </summary>
        public LootRuntime.FillResult ForceRefill()
        {
            EnsureContainerView();
            EnsureRevealMask();
            if (containerView is null || !containerView.IsInventoryReady)
                return new LootRuntime.FillResult(false, 0, 0, 0);

            alreadyLooted = false;
            revealMask?.HideImmediate();
            containerView.EnsureCoreGridMatchesView();
            var result = LootRuntime.TryFill(containerView, contentId, grade, true);
            alreadyLooted = true;

            if (containerView.EnablePersistence)
                containerView.TrySaveToDisk();

            // 模拟再次打开 强制播揭幕
            TryPlayReveal(result, true);
            return result;
        }

        /// <summary>
        /// 重置已搜过标记 不清空物品
        /// </summary>
        public void ResetLootedFlag()
        {
            alreadyLooted = false;
        }

        /// <summary>
        /// 尝试播放揭幕
        /// </summary>
        private void TryPlayReveal(LootRuntime.FillResult result, bool forceReveal)
        {
            if (revealMask is null)
                return;

            if (!forceReveal)
            {
                revealMask.HideImmediate();
                return;
            }

            if (!result.WasEmptyRoll && result.CandidateCount <= 0 && result.PlacedCount <= 0)
            {
                revealMask.HideImmediate();
                return;
            }

            revealMask.PlayReveal();
        }

        /// <summary>
        /// 容器内是否已有物品
        /// </summary>
        private bool HasAnyItem()
        {
            var itemViewList = containerView.GetItemViewList();
            return itemViewList != null && itemViewList.Count > 0;
        }

        /// <summary>
        /// 确保容器引用
        /// </summary>
        private void EnsureContainerView()
        {
            if (containerView is null)
                containerView = GetComponent<GridContainerView>();
        }

        /// <summary>
        /// 确保揭幕组件引用
        /// </summary>
        private void EnsureRevealMask()
        {
            if (revealMask is null)
                revealMask = GetComponent<ContainerLootRevealMask>();
        }
    }
}
