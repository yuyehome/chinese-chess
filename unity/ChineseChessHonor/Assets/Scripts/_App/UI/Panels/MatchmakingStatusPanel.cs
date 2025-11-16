// 文件路径: Assets/Scripts/_App/UI/Panels/MatchmakingStatusPanel.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MatchmakingStatusPanel : UIPanel
{
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text statusText;

    private float _timer = 0f;

    public override void Setup()
    {
        base.Setup();
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(OnCancelClicked);
        }
        Debug.Log("[MatchmakingStatusPanel] Setup complete.");
    }

    public override void Show()
    {
        base.Show();
        _timer = 0f;
        if (statusText != null)
        {
            statusText.text = "匹配中...";
        }
        Debug.Log("[MatchmakingStatusPanel] Panel is now visible.");
    }

    public override void Hide()
    {
        base.Hide();
        Debug.Log("[MatchmakingStatusPanel] Panel is now hidden.");
    }

    private void Update()
    {
        // 只有当面板可见时才执行
        if (!IsVisible) return;

        _timer += Time.deltaTime;
        if (statusText != null)
        {
            int dotCount = Mathf.FloorToInt(_timer * 2) % 4;
            statusText.text = "匹配中" + new string('.', dotCount);
        }
    }

    private void OnCancelClicked()
    {
        Debug.Log("[MatchmakingStatusPanel] Cancel button clicked.");
        // 调用SteamLobbyManager离开Lobby
        SteamLobbyManager.Instance.LeaveLobby();
        // 隐藏自己
        UIManager.Instance.HidePanel<MatchmakingStatusPanel>();
    }

    private void OnDestroy()
    {
        if (cancelButton != null)
        {
            cancelButton.onClick.RemoveAllListeners();
        }
    }
}