

using UnityEngine;

namespace Utils
{
    // Generic Singleton cho MonoBehaviour không bị hủy khi đổi scene (DontDestroyOnLoad)
    public class SingletonDontDestroy<T> : MonoBehaviour where T : MonoBehaviour
    {
        public static T Instance { get; private set; }

        protected virtual void Awake()
        {
            if (Instance == null)
                Instance = this as T;
            else
                Destroy(gameObject);
            DontDestroyOnLoad(gameObject);
        }

        protected virtual void OnDestroy()
        {
            if (Instance == this)
                Instance = null;
        }
    }
}


