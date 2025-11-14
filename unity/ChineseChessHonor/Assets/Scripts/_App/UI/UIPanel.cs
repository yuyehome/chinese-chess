// 文件路径: Assets/Scripts/_App/UI/UIPanel.cs

using UnityEngine;

/// <summary>
/// 所有UI面板的抽象基类。
/// 提供了UI面板生命周期的基本方法，所有具体的UI面板都应继承自此类。
/// </summary>
public abstract class UIPanel : MonoBehaviour
{
    /// <summary>
    /// 面板的初始化方法。在面板被UIManager实例化后立即调用，且仅调用一次。
    /// 适合用于获取组件引用、绑定初始事件等。
    /// </summary>
    public virtual void Setup()
    {
        // 子类可以重写此方法
    }

    /// <summary>
    /// 显示面板。
    /// </summary>
    public virtual void Show()
    {
        gameObject.SetActive(true);
    }

    /// <summary>
    /// 隐藏面板。
    /// </summary>
    public virtual void Hide()
    {
        gameObject.SetActive(false);
    }
}