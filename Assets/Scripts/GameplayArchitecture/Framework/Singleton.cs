using Unity.VisualScripting;
using UnityEngine;

namespace GamePlayArchitecture
{
    //创建两种类型的单例模板
    
    //纯C# 单例，无需挂载到GO上
    public class Singleton<T> where T : new()       //质检门，传进来的类型 T，必须拥有一个公开的、无参数的构造函数（也就是说，必须能被 new 出来）。
    {
        private static T _instance;                 //T类型的唯一实例，以static实现类变量
        public static T Instance => _instance ??= new T(); //需要开一个公开的（public）窗口。因为我们要拿的是静态的实体，所以这个窗口也必须是静态的（static）。
        //语法糖，与下列没区别
        /*public static T Instance
        {
            get
            {
                return ...;
            }
        }*/
        // ??= 空合并运算符：看左边这家伙（_instance）是不是空的（null）。如果是空的，就把右边的东西（new T()）造出来塞给它；如果不是空的，就什么都不做，直接把它交出去。
        // 等价于
        /*if (_instance == null)
        {
           _instance = new T();
        }
        return _instance;*/


    }

    
    //Unity单例，必须集成自MoniBehavior
    public class MonoSingleton<T> : MonoBehaviour where T : MonoBehaviour //安检门 类型 T，它自己也必须继承自 MonoBehaviour。
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

