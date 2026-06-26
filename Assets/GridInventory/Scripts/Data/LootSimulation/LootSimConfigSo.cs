using UnityEngine;

namespace MmInventory
{
    /// <summary>
    /// 容器随机投放模拟配置
    /// 后续在此扩展随机池 权重 容器筛选等
    /// </summary>
    [CreateAssetMenu(fileName = "LootSimConfig", menuName = "MmInventory/Data/Loot Sim Config")]
    public class LootSimConfigSo : ScriptableObject
    {
        public const string DefaultAssetPath = "Assets/GridInventory/SoDatas/LootSimConfig.asset";

        private static LootSimConfigSo instance;

        /// <summary> 运行时单例 </summary>
        public static LootSimConfigSo Instance => instance;

        private void OnEnable()
        {
            instance = this;
        }

        private void OnDisable()
        {
            if (instance == this)
                instance = null;
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
