// 文件路径: Assets/Scripts/_App/UI/UIPanel.cs

using UnityEngine;

[RequireComponent(typeof(CanvasGroup))]
public abstract class UIPanel : MonoBehaviour
{
    protected CanvasGroup _canvasGroup;
    public bool IsVisible { get; private set; }

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (_canvasGroup == null)
        {
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
    }

    public virtual void Setup()
    {
        // 子类可以重写此方法
    }

    public virtual void Show()
    {
        // 1. 确保GameObject是激活的
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // 2. 设置CanvasGroup属性
        _canvasGroup.alpha = 1f;
        _canvasGroup.interactable = true;
        _canvasGroup.blocksRaycasts = true;
        IsVisible = true;
    }

    public virtual void Hide()
    {
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;
        IsVisible = false;

        // 3. 在下一帧禁用GameObject，以允许动画播放完毕（如果需要）
        // 如果没有动画，可以直接SetActive(false)
        // 为简单起见，我们直接禁用
        gameObject.SetActive(false);
    }
}