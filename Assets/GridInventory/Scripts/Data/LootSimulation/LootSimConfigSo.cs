using System.Collections.Generic;
using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 容器随机投放模拟配置
    /// 密度档 A~D 与 contentId 候选池
    /// </summary>
    [CreateAssetMenu(fileName = "LootSimConfig", menuName = "MmInventory/Data/Loot Sim Config")]
    public class LootSimConfigSo : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/GridInventory/SoDatas/LootSimConfig.asset";

        private static LootSimConfigSo instance;

        /// <summary> A~D 四档密度 下标对应 ELootGrade </summary>
        [SerializeField]
        private LootDensityProfile[] densityProfiles = CreateDefaultDensityProfiles();

        /// <summary> 内容方向候选表列表 </summary>
        [SerializeField]
        private List<LootContentTable> contentTableList = new();

        /// <summary> 运行时单例 </summary>
        public static LootSimConfigSo Instance => instance;

        /// <summary>
        /// 确保配置已加载
        /// </summary>
        public static LootSimConfigSo EnsureLoaded()
        {
            if (instance != null)
                return instance;

#if UNITY_EDITOR
            instance = UnityEditor.AssetDatabase.LoadAssetAtPath<LootSimConfigSo>(DefaultAssetPath);
            if (instance != null)
                return instance;
#endif

            Debug.LogError("LootSimConfigSo 未加载 请保证资产存在并被引用");
            return null;
        }

        public IReadOnlyList<LootDensityProfile> DensityProfiles => densityProfiles;

        public IReadOnlyList<LootContentTable> ContentTableList => contentTableList;

        private void OnEnable()
        {
            instance = this;
            EnsureDensityProfiles();
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;
        }

        /// <summary>
        /// 按等级取密度档
        /// </summary>
        public bool TryGetDensity(ELootGrade grade, out LootDensityProfile density)
        {
            EnsureDensityProfiles();
            int index = (int)grade;
            if (index < 0 || index >= densityProfiles.Length || densityProfiles[index] is null)
            {
                density = null;
                return false;
            }

            density = densityProfiles[index];
            return true;
        }

        /// <summary>
        /// 按 contentId 取候选表
        /// </summary>
        public bool TryGetContent(string contentId, out LootContentTable contentTable)
        {
            contentTable = null;
            if (string.IsNullOrEmpty(contentId) || contentTableList is null)
                return false;

            for (int i = 0; i < contentTableList.Count; i++)
            {
                var table = contentTableList[i];
                if (table is null)
                    continue;

                if (string.Equals(table.ContentId, contentId, System.StringComparison.Ordinal))
                {
                    contentTable = table;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// 编辑器可写内容表列表
        /// </summary>
        public List<LootContentTable> EditorContentTableList => contentTableList;

        /// <summary>
        /// 编辑器可写密度档数组
        /// </summary>
        public LootDensityProfile[] EditorDensityProfiles
        {
            get
            {
                EnsureDensityProfiles();
                return densityProfiles;
            }
        }

        /// <summary>
        /// 保证四档密度存在
        /// </summary>
        private void EnsureDensityProfiles()
        {
            if (densityProfiles != null && densityProfiles.Length == 4)
            {
                for (int i = 0; i < 4; i++)
                {
                    if (densityProfiles[i] is null)
                        densityProfiles[i] = new LootDensityProfile();

                    EnsureRarityWeightList(densityProfiles[i]);
                }

                return;
            }

            densityProfiles = CreateDefaultDensityProfiles();
        }

        /// <summary>
        /// 保证稀有度权重五色齐全
        /// </summary>
        private static void EnsureRarityWeightList(LootDensityProfile density)
        {
            if (density is null)
                return;

            var rarityWeightList = density.RarityWeightList;
            if (rarityWeightList is null)
                return;

            if (rarityWeightList.Count == 0)
            {
                rarityWeightList.AddRange(LootDensityProfile.CreateDefaultRarityWeights());
                return;
            }

            for (int rarityIndex = 0; rarityIndex <= (int)EItemRarity.Red; rarityIndex++)
            {
                var eItemRarity = (EItemRarity)rarityIndex;
                bool found = false;
                for (int i = 0; i < rarityWeightList.Count; i++)
                {
                    if (rarityWeightList[i] != null && rarityWeightList[i].ItemRarity == eItemRarity)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    rarityWeightList.Add(new LootRarityWeight
                    {
                        ItemRarity = eItemRarity,
                        Weight = eItemRarity == EItemRarity.White ? 60 : 1
                    });
                }
            }
        }

        /// <summary>
        /// 创建默认四档密度
        /// </summary>
        private static LootDensityProfile[] CreateDefaultDensityProfiles()
        {
            return new[]
            {
                new LootDensityProfile(),
                new LootDensityProfile(),
                new LootDensityProfile(),
                new LootDensityProfile()
            };
        }

#if UNITY_EDITOR
        /// <summary>
        /// 编辑器保存资产
        /// </summary>
        public void EditorSave()
        {
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssets();
        }
#endif
    }
}
