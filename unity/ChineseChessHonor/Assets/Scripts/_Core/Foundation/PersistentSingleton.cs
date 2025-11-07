// 文件路径: Assets/Scripts/_Core/Foundation/PersistentSingleton.cs

using UnityEngine;

/// <summary>
/// 继承自Singleton<T>，增加了跨场景不销毁的功能。
/// </summary>
public abstract class PersistentSingleton<T> : Singleton<T> where T : MonoBehaviour
{
    protected override void Awake()
    {
        base.Awake(); // 执行基类的Awake逻辑，确保单例唯一性
        if (Instance == this) // 只有当自己是那个唯一的实例时，才设置为不销毁
        {
            DontDestroyOnLoad(this.gameObject);
        }
    }
}