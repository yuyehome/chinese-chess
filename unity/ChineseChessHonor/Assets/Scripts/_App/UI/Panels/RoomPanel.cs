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

    [Header("备战 - 动态生成配置")]
    [SerializeField] private PieceSelectionButton pieceSelectionButtonPrefab;
    [SerializeField] private Transform piecePoolContainer; // 这是 CenterPiecePool_LayoutGroup
    [SerializeField] private List<Sprite> pieceSprites; // 在Inspector中按顺序拖入车马炮象士兵的Sprite
    [SerializeField] private Transform animationLayer; // 这是 AnimationLayer

    private List<PieceSelectionButton> _activePieceButtons = new List<PieceSelectionButton>();
    private Coroutine _turnTimerCoroutine;

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
        preBattleViewCanvasGroup.gameObject.SetActive(true);

        // 先清理旧的按钮（如果重复进入）
        ClearPiecePool();
        // 动态生成棋子池
        GeneratePiecePool();

        // 渐显动画
        preBattleViewCanvasGroup.alpha = 0;
        LeanTween.alphaCanvas(preBattleViewCanvasGroup, 1f, 1f).setOnComplete(() => {
            // 动画结束后，开始第一回合
            // 这是一个临时测试，实际逻辑由网络驱动
            StartTurn("红方", 5f);
        });

        Debug.Log("[RoomPanel] 切换到备战状态。");
    }

    private void GeneratePiecePool()
    {
        for (int i = 0; i < pieceSprites.Count; i++)
        {
            PieceSelectionButton newButton = Instantiate(pieceSelectionButtonPrefab, piecePoolContainer);
            newButton.Initialize(i, pieceSprites[i], OnPieceSelected);
            _activePieceButtons.Add(newButton);
            Debug.Log("[GeneratePiecePool] ");
        }
    }

    private void ClearPiecePool()
    {
        foreach (var button in _activePieceButtons)
        {
            Destroy(button.gameObject);
        }
        _activePieceButtons.Clear();
    }

    private void OnPieceSelected(PieceSelectionButton selectedButton)
    {
        Debug.Log($"点击了棋子，索引: {selectedButton.pieceIndex}, 名称: {selectedButton.name}");

        // 禁用所有按钮，防止重复点击
        SetAllPieceButtonsInteractable(false);

        // 停止倒计时
        if (_turnTimerCoroutine != null)
        {
            StopCoroutine(_turnTimerCoroutine);
            _turnTimerCoroutine = null;
        }

        // --- 核心动画逻辑 ---
        // 1. 获取目标玩家的已选棋子容器
        //    (这里临时指定红队第一个玩家作为目标，实际由网络逻辑决定)
        Transform targetContainer = redTeamSlots[0].transform.Find("PlayerDataContainer/SelectedPiecesContainer");

        // 2. 播放飞行和消失动画
        PlayPieceFlyAnimation(selectedButton, targetContainer);
    }

    private void PlayPieceFlyAnimation(PieceSelectionButton pieceButton, Transform targetContainer)
    {
        // 1. 将按钮移动到动画层，保持其世界位置不变
        pieceButton.transform.SetParent(animationLayer, true);

        // 2. 计算目标位置 (在targetContainer中的下一个位置)
        //    这里需要一个辅助函数来计算布局后的位置
        Vector3 targetPosition = GetNextPositionInLayout(targetContainer);

        // 3. 使用LeanTween或其他工具播放动画
        float flyDuration = 1.0f;
        LeanTween.move(pieceButton.gameObject, targetPosition, flyDuration).setEase(LeanTweenType.easeInCubic);
        LeanTween.scale(pieceButton.gameObject, Vector3.one * 0.5f, flyDuration).setEase(LeanTweenType.easeInCubic).setOnComplete(() =>
        {
            // 4. 动画结束后
            // a. 在目标玩家槽位处创建一个静态的棋子图标
            //    (redTeamSlots[0].AddSelectedPiece(...))

            // b. 销毁这个飞行的棋子按钮
            Destroy(pieceButton.gameObject);

            // c. 移除它在_activePieceButtons中的引用
            _activePieceButtons.Remove(pieceButton);

            // d. 模拟开始下一回合
            StartTurn("黑方", 5f);
        });
    }

    // 辅助函数，用于模拟计算HorizontalLayoutGroup中的下一个位置
    private Vector3 GetNextPositionInLayout(Transform container)
    {
        // 这是一个简化的模拟，实际可能需要更精确的计算
        HorizontalLayoutGroup layout = container.GetComponent<HorizontalLayoutGroup>();
        int childCount = container.childCount;
        float spacing = layout.spacing;
        float childWidth = (container as RectTransform).rect.width / 5; // 假设最多放5个
        Vector3 startPos = container.position - new Vector3((childWidth + spacing) * (childCount / 2f), 0, 0);
        return startPos + new Vector3((childWidth + spacing) * childCount, 0, 0);
    }


    private void StartTurn(string turnText, float duration)
    {
        turnIndicatorText.text = $"轮到 {turnText} 选择";
        // TODO: 高亮对应玩家的SlotView

        // 激活所有剩余的棋子按钮
        SetAllPieceButtonsInteractable(true);

        // 开始倒计时
        if (_turnTimerCoroutine != null) StopCoroutine(_turnTimerCoroutine);
        _turnTimerCoroutine = StartCoroutine(TurnTimerCoroutine(duration));
    }

    private System.Collections.IEnumerator TurnTimerCoroutine(float duration)
    {
        float timer = duration;
        while (timer > 0)
        {
            timer -= Time.deltaTime;
            turnTimerText.text = timer.ToString("F1") + "s";
            yield return null;
        }
        turnTimerText.text = "0.0s";
        Debug.Log("倒计时结束，自动选择！");
        // TODO: 实现超时自动选择逻辑
        // (例如，找到_activePieceButtons中第一个按钮，并调用 OnPieceSelected(firstButton))
    }

    private void SetAllPieceButtonsInteractable(bool interactable)
    {
        foreach (var btn in _activePieceButtons)
        {
            btn.SetInteractable(interactable);
        }
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