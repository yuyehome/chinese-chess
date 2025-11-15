// ÎÄ¼þÂ·¾¶: Assets/Scripts/_App/UI/Views/PieceSelectionButton.cs

using System;
using UnityEngine;
using UnityEngine.UI;

public class PieceSelectionButton : MonoBehaviour
{
    public Button button;
    public Image iconImage;
    public int pieceIndex { get; private set; }

    private Action<PieceSelectionButton> _onClickCallback;

    public void Initialize(int index, Sprite icon, Action<PieceSelectionButton> onClickCallback)
    {
        pieceIndex = index;
        iconImage.sprite = icon;
        _onClickCallback = onClickCallback;
        button.onClick.AddListener(OnButtonClicked);
    }

    private void OnButtonClicked()
    {
        _onClickCallback?.Invoke(this);
    }

    public void SetInteractable(bool interactable)
    {
        button.interactable = interactable;
    }

    private void OnDestroy()
    {
        button.onClick.RemoveAllListeners();
    }
}