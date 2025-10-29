// File: _Scripts/Core/GameManager.cs

using UnityEngine;
using System;
using System.Collections.Generic;
using FishNet; 
using FishNet.Object.Synchronizing; 

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
    private float energyRecoveryRate = 0.3f;
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

    private bool isPVPMode = false;

    private bool pvpInitialized = false; // 新增一个标志位，防止重复初始化

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;

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

        isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;

        if (isPVPMode)
        {
            Debug.Log("[System] 检测到PVP模式，正在等待GameNetworkManager就绪...");

            // 客户端和服务器都需要订阅
            GameNetworkManager.OnInstanceReady += HandlePVPInitialization;
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.IsSpawned)
            {
                HandlePVPInitialization(GameNetworkManager.Instance);
            }
        }
        else
        {
            Debug.Log("[System] 检测到单机模式，初始化PVE对战...");
            InitializeForPVE();
        }
    }


    private void OnDestroy()
    {
        // 在销毁时取消订阅，这是个好习惯
        GameNetworkManager.OnInstanceReady -= HandlePVPInitialization;
    }

    private void HandlePVPInitialization(GameNetworkManager gnm)
    {
        // 因为Host会同时是Server和Client，用一个标志位确保核心逻辑只执行一次
        if (pvpInitialized) return;
        pvpInitialized = true;

        GameNetworkManager.OnInstanceReady -= HandlePVPInitialization; // 取消订阅，防止重复执行

        Debug.Log("[GameManager] GameNetworkManager 已就绪，开始PVP初始化。");

        // --- 服务器端逻辑 ---
        if (InstanceFinder.IsServer)
        {
            Debug.Log("[Server] 服务器正在初始化游戏逻辑模块...");
            // 1. 创建逻辑控制器 (不变)
            float collisionDistanceSquared = collisionDistance * collisionDistance;
            var rtController = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, collisionDistanceSquared);
            rtController.CombatManager.OnPieceKilled += HandlePieceKilled;
            currentGameMode = rtController;

            // 2. [核心修改] 调用BoardRenderer在服务器上生成网络化的棋盘对象
            BoardRenderer.Server_SpawnBoard(CurrentBoardState);
            Debug.Log("[Server] 已命令 BoardRenderer 生成网络化棋盘。");
        }

        // --- 客户端逻辑 ---
        if (InstanceFinder.IsClient)
        {
            Debug.Log("[Client] 客户端已准备就绪，等待服务器Spawn网络对象(棋子)...");

            // 创建本地的游戏模式控制器实例，用于处理未来的输入和视觉效果
            if (currentGameMode == null)
            {
                currentGameMode = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, EnergySystem, 0);
            }

            // 客户端向服务器注册自己的信息
            if (SteamManager.Instance != null && SteamManager.Instance.IsSteamInitialized)
            {
                // 等待单例实例准备好
                if (GameNetworkManager.Instance != null)
                {
                    GameNetworkManager.Instance.CmdRegisterPlayer(SteamManager.Instance.PlayerSteamId, SteamManager.Instance.PlayerName);
                    Debug.Log("[Client] 已向服务器发送玩家注册请求。");
                }
                else
                {
                    // 这是一个理论上的边界情况，因为事件驱动，Instance应该已经就绪
                    Debug.LogError("[Client] 尝试注册玩家，但 GameNetworkManager.Instance 为空！");
                }
            }
        }
    }

    /// <summary>
    /// 初始化单机PVE模式
    /// </summary>
    private void InitializeForPVE()
    {
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

        InitializeControllers(); // 调用原有的控制器初始化方法

        BoardRenderer.RenderBoard(CurrentBoardState);

        if (currentGameMode is RealTimeModeController rtController)
        {
            rtController.InitializeRealTimeStates();
        }
    }

    private void InitializeControllers()
    {
        if (isPVPMode)
        {
            Debug.LogWarning("[GameManager] InitializeControllers 在PVP模式下被调用，这可能是个错误。PVP控制器初始化应有单独的逻辑。");
            return;
        }

        if (currentGameMode is RealTimeModeController)
        {
            PlayerInputController playerController = GetComponent<PlayerInputController>();
            if (playerController == null)
            {
                playerController = gameObject.AddComponent<PlayerInputController>();
            }
            playerController.Initialize(PlayerColor.Red, this);
            controllers.Add(PlayerColor.Red, playerController);

            // AI控制器初始化逻辑调整

            // AI控制器初始化
            IAIStrategy aiStrategy;

            switch (GameModeSelector.SelectedAIDifficulty)
            {
                case AIDifficulty.VeryHard:
                    aiStrategy = new VeryHardAIStrategy();
                    break;
                case AIDifficulty.Hard:
                    aiStrategy = new EasyAIStrategy();
                    break;
                case AIDifficulty.Easy:
                default:
                    aiStrategy = new EasyAIStrategy();
                    break;
            }
            // 采用两步初始化AI控制器
            AIController aiController = gameObject.AddComponent<AIController>();
            aiController.Initialize(PlayerColor.Black, this);
            aiController.SetupAI(aiStrategy);

            controllers.Add(PlayerColor.Black, aiController);

        }
        else if (currentGameMode is TurnBasedModeController)
        {
            TurnBasedInputController turnBasedInput = GetComponent<TurnBasedInputController>();
            if (turnBasedInput == null)
            {
                turnBasedInput = gameObject.AddComponent<TurnBasedInputController>();
            }
            turnBasedInput.Initialize(PlayerColor.Red, this);
        }
    }

    private void Update()
    {
        if (IsGameEnded) return;

        // 在PVP模式下，所有逻辑都只在服务器上Tick
        if (InstanceFinder.IsServer)
        {
            // 实时模式的Tick逻辑
            if (currentGameMode is RealTimeModeController rtController)
            {
                EnergySystem?.Tick();
                rtController.Tick();
            }
            // 如果未来有回合制网络版，在这里添加其服务器Tick逻辑
        }
        else if (!isPVPMode) // 如果是单机模式
        {
            // 原有的单机Tick逻辑
            if (GameModeSelector.SelectedMode == GameModeType.RealTime)
            {
                EnergySystem?.Tick();
                if (currentGameMode is RealTimeModeController rtController)
                {
                    rtController.Tick();
                }
            }

        }

    }

    // --- 新增一个轻量级结构体，用于在后台线程传递棋子信息 ---
    public struct SimulatedPiece
    {
        public Piece PieceData;
        public Vector2Int BoardPosition;
    }

    /// <summary>
    /// [线程安全] 从一个给定的BoardState中，获取属于指定颜色的所有棋子的(模拟)信息列表。
    /// </summary>
    public List<SimulatedPiece> GetSimulatedPiecesOfColorFromBoard(PlayerColor color, BoardState board)
    {
        var pieces = new List<SimulatedPiece>();
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece pieceData = board.GetPieceAt(pos);
                if (pieceData.Type != PieceType.None && pieceData.Color == color)
                {
                    pieces.Add(new SimulatedPiece { PieceData = pieceData, BoardPosition = pos });
                }
            }
        }
        return pieces;
    }

    #region Public Game Actions & Helpers

    // --- NEW METHOD START ---
    /// <summary>
    /// 获取棋盘上属于指定颜色的所有存活棋子的列表。
    /// 主要供AI控制器使用，以获取其可操作的单位。
    /// </summary>
    public List<PieceComponent> GetAllPiecesOfColor(PlayerColor color)
    {
        var pieces = new List<PieceComponent>();
        if (BoardRenderer == null) return pieces;

        // 遍历整个棋盘寻找棋子
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                PieceComponent pc = BoardRenderer.GetPieceComponentAt(new Vector2Int(x, y));
                if (pc != null && pc.PieceData.Color == color && pc.RTState != null && !pc.RTState.IsDead)
                {
                    pieces.Add(pc);
                }
            }
        }

        // 注意：上面的循环只包含了静止的棋子。在实时模式下，AI也应该能获取到正在移动中的己方棋子。
        // 但为了简单起见，当前AI策略是不操作移动中的棋子，所以暂时可以忽略。
        // 如果未来需要更复杂的AI（如改变移动中棋子的目标），则需要从RealTimeModeController中获取movingPieces列表并筛选。

        return pieces;
    }

    /// <summary>
    /// 从一个给定的BoardState中，获取属于指定颜色的所有棋子的(临时)PieceComponent列表。
    /// 主要供Minimax算法在模拟棋盘上使用。
    /// </summary>
    public List<PieceComponent> GetAllPiecesOfColorFromBoard(PlayerColor color, BoardState board)
    {
        var pieces = new List<PieceComponent>();
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece pieceData = board.GetPieceAt(pos);
                if (pieceData.Type != PieceType.None && pieceData.Color == color)
                {
                    // 创建一个临时的Component，只包含必要信息
                    pieces.Add(new PieceComponent { PieceData = pieceData, BoardPosition = pos });
                }
            }
        }
        return pieces;
    }


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

        // 禁用玩家和AI输入控制器
        var playerInput = GetComponent<PlayerInputController>();
        if (playerInput != null) playerInput.enabled = false;
        var turnBasedInput = GetComponent<TurnBasedInputController>();
        if (turnBasedInput != null) turnBasedInput.enabled = false;
        var aiInput = GetComponent<AIController>();
        if (aiInput != null) aiInput.enabled = false;

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