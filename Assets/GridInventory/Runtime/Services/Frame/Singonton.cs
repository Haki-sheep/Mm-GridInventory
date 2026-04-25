using UnityEngine;

namespace MmInventory
{
    public abstract class SingletonSO<T> : ScriptableObject where T : ScriptableObject
    {
        private static T _instance;
        public static T Instance
        {
            get => _instance;
            private set => _instance = value;
        }

        protected virtual void OnEnable()
        {
            // 只赋值第一个实例，不销毁资产（避免Unity报错）
            if (_instance == null)
            {
                Instance = this as T;
                hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
        }

        protected virtual void OnDisable()
        {
            if (Instance == this)
                Instance = null;
        }

        // 禁止手动new实例
        protected SingletonSO() { }
    }
}