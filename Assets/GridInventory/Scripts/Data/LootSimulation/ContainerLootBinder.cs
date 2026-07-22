using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 场景容器投放绑定
    /// 打开时调用 EnsureLooted 不自动 OnEnable 刷
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

        /// <summary> 绑定的容器视图 </summary>
        private GridContainerView containerView;

        public string ContentId => contentId;
        public ELootGrade Grade => grade;
        public bool AlreadyLooted => alreadyLooted;
        public GridContainerView ContainerView => containerView;

        private void Awake()
        {
            containerView = GetComponent<GridContainerView>();
        }

        /// <summary>
        /// 确保已投放 幂等
        /// </summary>
        public bool EnsureLooted()
        {
            EnsureContainerView();
            if (containerView is null || !containerView.IsInventoryReady)
                return false;

            if (alreadyLooted)
                return true;

            // 已有存档或已有物品视为搜过
            if (containerView.HasSaveFile || HasAnyItem())
            {
                alreadyLooted = true;
                return true;
            }

            containerView.EnsureCoreGridMatchesView();
            var result = LootRuntime.TryFill(containerView, contentId, grade, false);
            alreadyLooted = true;

            if (containerView.EnablePersistence)
                containerView.TrySaveToDisk();

            Debug.Log(
                $"[{name}] EnsureLooted empty={result.WasEmptyRoll} " +
                $"candidates={result.CandidateCount} placed={result.PlacedCount} skipped={result.SkippedCount}");
            return true;
        }

        /// <summary>
        /// GM 强制清空并重投
        /// </summary>
        public LootRuntime.FillResult ForceRefill()
        {
            EnsureContainerView();
            if (containerView is null || !containerView.IsInventoryReady)
                return new LootRuntime.FillResult(false, 0, 0, 0);

            alreadyLooted = false;
            containerView.EnsureCoreGridMatchesView();
            var result = LootRuntime.TryFill(containerView, contentId, grade, true);
            alreadyLooted = true;

            if (containerView.EnablePersistence)
                containerView.TrySaveToDisk();

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
    }
}
