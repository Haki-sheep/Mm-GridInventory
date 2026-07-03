using System.Collections.Generic;

namespace MmInventory
{
    /// <summary>
    /// 跨容器放置操作结果
    /// </summary>
    public readonly struct CrossContainerOpResult
    {
        /// <summary> 是否操作成功 </summary>
        public readonly bool IsSuccess;

        /// <summary> 被拖拽物 A </summary>
        public readonly IItemRuntime ItemDataA;

        /// <summary> 被交换物 B </summary>
        public readonly IItemRuntime ItemDataB;

        /// <summary> 大换小时被挤开的小物品列表 </summary>
        public readonly List<IItemRuntime> DisplacedItemDataList;

        /// <summary> 交换类型 </summary>
        public readonly ESwapState SwapState;

        /// <summary>
        /// 构造跨容器操作结果
        /// </summary>
        public CrossContainerOpResult(bool isSuccess,
                                      IItemRuntime itemDataA,
                                      IItemRuntime itemDataB = null,
                                      List<IItemRuntime> displacedItemDataList = null,
                                      ESwapState swapState = ESwapState.CanNotSwap)
        {
            this.IsSuccess = isSuccess;
            this.ItemDataA = itemDataA;
            this.ItemDataB = itemDataB;
            this.DisplacedItemDataList = displacedItemDataList;
            this.SwapState = swapState;
        }

        /// <summary>
        /// 构造失败结果
        /// </summary>
        public static CrossContainerOpResult Fail(IItemRuntime itemDataA,
                                                  IItemRuntime itemDataB = null,
                                                  ESwapState swapState = ESwapState.CanNotSwap)
        {
            return new CrossContainerOpResult(false, itemDataA, itemDataB, null, swapState);
        }
    }
}
