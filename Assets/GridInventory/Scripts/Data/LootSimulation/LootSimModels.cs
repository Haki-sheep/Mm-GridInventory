using System;
using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 稀有度权重条目
    /// </summary>
    [Serializable]
    public class LootRarityWeight
    {
        /// <summary> 稀有度 </summary>
        [SerializeField]
        private EItemRarity itemRarity = EItemRarity.White;

        /// <summary> 相对权重 </summary>
        [SerializeField]
        private int weight = 1;

        public EItemRarity ItemRarity { get => itemRarity; set => itemRarity = value; }
        public int Weight { get => weight; set => weight = value; }
    }

    /// <summary>
    /// A~D 密度档 空箱与稀有度权重
    /// </summary>
    [Serializable]
    public class LootDensityProfile
    {
        /// <summary> 空箱概率 0~1 </summary>
        [SerializeField]
        [Range(0f, 1f)]
        private float emptyChance;

        /// <summary> 白蓝紫金红权重 </summary>
        [SerializeField]
        private List<LootRarityWeight> rarityWeightList = CreateDefaultRarityWeights();

        public float EmptyChance { get => emptyChance; set => emptyChance = value; }
        public List<LootRarityWeight> RarityWeightList => rarityWeightList;

        /// <summary>
        /// 默认稀有度权重
        /// </summary>
        public static List<LootRarityWeight> CreateDefaultRarityWeights()
        {
            return new List<LootRarityWeight>
            {
                new LootRarityWeight { ItemRarity = EItemRarity.White, Weight = 60 },
                new LootRarityWeight { ItemRarity = EItemRarity.Blue, Weight = 25 },
                new LootRarityWeight { ItemRarity = EItemRarity.Purple, Weight = 10 },
                new LootRarityWeight { ItemRarity = EItemRarity.Gold, Weight = 4 },
                new LootRarityWeight { ItemRarity = EItemRarity.Red, Weight = 1 }
            };
        }
    }

    /// <summary>
    /// 容器内容方向 件数区间与允许类型
    /// </summary>
    [Serializable]
    public class LootContentTable
    {
        /// <summary> 内容方向 ID 如 冰箱 </summary>
        [SerializeField]
        private string contentId = "Default";

        /// <summary> 最少物品个数 </summary>
        [SerializeField]
        private int itemCountMin = 1;

        /// <summary> 最多物品个数 </summary>
        [SerializeField]
        private int itemCountMax = 5;

        /// <summary> 允许出现的物品类型 </summary>
        [SerializeField]
        private List<EItemType> allowedItemTypeList = new();

        public string ContentId { get => contentId; set => contentId = value; }
        public int ItemCountMin { get => itemCountMin; set => itemCountMin = value; }
        public int ItemCountMax { get => itemCountMax; set => itemCountMax = value; }
        public List<EItemType> AllowedItemTypeList => allowedItemTypeList;

        /// <summary>
        /// 是否允许该类型
        /// </summary>
        public bool AllowsItemType(EItemType eItemType)
        {
            if (allowedItemTypeList is null || allowedItemTypeList.Count == 0)
                return false;

            for (int i = 0; i < allowedItemTypeList.Count; i++)
            {
                if (allowedItemTypeList[i] == eItemType)
                    return true;
            }

            return false;
        }
    }

    /// <summary>
    /// 一次抽签得到的候选物品
    /// </summary>
    public readonly struct LootCandidate
    {
        /// <summary> 物品 Excel ID </summary>
        public readonly int ExcelItemId;

        /// <summary> 堆叠数量 </summary>
        public readonly int StackCount;

        /// <summary> 占地格数 用于排序 </summary>
        public readonly int CellArea;

        /// <summary> 稀有度 抽取侧携带 </summary>
        public readonly EItemRarity ItemRarity;

        public LootCandidate(int excelItemId, int stackCount, int cellArea, EItemRarity itemRarity)
        {
            ExcelItemId = excelItemId;
            StackCount = stackCount;
            CellArea = cellArea;
            ItemRarity = itemRarity;
        }
    }
}
