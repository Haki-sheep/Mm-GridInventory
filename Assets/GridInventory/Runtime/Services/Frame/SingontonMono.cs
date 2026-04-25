
using UnityEngine;

namespace MmInventory
{
    public abstract class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    // 自动查找场景中实例
                    _instance = FindObjectOfType<T>();
                    // 无实例则自动创建空节点挂载
                    if (_instance == null)
                    {
                        GameObject obj = new GameObject(typeof(T).Name + " [Singleton]");
                        _instance = obj.AddComponent<T>();
                        DontDestroyOnLoad(obj);
                    }
                }
                return _instance;
            }
        }

        protected virtual void Awake()
        {
            // 场景中存在多个实例则销毁多余的
            if (_instance == null)
            {
                _instance = this as T;
                DontDestroyOnLoad(gameObject);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}