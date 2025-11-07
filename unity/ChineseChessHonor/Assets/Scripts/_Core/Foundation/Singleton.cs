// 文件路径: Assets/Scripts/_Core/Foundation/Singleton.cs

using UnityEngine;

/// <summary>
/// 线程安全的MonoBehaviour单例基类。
/// 所有管理器类（如AudioManager, UIManager）都应继承自它。
/// </summary>
/// <typeparam name="T">要实现单例的组件类型</typeparam>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    private static T _instance;
    private static readonly object _lock = new object();

    public static T Instance
    {
        get
        {
            // 双重检查锁定，确保线程安全
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // 在场景中查找该类型的实例
                        _instance = FindObjectOfType<T>();

                        if (_instance == null)
                        {
                            // 如果场景中没有，则动态创建一个新的GameObject并挂载该组件
                            var singletonObject = new GameObject();
                            _instance = singletonObject.AddComponent<T>();
                            singletonObject.name = typeof(T).ToString() + " (Singleton)";
                        }
                    }
                }
            }
            return _instance;
        }
    }

    protected virtual void Awake()
    {
        // 确保场景中只有一个此类型的实例
        if (_instance == null)
        {
            _instance = this as T;
        }
        else if (_instance != this)
        {
            Debug.LogWarning($"场景中已存在 {typeof(T)} 的实例，将销毁重复的实例：{gameObject.name}");
            Destroy(gameObject);
        }
    }
}