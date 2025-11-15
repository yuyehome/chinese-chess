// 文件路径: Assets/Scripts/_App/UI/Panels/RoomPanel.cs

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class RoomPanel : UIPanel
{
    [Header("玩家槽位")]
    [SerializeField] private List<PlayerSlotView> redTeamSlots;
    [SerializeField] private List<PlayerSlotView> blackTeamSlots;

    [Header("底部操作栏")]
    [SerializeField] private GameObject bottomActionBar;
    [SerializeField] private Button startPreBattleButton; // “开始备战”按钮
    [SerializeField] private Button leaveRoomButton;

    [Header("备战 - 中央区域")]
    [SerializeField] private CanvasGroup preBattleViewCanvasGroup; // 用于整体渐显
    [SerializeField] private TMP_Text turnIndicatorText;
    [SerializeField] private TMP_Text turnTimerText;
    [SerializeField] private List<Button> pieceSelectionButtons; // 车,马,炮,象,士,兵

    [Header("游戏开始倒计时")]
    [SerializeField] private GameObject gameStartCountdownView;
    [SerializeField] private TMP_Text gameStartCountdownText;

    public override void Setup()
    {
        base.Setup();
        startPreBattleButton.onClick.AddListener(OnStartPreBattleClicked);
        leaveRoomButton.onClick.AddListener(OnLeaveRoomClicked);

        // 绑定棋子选择按钮事件
        for (int i = 0; i < pieceSelectionButtons.Count; i++)
        {
            int index = i; // 闭包陷阱
            pieceSelectionButtons[i].onClick.AddListener(() => OnPieceSelected(index));
        }
    }

    public override void Show()
    {
        base.Show();
        // 默认显示等待状态
        ShowWaitingState();
    }

    /// <summary>
    /// 切换到等待玩家状态
    /// </summary>
    public void ShowWaitingState()
    {
        bottomActionBar.SetActive(true);
        preBattleViewCanvasGroup.gameObject.SetActive(false);
        gameStartCountdownView.SetActive(false);

        Debug.Log("[RoomPanel] 切换到等待状态。");
    }

    /// <summary>
    /// 切换到备战-抢棋子状态
    /// </summary>
    public void ShowPreBattleState()
    {
        bottomActionBar.SetActive(false);
        gameStartCountdownView.SetActive(false);

        // 渐显效果
        preBattleViewCanvasGroup.gameObject.SetActive(true);
        preBattleViewCanvasGroup.alpha = 0;
        // TODO: 在后续步骤中实现1秒渐显动画 (LeanTween/DoTween/Coroutine)
        LeanTween.alphaCanvas(preBattleViewCanvasGroup, 1f, 1f);

        Debug.Log("[RoomPanel] 切换到备战状态。");
    }

    /// <summary>
    /// 切换到游戏开始倒计时状态
    /// </summary>
    public void ShowGameStartCountdownState()
    {
        bottomActionBar.SetActive(false);
        preBattleViewCanvasGroup.gameObject.SetActive(false);
        gameStartCountdownView.SetActive(true);

        Debug.Log("[RoomPanel] 切换到游戏开始倒计时状态。");
        // TODO: 实现倒计时逻辑
    }

    // --- 事件处理 ---
    private void OnStartPreBattleClicked()
    {
        Debug.Log("【开始备战】按钮被点击");
        // 临时测试
        ShowPreBattleState();
    }

    private void OnLeaveRoomClicked() => Debug.Log("【退出房间】按钮被点击");

    private void OnPieceSelected(int pieceIndex)
    {
        Debug.Log($"选择了棋子，索引: {pieceIndex}");
        pieceSelectionButtons[pieceIndex].interactable = false; // 示例：点击后禁用
    }

    private void OnDestroy()
    {
        startPreBattleButton.onClick.RemoveAllListeners();
        leaveRoomButton.onClick.RemoveAllListeners();
        foreach (var btn in pieceSelectionButtons) btn.onClick.RemoveAllListeners();
    }
}