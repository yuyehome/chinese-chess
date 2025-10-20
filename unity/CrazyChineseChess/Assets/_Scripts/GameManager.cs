// File: _Scripts/GameManager.cs
using UnityEngine;

/// <summary>
/// 【已修正】游戏总管理器，终局判断逻辑已更新以符合GDD。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public BoardState CurrentBoardState { get; private set; }
    public GameModeController CurrentGameMode { get; private set; }

    public EnergySystem EnergySystem { get; private set; }

    private BoardRenderer boardRenderer;
    private bool isGameEnded = false; // 新增一个标志位，防止游戏结束后还能继续操作

    public bool IsAnimating { get; private set; } = false; // 【新增】动画状态锁

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        boardRenderer = FindObjectOfType<BoardRenderer>();
        if (boardRenderer == null)
        {
            Debug.LogError("场景中找不到 BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();

        // 【修正】只有在实时模式下才需要创建EnergySystem
        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem = new EnergySystem();
        }

        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                Debug.Log("游戏开始，已进入【传统回合制】模式。");
                break;
            case GameModeType.RealTime:
                // 【关键修正】确保在这里将已经创建的 EnergySystem 实例传递进去
                CurrentGameMode = new RealTimeModeController(this, CurrentBoardState, boardRenderer, EnergySystem);
                Debug.Log("游戏开始，已进入【实时对战】模式。");
                break;
            default:
                Debug.LogError("未知的游戏模式！默认进入回合制。");
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                break;
        }

        boardRenderer.RenderBoard(CurrentBoardState);
    }


    // Update方法来驱动能量系统
    private void Update()
    {
        // 只有在实时模式下才更新能量
        if (GameModeSelector.SelectedMode == GameModeType.RealTime && !isGameEnded)
        {
            EnergySystem?.Tick();

            // (可选) 在这里打印能量值用于调试
            // Debug.Log($"Red Energy: {EnergySystem.GetEnergyInt(PlayerColor.Red)}, Black Energy: {EnergySystem.GetEnergyInt(PlayerColor.Black)}");
        }
    }

    /// <summary>
    /// 【已修正】执行移动操作，并在移动后检查将军和游戏结束（吃掉将/帅）。
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        if (IsAnimating) return;
        if (isGameEnded) return;

        // 1. 【核心改动】在所有操作之前，先判断这次移动是否是吃子
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        bool isCapture = targetPiece.Type != PieceType.None;

        // 2. 如果是吃子，处理终局判断和视觉移除
        if (isCapture)
        {
            boardRenderer.RemovePieceAt(to);
            if (targetPiece.Type == PieceType.General)
            {
                GameStatus status = (targetPiece.Color == PlayerColor.Black) ? GameStatus.RedWin : GameStatus.BlackWin;
                CurrentBoardState.MovePiece(from, to);
                // 【修改】将 isCapture 信息传递过去
                boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture);
                HandleEndGame(status);
                return;
            }
        }

        // 3. 更新数据层
        CurrentBoardState.MovePiece(from, to);

        // 4. 【修改】调用视觉移动，并明确告知它这是否是一次吃子
        boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture);

        // 5. 检查将军
        CheckForCheck();
    }

    /// <summary>
    /// 【新增】公共方法，用于从外部（如BoardRenderer）设置动画状态。
    /// </summary>
    public void SetAnimating(bool isAnimating)
    {
        this.IsAnimating = isAnimating;
    }


    /// <summary>
    /// 【已修正】只检查将军状态，不判断游戏结束。
    /// </summary>
    private void CheckForCheck()
    {
        // 在回合制模式下，检查下一个行动方是否被将军
        if (CurrentGameMode is TurnBasedModeController turnBasedMode)
        {
            PlayerColor nextPlayer = turnBasedMode.GetCurrentPlayer();
            if (RuleEngine.IsKingInCheck(nextPlayer, CurrentBoardState))
            {
                Debug.Log($"将军！{nextPlayer} 方的王正被攻击！");
                // TODO: 在这里播放“将军”音效，并显示将军UI提示
            }
        }
        // TODO: 在实时模式下，可能需要同时检查双方是否被将军
    }

    /// <summary>
    /// 处理游戏结束的逻辑。
    /// </summary>
    private void HandleEndGame(GameStatus status)
    {
        isGameEnded = true; // 设置游戏结束标志
        Debug.Log($"游戏结束！结果: {status}");
        // TODO: 在这里播放“胜利/失败/和棋”音效，并显示游戏结束面板

        // 禁用玩家输入
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }
}