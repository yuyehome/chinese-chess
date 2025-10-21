// File: _Scripts/GameManager.cs

using UnityEngine;
using System;

/// <summary>
/// 游戏总管理器 (Singleton)，作为游戏核心逻辑的入口和协调者。
/// 负责管理游戏状态、模式切换、和核心操作的执行。
/// </summary>
public class GameManager : MonoBehaviour
{
    // 单例实例，方便全局访问
    public static GameManager Instance { get; private set; }

    // --- 核心系统引用 ---
    public BoardState CurrentBoardState { get; private set; }
    public GameModeController CurrentGameMode { get; private set; }
    public EnergySystem EnergySystem { get; private set; }

    // --- 内部模块引用 ---
    private BoardRenderer boardRenderer;
    private bool isGameEnded = false;

    private void Awake()
    {
        // 标准单例模式实现
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

        // 初始化核心数据系统
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem();

        // 根据模式选择器，实例化对应的游戏模式控制器 (策略模式)
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

        // 实时模式下，每帧驱动能量和游戏逻辑的更新
        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem?.Tick();
            if (CurrentGameMode is RealTimeModeController rtController)
            {
                rtController.Tick();
            }
        }
    }

    #region Public Game Actions

    /// <summary>
    /// 执行移动的简化接口，主要供回合制模式调用。
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        ExecuteMove(from, to, null, null);
    }

    /// <summary>
    /// 执行一次棋子移动的视觉表现和逻辑起始。
    /// 注意：此方法不再处理任何战斗判定，只负责将棋子从BoardState中“提起”，并触发视觉移动。
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to, Action<PieceComponent, float> onProgressUpdate = null, Action<PieceComponent> onComplete = null)
    {
        if (isGameEnded) return;

        // 在逻辑上将棋子从起点“提起”，使其在移动过程中不占据起始格
        CurrentBoardState.RemovePieceAt(from);

        // 触发棋盘渲染器的移动动画，并传递回调函数
        boardRenderer.MovePiece(from, to, CurrentBoardState, onProgressUpdate, onComplete);
    }

    /// <summary>
    /// 处理游戏结束的逻辑。
    /// </summary>
    public void HandleEndGame(GameStatus status)
    {
        if (isGameEnded) return; // 防止重复调用
        isGameEnded = true;
        Debug.Log($"[GameFlow] 游戏结束！结果: {status}");

        // 禁用玩家输入，防止游戏结束后继续操作
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }

    #endregion

    /// <summary>
    /// 检查并打印将军状态，目前主要在回合制模式下有提示意义。
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
}