using Unity.VisualScripting;
using UnityEngine;

namespace GamePlayArchitecture
{
    public class Singleton<T> where T : new()
    {
        private static T _instance;
        public static T Instance => _instance ??= new T();
    }

    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour
    {
        private static T _instance;
        private static bool _isApplicationQuitting = false;

        public static T Instance
        {
            get
            {
                // 应用退出时，禁止创建新实例
                if (_isApplicationQuitting)
                {
                    return null;
                }

                if (_instance == null)
                {
                    // 查找场景中是否已存在实例
                    _instance = FindObjectOfType<T>();

                    if (_instance == null)
                    {
                        GameObject newGO = new GameObject(typeof(T).Name);
                        _instance = newGO.AddComponent<T>();
                    }
                }
                return _instance;
            }
        }

        protected virtual void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        // 在应用退出时设置标记
        protected virtual void OnApplicationQuit()
        {
            _isApplicationQuitting = true;
        }
    }

}

