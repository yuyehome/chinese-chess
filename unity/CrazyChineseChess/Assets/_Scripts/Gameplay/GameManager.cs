// File: _Scripts/Gameplay/GameManager.cs

using UnityEngine;
using System;
using System.Collections.Generic;
using FishNet;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using Steamworks;
using System;
using FishNet.Managing.Server;

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

    public BoardRenderer BoardRenderer { get; private set; }
    public bool IsGameEnded { get; private set; } = false;

    // --- 控制器管理 ---
    private Dictionary<PlayerColor, IPlayerController> controllers = new Dictionary<PlayerColor, IPlayerController>();

    private bool isPVPMode = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        // isPVPMode的判断应在Awake中完成
        isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;
    }

    void Start()
    {
        BoardRenderer = BoardRenderer.Instance;
        if (BoardRenderer == null)
        {
            Debug.LogError("[Error] 场景中找不到 BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();

        // 初始化能量（服务器端）
        if (InstanceFinder.IsServer && GameNetworkManager.Instance != null)
        {
            GameNetworkManager.Instance.redPlayerEnergy.Value = startEnergy;
            GameNetworkManager.Instance.blackPlayerEnergy.Value = startEnergy;
        }

        if (isPVPMode)
        {
            Debug.Log("[GameManager] PVP模式已检测。正在订阅GameNetworkManager的启动事件...");
            // 订阅来自GNM的事件，它将在服务器和客户端各自准备好时触发
            GameNetworkManager.OnNetworkStart += HandleNetworkStart;
            GameNetworkManager.OnLocalPlayerDataReceived += InitializeLocalPlayerController;
        }
        else
        {
            Debug.Log("[System] 检测到单机模式，初始化PVE对战...");
            InitializeForPVE();
        }
    }
    /// <summary>
    /// 服务器端能量恢复逻辑
    /// </summary>[Server]
    private void Server_UpdateEnergy()
    {
        // 添加变化检测，避免不必要的SyncVar更新
        if (GameNetworkManager.Instance.redPlayerEnergy.Value < maxEnergy)
        {
            float newRedEnergy = GameNetworkManager.Instance.redPlayerEnergy.Value + energyRecoveryRate * Time.deltaTime;
            newRedEnergy = Mathf.Min(newRedEnergy, maxEnergy);

            // 只有值真正变化时才设置，减少SyncVar触发
            if (Mathf.Abs(newRedEnergy - GameNetworkManager.Instance.redPlayerEnergy.Value) > 0.01f)
            {
                GameNetworkManager.Instance.redPlayerEnergy.Value = newRedEnergy;
            }
        }

        if (GameNetworkManager.Instance.blackPlayerEnergy.Value < maxEnergy)
        {
            float newBlackEnergy = GameNetworkManager.Instance.blackPlayerEnergy.Value + energyRecoveryRate * Time.deltaTime;
            newBlackEnergy = Mathf.Min(newBlackEnergy, maxEnergy);

            if (Mathf.Abs(newBlackEnergy - GameNetworkManager.Instance.blackPlayerEnergy.Value) > 0.01f)
            {
                GameNetworkManager.Instance.blackPlayerEnergy.Value = newBlackEnergy;
            }
        }
    }

    // 在 GameManager.cs 中添加测试方法
    [Server]
    public void TestEnergySystem()
    {
        if (!InstanceFinder.IsServer) return;

        Debug.Log($"[Test] 红方能量: {GameNetworkManager.Instance.redPlayerEnergy.Value}");
        Debug.Log($"[Test] 黑方能量: {GameNetworkManager.Instance.blackPlayerEnergy.Value}");

        // 手动触发能量消耗
        SpendEnergy(PlayerColor.Red);
        Debug.Log($"[Test] 消耗红方能量后: {GameNetworkManager.Instance.redPlayerEnergy.Value}");
    }

    // 添加能量访问方法
    public float GetEnergy(PlayerColor player)
    {
        if (GameNetworkManager.Instance != null)
        {
            if (player == PlayerColor.Red)
                return GameNetworkManager.Instance.redPlayerEnergy.Value;
            else if (player == PlayerColor.Black)
                return GameNetworkManager.Instance.blackPlayerEnergy.Value;
        }
        return 0;
    }

    [Server]
    public void SpendEnergy(PlayerColor player)
    {
        if (GameNetworkManager.Instance != null && InstanceFinder.IsServer)
        {
            if (player == PlayerColor.Red)
            {
                GameNetworkManager.Instance.redPlayerEnergy.Value -= moveCost;
                GameNetworkManager.Instance.redPlayerEnergy.Value = Mathf.Max(GameNetworkManager.Instance.redPlayerEnergy.Value, 0);
            }
            else if (player == PlayerColor.Black)
            {
                GameNetworkManager.Instance.blackPlayerEnergy.Value -= moveCost;
                GameNetworkManager.Instance.blackPlayerEnergy.Value = Mathf.Max(GameNetworkManager.Instance.blackPlayerEnergy.Value, 0);
            }
        }
    }


    /// <summary>
    /// 检查玩家是否有足够能量
    /// </summary>
    public bool CanSpendEnergy(PlayerColor player)
    {
        return GetEnergy(player) >= moveCost;
    }

    private void OnDestroy()
    {
        // 确保取消订阅所有事件
        GameNetworkManager.OnNetworkStart -= HandleNetworkStart;
        GameNetworkManager.OnLocalPlayerDataReceived -= InitializeLocalPlayerController;
    }

    private void HandleNetworkStart(bool isServer)
    {
        Debug.Log($"[GameManager] 接到网络启动通知. IsServer: {isServer}");
        if (isServer)
        {
            // 服务器端的初始化
            if (currentGameMode != null) return;
            Debug.Log("[GameManager-Server] 正在初始化服务器端游戏模式...");
            float collisionDistanceSquared = collisionDistance * collisionDistance;
            var rtController = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, collisionDistanceSquared);
            rtController.CombatManager.OnPieceKilled += HandlePieceKilled;
            currentGameMode = rtController;

            // 命令GNM生成棋盘
            if (GameNetworkManager.Instance != null)
            {
                GameNetworkManager.Instance.Server_InitializeBoard(CurrentBoardState);
            }
        }
        else
        {
            // 客户端端的初始化
            if (currentGameMode != null) return;
            Debug.Log("[GameManager-Client] 正在为客户端初始化游戏模式...");
            currentGameMode = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, 0);
        }
    }


    /// <summary>
    /// 当从服务器接收到本地玩家的数据后，此方法被调用。
    /// </summary>
    public void InitializeLocalPlayerController(PlayerNetData localPlayerData)
    {
        if (controllers.ContainsKey(localPlayerData.Color))
        {
            Debug.LogWarning($"[DIAG-5B] Controller for {localPlayerData.Color} already exists. Aborting.");
            return;
        }

        PlayerInputController playerController = GetComponent<PlayerInputController>();
        if (playerController == null)
        {
            playerController = gameObject.AddComponent<PlayerInputController>();
        }
        else
        {
        }

        if (playerController != null)
        {
            playerController.Initialize(localPlayerData.Color, this);
            controllers.Add(localPlayerData.Color, playerController);

            // 如果是黑方，旋转相机
            if (localPlayerData.Color == PlayerColor.Black)
            {
                Debug.Log("[Client Setup] 检测到本地玩家为黑方，正在调整视角...");
                Camera mainCamera = Camera.main;
                if (mainCamera != null)
                {
                    mainCamera.transform.rotation = Quaternion.Euler(0, 180f, 0);
                }
            }
        }
        else
        {
            Debug.LogError("[DIAG-5G-ERROR] FATAL: playerController is NULL after get/add! Cannot initialize.");
        }
    }

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
                currentGameMode = new RealTimeModeController(this, CurrentBoardState, BoardRenderer, collisionDistanceSquared);
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
            if (playerController == null) playerController = gameObject.AddComponent<PlayerInputController>();
            playerController.Initialize(PlayerColor.Red, this);
            controllers.Add(PlayerColor.Red, playerController);

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
            AIController aiController = gameObject.AddComponent<AIController>();
            aiController.Initialize(PlayerColor.Black, this);
            aiController.SetupAI(aiStrategy);
            controllers.Add(PlayerColor.Black, aiController);
        }
        else if (currentGameMode is TurnBasedModeController)
        {
            TurnBasedInputController turnBasedInput = GetComponent<TurnBasedInputController>();
            if (turnBasedInput == null) turnBasedInput = gameObject.AddComponent<TurnBasedInputController>();
            turnBasedInput.Initialize(PlayerColor.Red, this);
        }
    }

    // 在Update方法中更新能量恢复
    private void Update()
    {
        if (IsGameEnded) return;

        if (InstanceFinder.IsServer && GameNetworkManager.Instance != null)
        {
            if (currentGameMode is RealTimeModeController rtController)
            {
                // 能量恢复逻辑
                if (GameNetworkManager.Instance.redPlayerEnergy.Value < maxEnergy)
                {
                    GameNetworkManager.Instance.redPlayerEnergy.Value += energyRecoveryRate * Time.deltaTime;
                    GameNetworkManager.Instance.redPlayerEnergy.Value = Mathf.Min(GameNetworkManager.Instance.redPlayerEnergy.Value, maxEnergy);
                }

                if (GameNetworkManager.Instance.blackPlayerEnergy.Value < maxEnergy)
                {
                    GameNetworkManager.Instance.blackPlayerEnergy.Value += energyRecoveryRate * Time.deltaTime;
                    GameNetworkManager.Instance.blackPlayerEnergy.Value = Mathf.Min(GameNetworkManager.Instance.blackPlayerEnergy.Value, maxEnergy);
                }

                rtController.Tick();
            }
        }
    }

    public struct SimulatedPiece
    {
        public Piece PieceData;
        public Vector2Int BoardPosition;
    }

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

    public List<PieceComponent> GetAllPiecesOfColor(PlayerColor color)
    {
        var pieces = new List<PieceComponent>();
        if (BoardRenderer == null) return pieces;

        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                PieceComponent pc = BoardRenderer.GetPieceComponentAt(new Vector2Int(x, y));
                if (pc != null && pc.Color.Value == color && pc.RTState != null && !pc.RTState.IsDead)
                {
                    pieces.Add(pc);
                }
            }
        }
        return pieces;
    }

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
                    var tempPc = new GameObject("TempPiece").AddComponent<PieceComponent>();
                    if (tempPc.PieceData.Color == color)
                    {
                        pieces.Add(tempPc);
                    }
                    Destroy(tempPc.gameObject);
                }
            }
        }
        return pieces;
    }

    /// <summary>
    /// [Client-Side Entry] 供客户端的输入控制器调用，用于发起一个移动请求。
    /// </summary>
    public void Client_RequestMove(Vector2Int from, Vector2Int to)
    {
        if (GameNetworkManager.Instance != null)
        {
            Debug.Log($"[Client] 发送移动请求到服务器: 从 {from} 到 {to}");
            GameNetworkManager.Instance.CmdRequestMove(from, to);
        }
        else
        {
            Debug.LogError("[Client] Client_RequestMove 无法找到 GameNetworkManager.Instance！");
        }
    }

    /// <summary>
    /// [Server-Side Logic] 服务器处理经过验证的移动请求。
    /// </summary>
    public void Server_ProcessMoveRequest(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        // 关键保护：确保此逻辑只在服务器上运行
        if (!InstanceFinder.IsServer) return;

        if (IsGameEnded) return;

        // 使用新的能量检查方法
        if (!CanSpendEnergy(color))
        {
            Debug.LogWarning($"[Server] 玩家 {color} 的移动请求被拒绝：能量不足。");
            return;
        }
        SpendEnergy(color); // 使用新的能量消耗方法

        // 将移动指令交给实时模式控制器执行
        if (currentGameMode is RealTimeModeController rtController)
        {
            Debug.Log($"[Server] 验证通过，正在执行玩家 {color} 的移动: 从 {from} 到 {to}");
            PieceComponent pieceToMove = rtController.ExecuteMoveCommand(from, to);

            // 如果成功执行了移动逻辑，则命令该棋子在所有客户端上播放动画
            if (pieceToMove != null)
            {
                pieceToMove.Observer_PlayMoveAnimation(from, to);
            }
        }
    }

    /// <summary>
    /// [Local/Single-Player Logic] 单机模式下处理移动请求
    /// </summary>
    private void Local_ProcessMoveRequest(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        if (IsGameEnded) return;

        if (!CanSpendEnergy(color))
        {
            Debug.LogWarning($"[GameManager] 来自 {color} 的移动请求被拒绝：能量不足。");
            return;
        }
        SpendEnergy(color);

        if (currentGameMode is RealTimeModeController rtController)
        {
            PieceComponent pieceToMove = rtController.ExecuteMoveCommand(from, to);

            if (pieceToMove != null)
            {
                pieceToMove.Observer_PlayMoveAnimation(from, to);
            }
        }
    }

    /// <summary>
    /// [统一入口] 请求移动棋子
    /// </summary>
    public void RequestMove(PlayerColor color, Vector2Int from, Vector2Int to)
    {
        if (isPVPMode)
        {
            Client_RequestMove(from, to);
        }
        else
        {
            Local_ProcessMoveRequest(color, from, to);
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

        if (killedPiece.Type.Value == PieceType.General)
        {
            Debug.Log($"[GameFlow] {killedPiece.Type.Value} 被击杀！游戏结束！");
            GameStatus status = (killedPiece.Color.Value == PlayerColor.Black)
                                ? GameStatus.RedWin
                                : GameStatus.BlackWin;
            HandleEndGame(status);
        }
    }
    #endregion
}