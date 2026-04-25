using UnityEngine;
using System.Collections.Generic;

namespace MmInventory
{
    public abstract class SingletonSO_ItemRoot<T, TData> : ScriptableObject where T : SingletonSO_ItemRoot<T, TData> where TData : IItemRootData
    {
        public static T Instance { get; private set; }

        [SerializeField] public List<TData> itemList; 

        protected virtual void Initialize()
        {
            if (Instance == null)
            {
                Instance = (T)this;
                hideFlags = HideFlags.DontUnloadUnusedAsset;
            }
            else if (Instance != this)
            {
                DestroyImmediate(this);
            }
        }

        protected virtual void OnDisable()
        {
            if (Instance == this) Instance = null;
        }

        protected SingletonSO_ItemRoot() { }

        public TData GetOrDefault(int id)
        {
            return itemList.Find(i => i.ItemId == id);
        }
    }
}