namespace MmInventory
{
    /// <summary>
    /// 快捷互转结果类型
    /// </summary>
    public enum EQuickMoveResultKind
    {
        /// <summary> 失败 </summary>
        Failed,

        /// <summary> 部分堆叠 源堆仍在 </summary>
        StackedPartial,

        /// <summary> 全部堆叠 源堆消失 </summary>
        StackedFull,

        /// <summary> 整件移动到目标首个空位 </summary>
        Moved,
    }

    /// <summary>
    /// 快捷互转失败原因
    /// </summary>
    public enum EQuickMoveFailReason
    {
        /// <summary> 无 </summary>
        None,

        /// <summary> 没有对面的活跃容器 </summary>
        NoTargetContainer,

        /// <summary> 容器不参与互转 </summary>
        InvalidContainerRole,

        /// <summary> 物品无效 </summary>
        ItemInvalid,

        /// <summary> 目标背包放不下 </summary>
        TargetFull,
    }

    /// <summary>
    /// 快捷互转 Core 结果
    /// </summary>
    public readonly struct QuickMoveOpResult
    {
        /// <summary> 结果类型 </summary>
        public readonly EQuickMoveResultKind Kind;

        /// <summary> 被移动的物品 </summary>
        public readonly IItemRuntime MovedItem;

        /// <summary> 堆叠目标物品 </summary>
        public readonly IItemRuntime StackTarget;

        /// <summary> 是否成功 </summary>
        public bool IsSuccess => Kind != EQuickMoveResultKind.Failed;

        /// <summary>
        /// 构造快捷互转结果
        /// </summary>
        public QuickMoveOpResult(EQuickMoveResultKind kind,
                                 IItemRuntime movedItem = null,
                                 IItemRuntime stackTarget = null)
        {
            Kind = kind;
            MovedItem = movedItem;
            StackTarget = stackTarget;
        }

        /// <summary>
        /// 构造失败结果
        /// </summary>
        public static QuickMoveOpResult Fail() => new(EQuickMoveResultKind.Failed);
    }
}
