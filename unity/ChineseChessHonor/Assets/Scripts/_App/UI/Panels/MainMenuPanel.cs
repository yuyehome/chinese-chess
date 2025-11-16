// 文件路径: Assets/Scripts/_App/UI/Panels/MainMenuPanel.cs

using Steamworks;
using UnityEngine;
using UnityEngine.UI;

public class MainMenuPanel : UIPanel
{
    [Header("UI 引用")]
    [SerializeField] private PlayerInfoView playerInfoView;
    [SerializeField] private Button rank1v1Button;
    [SerializeField] private Button campaignButton;
    [SerializeField] private Button rank2v2Button;
    [SerializeField] private Button roomButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button storeButton;
    [SerializeField] private Button quitButton;

    private CSteamID _localPlayerSteamId;

    public override void Setup()
    {
        base.Setup(); // 调用基类的Setup

        // 绑定按钮事件
        rank1v1Button.onClick.AddListener(OnRank1v1Clicked);
        campaignButton.onClick.AddListener(OnCampaignClicked);
        rank2v2Button.onClick.AddListener(OnRank2v2Clicked);
        roomButton.onClick.AddListener(OnRoomClicked);
        leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        storeButton.onClick.AddListener(OnStoreClicked);
        quitButton.onClick.AddListener(OnQuitClicked);

        // 订阅SteamLobbyManager的事件
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnAvatarReady += OnAvatarReady;
        }
    }

    private void OnQuitClicked()
    {
        Debug.Log("【退出游戏】按钮被点击");
        Application.Quit();

    }

    public override void Show()
    {
        base.Show();
        // 每次显示时可以刷新信息
        RefreshPlayerInfo();
        Debug.Log("[MainMenuPanel] 面板已显示。");
    }

    private void RefreshPlayerInfo()
    {
        if (SteamLobbyManager.Instance == null)
        {
            Debug.LogError("[MainMenuPanel] SteamLobbyManager.Instance is null! Cannot refresh player info.");
            return;
        }

        PlayerProfile localProfile = SteamLobbyManager.Instance.GetLocalPlayerProfile();
        if (playerInfoView != null)
        {
            playerInfoView.UpdateView(localProfile);
        }

        _localPlayerSteamId = new CSteamID(localProfile.steamId);

        Texture2D avatar = SteamLobbyManager.Instance.GetAvatar(_localPlayerSteamId);
        if (avatar != null && playerInfoView != null)
        {
            playerInfoView.UpdateAvatar(avatar);
        }
    }

    private void OnAvatarReady(CSteamID steamId)
    {
        if (steamId == _localPlayerSteamId && IsVisible)
        {
            Texture2D avatar = SteamLobbyManager.Instance.GetAvatar(steamId);
            if (playerInfoView != null)
            {
                playerInfoView.UpdateAvatar(avatar);
            }
        }
    }

    // --- 按钮点击事件处理 ---
    private void OnRank1v1Clicked()
    {
        Debug.Log("--- [MainMenuPanel] OnRank1v1Clicked: 排位按钮被点击！ ---");

        // 1. 显示“匹配中”UI
        Debug.Log("[MainMenuPanel] 步骤1: 请求UIManager显示MatchmakingStatusPanel...");
        UIManager.Instance.ShowPanel<MatchmakingStatusPanel>();

        // 2. 调用SteamLobbyManager开始寻找或创建Lobby
        Debug.Log("[MainMenuPanel] 步骤2: 请求SteamLobbyManager开始FindOrCreateLobby...");
        SteamLobbyManager.Instance.FindOrCreateLobby();
    }

    private void OnCampaignClicked() => Debug.Log("【闯关】按钮被点击");
    private void OnRank2v2Clicked() => Debug.Log("【2V2排位】按钮被点击");
    private void OnRoomClicked() => Debug.Log("【房间】按钮被点击");
    private void OnLeaderboardClicked() => Debug.Log("【排行榜】按钮被点击");
    private void OnStoreClicked() => Debug.Log("【商城】按钮被点击");

    private void OnDestroy()
    {
        // 良好习惯：在对象销毁时移除所有监听器
        rank1v1Button.onClick.RemoveAllListeners();
        campaignButton.onClick.RemoveAllListeners();
        rank2v2Button.onClick.RemoveAllListeners();
        roomButton.onClick.RemoveAllListeners();
        leaderboardButton.onClick.RemoveAllListeners();
        storeButton.onClick.RemoveAllListeners();
        quitButton.onClick.RemoveAllListeners();

        // 取消订阅事件
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnAvatarReady -= OnAvatarReady;
        }
    }
}