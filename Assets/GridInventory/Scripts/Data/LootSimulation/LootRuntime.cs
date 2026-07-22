using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 容器投放运行时
    /// A~D 管空箱与稀有度 内容方向管件数区间与允许类型
    /// </summary>
    public static class LootRuntime
    {
        #region 结果与预览

        /// <summary>
        /// 投放结果
        /// </summary>
        public readonly struct FillResult
        {
            /// <summary> 是否判定为空箱 </summary>
            public readonly bool WasEmptyRoll;

            /// <summary> 候选数量 </summary>
            public readonly int CandidateCount;

            /// <summary> 成功放入数量 </summary>
            public readonly int PlacedCount;

            /// <summary> 因放不下跳过数量 </summary>
            public readonly int SkippedCount;

            public FillResult(bool wasEmptyRoll, int candidateCount, int placedCount, int skippedCount)
            {
                WasEmptyRoll = wasEmptyRoll;
                CandidateCount = candidateCount;
                PlacedCount = placedCount;
                SkippedCount = skippedCount;
            }
        }

        /// <summary>
        /// 计算期望抽取次数
        /// </summary>
        public static float CalcExpectedRollCount(LootContentTable contentTable)
        {
            if (contentTable is null)
                return 0f;

            int countMin = Mathf.Max(0, contentTable.ItemCountMin);
            int countMax = Mathf.Max(countMin, contentTable.ItemCountMax);
            return (countMin + countMax) * 0.5f;
        }

        /// <summary>
        /// 计算各稀有度相对占比 0~1
        /// </summary>
        public static void CalcRarityShareDict(
            LootDensityProfile density,
            Dictionary<EItemRarity, float> outShareDict)
        {
            outShareDict.Clear();
            if (density is null)
                return;

            int totalWeight = SumRarityWeights(density.RarityWeightList);
            if (totalWeight <= 0)
                return;

            var rarityWeightList = density.RarityWeightList;
            for (int i = 0; i < rarityWeightList.Count; i++)
            {
                var entry = rarityWeightList[i];
                if (entry is null || entry.Weight <= 0)
                    continue;

                float share = (float)entry.Weight / totalWeight;
                if (outShareDict.TryGetValue(entry.ItemRarity, out float oldValue))
                    outShareDict[entry.ItemRarity] = oldValue + share;
                else
                    outShareDict[entry.ItemRarity] = share;
            }
        }

        /// <summary>
        /// 计算各稀有度期望出现次数
        /// </summary>
        public static void CalcExpectedRarityCounts(
            LootDensityProfile density,
            LootContentTable contentTable,
            Dictionary<EItemRarity, float> outExpectedDict)
        {
            outExpectedDict.Clear();
            if (density is null || contentTable is null)
                return;

            float expectedRoll = CalcExpectedRollCount(contentTable);
            CalcRarityShareDict(density, outExpectedDict);
            if (expectedRoll <= 0f || outExpectedDict.Count == 0)
            {
                outExpectedDict.Clear();
                return;
            }

            var keyList = new List<EItemRarity>(outExpectedDict.Keys);
            for (int i = 0; i < keyList.Count; i++)
            {
                var eItemRarity = keyList[i];
                outExpectedDict[eItemRarity] *= expectedRoll;
            }
        }

        /// <summary>
        /// 统计总表中类型与稀有度交叉条目数
        /// </summary>
        public static int CountTableItems(EItemType eItemType, EItemRarity eItemRarity)
        {
            var itemList = GetItemTableList();
            int count = 0;
            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                if (item != null && item.ItemType == eItemType && item.ItemRarity == eItemRarity)
                    count++;
            }

            return count;
        }

        #endregion

        #region 抽池与填充

        /// <summary> 总表筛选临时列表 </summary>
        private static readonly List<IItemTableData> tempItemList = new();

        /// <summary>
        /// 模拟候选列表 不写入容器
        /// </summary>
        public static List<LootCandidate> SimulateCandidates(
            LootSimConfigSo configSo,
            string contentId,
            ELootGrade grade,
            out bool wasEmptyRoll)
        {
            wasEmptyRoll = false;
            var candidateList = new List<LootCandidate>();
            if (configSo is null)
                return candidateList;

            if (!configSo.TryGetDensity(grade, out var density) || density is null)
                return candidateList;

            if (!configSo.TryGetContent(contentId, out var contentTable) || contentTable is null)
                return candidateList;

            if (density.EmptyChance > 0f && Random.value < density.EmptyChance)
            {
                wasEmptyRoll = true;
                return candidateList;
            }

            int countMin = Mathf.Max(0, contentTable.ItemCountMin);
            int countMax = Mathf.Max(countMin, contentTable.ItemCountMax);
            int rollCount = Random.Range(countMin, countMax + 1);
            if (rollCount <= 0)
                return candidateList;

            for (int i = 0; i < rollCount; i++)
            {
                if (!TryPickCandidate(density, contentTable, out var candidate))
                    continue;

                candidateList.Add(candidate);
            }

            return candidateList;
        }

        /// <summary>
        /// 向容器填充 放不下跳过
        /// </summary>
        public static FillResult TryFill(
            GridContainerView containerView,
            string contentId,
            ELootGrade grade,
            bool forceClear)
        {
            if (containerView is null || !containerView.IsInventoryReady)
                return new FillResult(false, 0, 0, 0);

            var configSo = LootSimConfigSo.EnsureLoaded();
            if (configSo is null)
            {
                Debug.LogWarning("LootSimConfigSo 未加载");
                return new FillResult(false, 0, 0, 0);
            }

            if (forceClear)
                containerView.ClearAllItems();

            var candidateList = SimulateCandidates(configSo, contentId, grade, out bool wasEmptyRoll);
            if (wasEmptyRoll || candidateList.Count == 0)
                return new FillResult(wasEmptyRoll, 0, 0, 0);

            candidateList.Sort(CompareCandidateByAreaDesc);

            int placedCount = 0;
            int skippedCount = 0;
            for (int i = 0; i < candidateList.Count; i++)
            {
                var candidate = candidateList[i];
                var itemView = containerView.CreatItemUIAtRandomEmpty(
                    candidate.ExcelItemId,
                    candidate.StackCount);
                if (itemView is null)
                {
                    skippedCount++;
                    continue;
                }

                placedCount++;
            }

            return new FillResult(false, candidateList.Count, placedCount, skippedCount);
        }

        #endregion

        #region 加权抽取

        /// <summary>
        /// 先抽稀有度再按允许类型从总表抽物品 无货则降稀有度
        /// </summary>
        private static bool TryPickCandidate(
            LootDensityProfile density,
            LootContentTable contentTable,
            out LootCandidate candidate)
        {
            candidate = default;
            if (!TryPickRarity(density, out var eItemRarity))
                return false;

            // 从抽中稀有度向下降级直到找到物品
            for (int step = (int)eItemRarity; step >= 0; step--)
            {
                var eTryRarity = (EItemRarity)step;
                if (!TryPickItemFromTable(contentTable, eTryRarity, out var tableData))
                    continue;

                int stackCount = ResolveStackCount(tableData);
                int cellArea = Mathf.Max(1, tableData.DataSize.x * tableData.DataSize.y);
                candidate = new LootCandidate(
                    tableData.ExcelItemId,
                    stackCount,
                    cellArea,
                    tableData.ItemRarity);
                return true;
            }

            return false;
        }

        /// <summary>
        /// 按密度权重抽稀有度
        /// </summary>
        private static bool TryPickRarity(LootDensityProfile density, out EItemRarity eItemRarity)
        {
            eItemRarity = EItemRarity.White;
            var rarityWeightList = density.RarityWeightList;
            if (rarityWeightList is null || rarityWeightList.Count == 0)
                return false;

            int totalWeight = SumRarityWeights(rarityWeightList);
            if (totalWeight <= 0)
                return false;

            int roll = Random.Range(0, totalWeight);
            int cursor = 0;
            for (int i = 0; i < rarityWeightList.Count; i++)
            {
                var entry = rarityWeightList[i];
                if (entry is null || entry.Weight <= 0)
                    continue;

                cursor += entry.Weight;
                if (roll < cursor)
                {
                    eItemRarity = entry.ItemRarity;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 从总表按允许类型与稀有度等权抽取
        /// </summary>
        private static bool TryPickItemFromTable(
            LootContentTable contentTable,
            EItemRarity eItemRarity,
            out IItemTableData tableData)
        {
            tableData = null;
            tempItemList.Clear();

            var itemList = GetItemTableList();
            for (int i = 0; i < itemList.Count; i++)
            {
                var item = itemList[i];
                if (item is null)
                    continue;

                if (item.ItemRarity != eItemRarity)
                    continue;

                if (!contentTable.AllowsItemType(item.ItemType))
                    continue;

                tempItemList.Add(item);
            }

            if (tempItemList.Count == 0)
                return false;

            int index = Random.Range(0, tempItemList.Count);
            tableData = tempItemList[index];
            return true;
        }

        /// <summary>
        /// 解析堆叠数量
        /// </summary>
        private static int ResolveStackCount(IItemTableData tableData)
        {
            if (tableData.ItemStackType == EItemStackType.NoStackable)
                return 1;

            int maxStack = Mathf.Max(1, tableData.MaxStackCount);
            return Random.Range(1, maxStack + 1);
        }

        /// <summary>
        /// 稀有度权重求和
        /// </summary>
        private static int SumRarityWeights(List<LootRarityWeight> rarityWeightList)
        {
            if (rarityWeightList is null)
                return 0;

            int total = 0;
            for (int i = 0; i < rarityWeightList.Count; i++)
            {
                var entry = rarityWeightList[i];
                if (entry != null && entry.Weight > 0)
                    total += entry.Weight;
            }

            return total;
        }

        /// <summary>
        /// 获取物品总表
        /// </summary>
        private static IReadOnlyList<ItemTableData> GetItemTableList()
        {
            var listSo = ItemTableDataListSo.Instance;
#if UNITY_EDITOR
            if (listSo is null)
            {
                listSo = UnityEditor.AssetDatabase.LoadAssetAtPath<ItemTableDataListSo>(
                    ItemTableDataListSo.DefaultAssetPath);
            }
#endif
            if (listSo is null)
                return System.Array.Empty<ItemTableData>();

            return listSo.ItemDataList;
        }

        /// <summary>
        /// 候选按占地降序
        /// </summary>
        private static int CompareCandidateByAreaDesc(LootCandidate a, LootCandidate b)
        {
            return b.CellArea.CompareTo(a.CellArea);
        }

        #endregion
    }
}
