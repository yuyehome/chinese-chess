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

    private CSteamID _localPlayerSteamId;

    public override void Setup()
    {
        base.Setup();

        // 绑定按钮事件
        rank1v1Button.onClick.AddListener(OnRank1v1Clicked);
        campaignButton.onClick.AddListener(OnCampaignClicked);
        rank2v2Button.onClick.AddListener(OnRank2v2Clicked);
        roomButton.onClick.AddListener(OnRoomClicked);
        leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        storeButton.onClick.AddListener(OnStoreClicked);

        // 订阅SteamLobbyManager的事件
        SteamLobbyManager.Instance.OnAvatarReady += OnAvatarReady;
    }

    public override void Show()
    {
        base.Show();
        // 每次显示面板时，都刷新玩家信息
        RefreshPlayerInfo();
    }

    private void RefreshPlayerInfo()
    {
        // 1. 获取并显示基本信息 (昵称、金币等)
        PlayerProfile localProfile = SteamLobbyManager.Instance.GetLocalPlayerProfile();
        playerInfoView.UpdateView(localProfile);
        _localPlayerSteamId = new CSteamID(localProfile.steamId);

        // 2. 尝试获取并显示头像
        Texture2D avatar = SteamLobbyManager.Instance.GetAvatar(_localPlayerSteamId);
        if (avatar != null)
        {
            playerInfoView.UpdateAvatar(avatar);
        }
        else
        {
            // 如果头像还未加载，这里什么都不做，等待OnAvatarReady回调
            Debug.Log("[MainMenuPanel] 头像尚未缓存，等待Steam回调...");
        }
    }

    // 当Steam加载完头像后，此事件处理器被调用
    private void OnAvatarReady(CSteamID steamId)
    {
        // 确保是当前玩家的头像，并且当前面板是激活的
        if (steamId == _localPlayerSteamId && gameObject.activeInHierarchy)
        {
            Debug.Log("[MainMenuPanel] 收到头像就绪事件，正在更新头像...");
            Texture2D avatar = SteamLobbyManager.Instance.GetAvatar(steamId);
            playerInfoView.UpdateAvatar(avatar);
        }
    }

    private void OnRank1v1Clicked()
    {
        Debug.Log("【1V1排位】按钮被点击 - 后续将在此处调用匹配逻辑");
        // 下一步的开发内容:
        // UIManager.Instance.ShowPanel<MatchmakingStatusPanel>();
        // SteamLobbyManager.Instance.FindOrCreateLobby(GameModeType.RealTime_Fair);
    }

    // ... 其他按钮点击事件 ...
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

        // 取消订阅事件
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnAvatarReady -= OnAvatarReady;
        }
    }
}