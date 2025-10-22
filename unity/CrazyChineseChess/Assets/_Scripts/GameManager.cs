// File: _Scripts/Core/GameManager.cs

using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 游戏总管理器 (Singleton)，作为游戏核心逻辑的入口和协调者。
/// 负责管理游戏状态、模式切换、和核心操作的执行。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("游戏平衡性配置")]
    [SerializeField]
    [Tooltip("能量最大值")]
    private float maxEnergy = 4.0f;
    [SerializeField]
    [Tooltip("能量每秒恢复速率")]
    private float energyRecoveryRate = 1.0f;
    [SerializeField]
    [Tooltip("移动一次消耗的能量点数")]
    private int moveCost = 1;
    [SerializeField]
    [Tooltip("开局时的初始能量")]
    private float startEnergy = 2.0f;
    [SerializeField]
    [Tooltip("实时模式下，棋子碰撞的判定距离")]
    private float collisionDistance = 0.0175f;

    // --- 核心系统引用 ---
    public BoardState CurrentBoardState { get; private set; }
    private GameModeController currentGameMode;
    public EnergySystem EnergySystem { get; private set; }
    public BoardRenderer BoardRenderer { get; private set; }
    public bool IsGameEnded { get; private set; } = false;

    // --- 控制器管理 ---
    private Dictionary<PlayerColor, IPlayerController> controllers = new Dictionary<PlayerColor, IPlayerController>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        BoardRenderer = FindObjectOfType<BoardRenderer>();
        if (BoardRenderer == null)
        {
            Debug.LogError("[Error] 场景中找不到 BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem(maxEnergy, energyRecoveryRate, moveCost, startEnergy);

        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                currentGameMode = new TurnBasedModeController(this, CurrentBoardState, BoardRenderer);
                Debug.Log("[System] 游戏开始，已进入【传统回合制】模式。");
                break;
            case GameModeType.RealTime:
                float collisionDistanceSquared = collisionDistance * collisionDistance;
                currentGameMode = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, collisionDistanceSquared);
                ((RealTimeModeController)currentGameMode).CombatManager.OnPieceKilled += HandlePieceKilled;
                Debug.Log("[System] 游戏开始，已进入【实时对战】模式。");
                break;
            default:
                Debug.LogWarning("[Warning] 未知的游戏模式，默认进入回合制。");
                currentGameMode = new TurnBasedModeController(this, CurrentBoardState, BoardRenderer);
                break;
        }

        InitializeControllers();

        BoardRenderer.RenderBoard(CurrentBoardState);

        if (currentGameMode is RealTimeModeController rtController)
        {
            rtController.InitializeRealTimeStates();
        }
    }

    private void InitializeControllers()
    {
        if (currentGameMode is RealTimeModeController)
        {
            PlayerInputController playerController = gameObject.AddComponent<PlayerInputController>();
            playerController.Initialize(PlayerColor.Red, this);
            controllers.Add(PlayerColor.Red, playerController);
        }
        else if (currentGameMode is TurnBasedModeController)
        {
            TurnBasedInputController turnBasedInput = gameObject.AddComponent<TurnBasedInputController>();
            turnBasedInput.Initialize(PlayerColor.Red, this);
        }
    }

    private void Update()
    {
        if (IsGameEnded) return;

        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem?.Tick();
            if (currentGameMode is RealTimeModeController rtController)
            {
                rtController.Tick();
            }
        }
    }

    #region Public Game Actions & Helpers

    public void RequestMove(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        if (IsGameEnded) return;

        if (!EnergySystem.CanSpendEnergy(color))
        {
            Debug.LogWarning($"[GameManager] 来自 {color} 的移动请求被拒绝：能量不足。");
            return;
        }
        EnergySystem.SpendEnergy(color);

        if (currentGameMode is RealTimeModeController rtController)
        {
            rtController.ExecuteMoveCommand(from, to);
        }
    }

    public BoardState GetLogicalBoardState()
    {
        if (currentGameMode is RealTimeModeController rtController)
        {
            return rtController.GetLogicalBoardState();
        }
        return CurrentBoardState;
    }

    public GameModeController GetCurrentGameMode()
    {
        return currentGameMode;
    }

    public void HandleEndGame(GameStatus status)
    {
        if (IsGameEnded) return;
        IsGameEnded = true;
        Debug.Log($"[GameFlow] 游戏结束！结果: {status}");

        // 禁用玩家输入控制器
        var playerInput = GetComponent<PlayerInputController>();
        if (playerInput != null) playerInput.enabled = false;
        var turnBasedInput = GetComponent<TurnBasedInputController>();
        if (turnBasedInput != null) turnBasedInput.enabled = false;
    }

    private void HandlePieceKilled(PieceComponent killedPiece)
    {
        if (killedPiece == null) return;

        Debug.Log($"[GameManager] 收到 {killedPiece.name} 的死亡事件。");

        if (killedPiece.RTState.IsMoving)
        {
            Debug.Log($"[GameManager] 死亡的棋子 {killedPiece.name} 正在移动，直接移除其GameObject。");
            BoardRenderer.RemovePiece(killedPiece);
        }
        else
        {
            Debug.Log($"[GameManager] 死亡的棋子 {killedPiece.name} 是静止的，按坐标移除并更新模型。");
            CurrentBoardState.RemovePieceAt(killedPiece.RTState.LogicalPosition);
            BoardRenderer.RemovePieceAt(killedPiece.RTState.LogicalPosition);
        }

        if (killedPiece.PieceData.Type == PieceType.General)
        {
            Debug.Log($"[GameFlow] {killedPiece.PieceData.Type} 被击杀！游戏结束！");
            GameStatus status = (killedPiece.PieceData.Color == PlayerColor.Black)
                                ? GameStatus.RedWin
                                : GameStatus.BlackWin;
            HandleEndGame(status);
        }
    }
    #endregion
}