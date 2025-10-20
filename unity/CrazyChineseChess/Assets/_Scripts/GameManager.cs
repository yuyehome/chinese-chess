using UnityEngine;
using System;

/// <summary>
/// 游戏总管理器，作为游戏核心逻辑的入口和协调者。
/// 负责管理游戏状态、模式切换、和核心操作的执行。
/// </summary>
public class GameManager : MonoBehaviour
{
    // 单例模式，方便全局访问
    public static GameManager Instance { get; private set; }

    // 核心数据和系统的引用
    public BoardState CurrentBoardState { get; private set; }
    public GameModeController CurrentGameMode { get; private set; }
    public EnergySystem EnergySystem { get; private set; }

    private BoardRenderer boardRenderer;
    private bool isGameEnded = false;

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
            Debug.LogError("[Error] 场景中找不到 BoardRenderer!");
            return;
        }

        // 初始化核心数据
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem();

        // 根据模式选择器，实例化对应的游戏模式控制器
        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                Debug.Log("[System] 游戏开始，已进入【传统回合制】模式。");
                break;
            case GameModeType.RealTime:
                CurrentGameMode = new RealTimeModeController(this, CurrentBoardState, boardRenderer, EnergySystem);
                Debug.Log("[System] 游戏开始，已进入【实时对战】模式。");
                break;
            default: // 备用分支，保证游戏能正常运行
                Debug.LogWarning("[Warning] 未知的游戏模式，默认进入回合制。");
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                break;
        }

        // 1. 渲染棋盘，创建所有棋子的GameObject
        boardRenderer.RenderBoard(CurrentBoardState);

        // 2. 如果是实时模式，在棋子创建完毕后，初始化它们的实时状态
        if (CurrentGameMode is RealTimeModeController rtController)
        {
            rtController.InitializeRealTimeStates();
        }
    }

    private void Update()
    {
        if (isGameEnded) return;

        // 实时模式下，每帧更新能量和游戏逻辑
        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem?.Tick();
            if (CurrentGameMode is RealTimeModeController rtController)
            {
                rtController.Tick();
            }
        }
    }

    /// <summary>
    /// 执行移动的简化接口，主要供回合制模式或不需要回调的场景使用。
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        ExecuteMove(from, to, null, null);
    }

    /// <summary>
    /// 执行一次棋子移动的完整操作，包含逻辑更新和视觉表现触发。
    /// </summary>
    /// <param name="from">起始坐标</param>
    /// <param name="to">目标坐标</param>
    /// <param name="onProgressUpdate">（实时模式用）动画过程回调</param>
    /// <param name="onComplete">（实时模式用）动画完成回调</param>
    public void ExecuteMove(Vector2Int from, Vector2Int to, Action<PieceComponent, float> onProgressUpdate = null, Action<PieceComponent> onComplete = null)
    {
        if (isGameEnded) return;

        Piece movingPieceData = CurrentBoardState.GetPieceAt(from);
        Piece targetPieceData = CurrentBoardState.GetPieceAt(to);
        bool isCapture = targetPieceData.Type != PieceType.None;

        if (isCapture)
        {
            Debug.Log($"[Combat] 发生吃子: {movingPieceData.Color}方的{movingPieceData.Type} 捕获了 {targetPieceData.Color}方的{targetPieceData.Type}。");
            boardRenderer.RemovePieceAt(to);
            if (targetPieceData.Type == PieceType.General)
            {
                // 游戏结束的终局判断
                GameStatus status = (targetPieceData.Color == PlayerColor.Black) ? GameStatus.RedWin : GameStatus.BlackWin;
                CurrentBoardState.MovePiece(from, to);
                boardRenderer.MovePiece(from, to, CurrentBoardState, true, onProgressUpdate, onComplete);
                HandleEndGame(status);
                return;
            }
        }

        // 更新棋盘数据模型
        CurrentBoardState.MovePiece(from, to);
        // 触发棋盘视觉表现
        boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture, onProgressUpdate, onComplete);

        // 检查是否将军（主要用于回合制）
        CheckForCheck();
    }

    /// <summary>
    /// 检查并打印将军状态，目前仅在回合制模式下有实际意义。
    /// </summary>
    private void CheckForCheck()
    {
        if (CurrentGameMode is TurnBasedModeController turnBasedMode)
        {
            PlayerColor nextPlayer = turnBasedMode.GetCurrentPlayer();
            if (RuleEngine.IsKingInCheck(nextPlayer, CurrentBoardState))
            {
                Debug.Log($"[GameFlow] 将军！{nextPlayer} 方的王正被攻击！");
            }
        }
    }

    /// <summary>
    /// 处理游戏结束的逻辑。
    /// </summary>
    private void HandleEndGame(GameStatus status)
    {
        isGameEnded = true;
        Debug.Log($"[GameFlow] 游戏结束！结果: {status}");

        // 禁用玩家输入，防止游戏结束后继续操作
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }
}