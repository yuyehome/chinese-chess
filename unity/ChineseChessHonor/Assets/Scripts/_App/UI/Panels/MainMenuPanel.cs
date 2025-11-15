// 文件路径: Assets/Scripts/_App/UI/Panels/MainMenuPanel.cs

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

        // --- 临时测试代码: 使用假数据填充玩家信息 ---
        PlayerProfile testProfile = new PlayerProfile
        {
            steamId = 123456789,
            nickname = "棋圣",
            eloRating = 1850,
            goldCoins = 123456
        };
        playerInfoView.UpdateView(testProfile);
        // --- 临时测试代码结束 ---
    }

    public override void Show()
    {
        base.Show();
        // 每次显示时可以刷新信息
        Debug.Log("[MainMenuPanel] 面板已显示。");
    }

    // --- 按钮点击事件处理 ---
    private void OnRank1v1Clicked() => Debug.Log("【1V1排位】按钮被点击");
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
    }
}