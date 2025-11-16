// 文件路径: Assets/Scripts/_App/UI/Panels/MatchmakingStatusPanel.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;

// 这是一个简单的面板，继承UIPanel
public class MatchmakingStatusPanel : UIPanel
{
    [SerializeField] private Button cancelButton;
    [SerializeField] private TMP_Text statusText;

    private float _timer = 0f;

    public override void Setup()
    {
        base.Setup();
        cancelButton.onClick.AddListener(OnCancelClicked);
    }

    public override void Show()
    {
        base.Show();
        _timer = 0f;
        statusText.text = "匹配中...";
    }

    private void Update()
    {
        if (IsVisible)
        {
            _timer += Time.deltaTime;
            // 每0.5秒更新一次省略号
            int dotCount = Mathf.FloorToInt(_timer * 2) % 4;
            statusText.text = "匹配中" + new string('.', dotCount);
        }
    }

    private void OnCancelClicked()
    {
        Debug.Log("[MatchmakingStatusPanel] 取消匹配被点击。");
        // TODO: 调用SteamLobbyManager.Instance.LeaveLobby();
        UIManager.Instance.HidePanel<MatchmakingStatusPanel>();
    }

    private void OnDestroy()
    {
        cancelButton.onClick.RemoveAllListeners();
    }
}