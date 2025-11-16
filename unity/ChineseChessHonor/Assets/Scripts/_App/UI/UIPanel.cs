// 文件路径: Assets/Scripts/_App/UI/UIPanel.cs

using UnityEngine;

/// <summary>
/// 所有UI面板的抽象基类。
/// 提供了统一的生命周期接口和基础功能。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPanel : MonoBehaviour
{
    protected CanvasGroup _canvasGroup;

    /// <summary>
    /// 获取该面板当前是否可见。
    /// </summary>
    public bool IsVisible { get; private set; }

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            // 为了健壮性，如果忘记添加CanvasGroup，就自动添加一个
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    /// <summary>
    /// 一次性初始化设置。在面板第一次被创建时由UIManager调用。
    /// 通常用于绑定按钮事件等。
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
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        IsVisible = true;
    }

    /// <summary>
    /// 隐藏面板。
    /// </summary>
    public virtual void Hide()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        // 注意：我们通常不直接SetActive(false)，因为这会停止协程。
        // 使用CanvasGroup控制显隐是更好的实践。如果确实需要禁用，可以取消下面这行注释。
        // gameObject.SetActive(false);
        IsVisible = false;
    }
}