// 文件路径: Assets/Scripts/_App/GameManagement/GameLoopController.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class GameLoopController : PersistentSingleton<GameLoopController>
{
    [Header("测试选项")]
    [SerializeField] private bool _enableStandaloneTestMode = true; // <-- 新增: 开启测试模式的开关

    [Header("场景引用")]
    [SerializeField] private BoardView boardView;

    private CommandProcessor _commandProcessor;
    private GameState _localGameState;
    private IGameModeLogic _gameModeLogic;
    private INetworkService _networkService;

    public bool IsAuthority => _networkService != null && _networkService.IsHost;

    public GameState GetCurrentState() => _localGameState;

    void Start()
    {
        // --- 新增: 独立测试模式 ---
        if (_enableStandaloneTestMode)
        {
            Debug.LogWarning("<<<<< 独立测试模式已激活 >>>>>");
            // 延迟调用以确保所有其他 Awake/Start 都已执行
            Invoke(nameof(StartStandaloneTest), 0.1f);
            return; // 阻止执行下面的网络相关代码
        }
        // --- 测试代码结束 ---

        _networkService = NetworkServiceProvider.Instance;

        // --- START: 临时测试代码 ---
        // 自动以单机模式启动游戏，方便测试
        if (_networkService != null && !_networkService.IsConnected)
        {
            Debug.Log("--- 自动启动单机测试模式 ---");
            NetworkServiceProvider.IsOnlineMode = false;
            // !! 注意：因为要修改IsOnlineMode，所以必须重置服务 !!
            NetworkServiceProvider.Reset();
            // 重新获取实例，这次会是OfflineService
            _networkService = NetworkServiceProvider.Instance;

            LocalizationManager.Instance.SetLanguage(Language.EN_US);
            //LocalizationManager.Instance.SetLanguage(Language.ZH_CN);
            _networkService.StartHostAndGame(); // <-- 修改点
        }

        // 启动后立即播放战斗场景的背景音乐
        // 我们加一个小的延迟调用，确保AudioManager已经初始化完毕
        Invoke(nameof(PlayBattleMusic), 1.0f);
        // --- END: 临时测试代码 ---
    }

    // --- 新增的测试函数 ---
    private void StartStandaloneTest()
    {
        Debug.Log("[TestMode] 开始生成测试棋局...");

        // 1. 创建一个临时的 GameState
        _localGameState = new GameState();
        _localGameState.phase = GamePhase.Playing;
        _localGameState.boardSize = new Vector2Int(9, 10);

        // 2. 手动添加几个棋子到 GameState
        int uniqueIdCounter = 0;
        var pieces = new Dictionary<int, PieceData>
        {
            // 红方
            { uniqueIdCounter++, new PieceData { uniqueId = 0, team = PlayerTeam.Red, type = PieceType.Chariot, position = new Vector2Int(0, 0), status = PieceStatus.IsAlive }},
            { uniqueIdCounter++, new PieceData { uniqueId = 1, team = PlayerTeam.Red, type = PieceType.Horse, position = new Vector2Int(1, 0), status = PieceStatus.IsAlive }},
            { uniqueIdCounter++, new PieceData { uniqueId = 2, team = PlayerTeam.Red, type = PieceType.General, position = new Vector2Int(4, 0), status = PieceStatus.IsAlive }},
            // 黑方
            { uniqueIdCounter++, new PieceData { uniqueId = 3, team = PlayerTeam.Black, type = PieceType.Chariot, position = new Vector2Int(0, 9), status = PieceStatus.IsAlive }},
            { uniqueIdCounter++, new PieceData { uniqueId = 4, team = PlayerTeam.Black, type = PieceType.Soldier, position = new Vector2Int(4, 6), status = PieceStatus.IsAlive }},
            // 队友 (如果需要测试)
            { uniqueIdCounter++, new PieceData { uniqueId = 5, team = PlayerTeam.Purple, type = PieceType.Cannon, position = new Vector2Int(7, 1), status = PieceStatus.IsAlive }},
            { uniqueIdCounter++, new PieceData { uniqueId = 6, team = PlayerTeam.Blue, type = PieceType.Elephant, position = new Vector2Int(2, 9), status = PieceStatus.IsAlive }},
        };
        _localGameState.pieces = pieces;

        Debug.Log($"[TestMode] GameState 创建完毕，包含 {pieces.Count} 个棋子。");

        // 3. 直接命令 BoardView 渲染这个 GameState
        if (boardView != null)
        {
            Debug.Log("[TestMode] 命令 BoardView 创建棋子...");
            boardView.OnPieceCreated(_localGameState.pieces);
        }
        else
        {
            Debug.LogError("[TestMode] 错误: GameLoopController 上的 BoardView 引用为空!");
        }

        Debug.Log("[TestMode] 测试棋局生成完毕。");
    }
    // --- 测试函数结束 ---


    void Update()
    {
        if (Input.GetKeyDown(KeyCode.C))
        {
            LocalizationManager.Instance.SetLanguage(Language.ZH_CN);
        }
        if (Input.GetKeyDown(KeyCode.E))
        {
            LocalizationManager.Instance.SetLanguage(Language.EN_US);
        }
    }

    private void PlayBattleMusic()
    {
        Debug.Log("--- 尝试播放战斗背景音乐 ---");
        AudioManager.Instance.PlayBGM("bgm_battle"); 
    }


    public void InitializeAsHost()
    {
        Debug.Log("[GameLoopController] InitializeAsHost: 作为Host初始化游戏...");
        _localGameState = new GameState();
        _commandProcessor = new CommandProcessor(_localGameState);
        _gameModeLogic = GameModeManager.CreateLogic(GameModeType.RealTime_Fair);
        _commandProcessor.SetGameMode(_gameModeLogic);

        SubscribeToProcessorEvents();

        _gameModeLogic.Initialize(_localGameState);
        Debug.Log($"[GameLoopController] InitializeAsHost: 逻辑状态已初始化，棋子数量: {_localGameState.pieces.Count}");

        if (boardView != null)
        {
            boardView.OnPieceCreated(_localGameState.pieces);
        }
        else
        {
            Debug.LogError("[GameLoopController] InitializeAsHost: BoardView为空，无法创建棋子视觉对象!");
        }
    }

    public void InitializeAsClient(PieceData[] initialPieces)
    {
        Debug.Log($"[GameLoopController] InitializeAsClient: 作为Client初始化，收到 {initialPieces.Length} 个棋子。");
        _localGameState = new GameState();
        var dict = initialPieces.ToDictionary(p => p.uniqueId, p => p);
        _localGameState.pieces = dict;

        if (boardView != null)
        {
            Debug.Log("[GameLoopController] InitializeAsClient: 正在通知BoardView创建棋子...");
            boardView.OnPieceCreated(dict);
        }
        else
        {
            Debug.LogError("[GameLoopController] InitializeAsClient: BoardView为空，无法创建棋子视觉对象!");
        }
    }

    void FixedUpdate()
    {
        if (IsAuthority && _commandProcessor != null)
        {
            _commandProcessor.Tick();
        }
    }

    public void RequestProcessCommand(ICommand command)
    {
        Debug.Log("[GameLoopController] RequestProcessCommand: 收到一个指令处理请求。");
        if (IsAuthority)
        {
            Debug.Log("[GameLoopController] RequestProcessCommand: 我是Host，正在处理指令...");
            _commandProcessor.ProcessCommand(command);
        }
        else
        {
            Debug.LogWarning("[GameLoopController] RequestProcessCommand: 我不是Host，忽略指令处理请求。");
        }
    }

    private void OnDestroy()
    {
        if (IsAuthority && _commandProcessor != null) UnsubscribeFromProcessorEvents();
    }

    #region Event Subscription & Forwarding (Host Only)
    private void SubscribeToProcessorEvents()
    {
        _commandProcessor.OnPieceUpdated += HandlePieceUpdated;
        _commandProcessor.OnPieceRemoved += HandlePieceRemoved;
        _commandProcessor.OnActionPointsUpdated += HandleActionPointsUpdated;
    }
    private void UnsubscribeFromProcessorEvents()
    {
        _commandProcessor.OnPieceUpdated -= HandlePieceUpdated;
        _commandProcessor.OnPieceRemoved -= HandlePieceRemoved;
        _commandProcessor.OnActionPointsUpdated -= HandleActionPointsUpdated;
    }


    private void HandlePieceUpdated(PieceData pieceData)
    {
        // 1. Host立即更新自己的视图
        if (boardView != null)
        {
            boardView.OnPieceUpdated(pieceData);
        }
        // 2. 将事件广播给所有纯客户端
        NetworkEvents.Instance?.RpcOnPieceUpdated(pieceData);
    }

    private void HandlePieceRemoved(int pieceId)
    {
        // 1. Host立即更新自己的视图
        if (boardView != null)
        {
            boardView.OnPieceRemoved(pieceId);
        }
        // 2. 将事件广播给所有纯客户端
        NetworkEvents.Instance?.RpcOnPieceRemoved(pieceId);
    }

    private void HandleActionPointsUpdated(PlayerTeam team, float newAmount)
    {
        // 注意：行动点这类UI更新，Host也需要立即响应
        // (假设你有UI逻辑来显示行动点)
        // LocalUIManager.Instance.UpdateActionPoints(team, newAmount); 

        // 将事件广播给所有纯客户端
        NetworkEvents.Instance?.RpcOnActionPointsUpdated(team, newAmount);
    }

    #endregion

    #region Handlers for Network Events (Client Side)
    public void HandlePieceUpdated_FromNet(PieceData updatedPiece)
    {
        Debug.Log($"[GameLoopController] HandlePieceUpdated_FromNet: 客户端正在更新棋子 {updatedPiece.uniqueId} 的状态。");
        if (_localGameState == null || _localGameState.pieces == null) return;
        _localGameState.pieces[updatedPiece.uniqueId] = updatedPiece;
        boardView.OnPieceUpdated(updatedPiece);
    }
    public void HandlePieceRemoved_FromNet(int pieceId)
    {
        Debug.Log($"[GameLoopController] HandlePieceRemoved_FromNet: 客户端正在移除棋子 {pieceId}。");
        if (_localGameState == null || _localGameState.pieces == null) return;
        _localGameState.pieces.Remove(pieceId);
        boardView.OnPieceRemoved(pieceId);
    }
    public void HandleActionPointsUpdated_FromNet(PlayerTeam team, float newAmount)
    {
        if (_localGameState == null || _localGameState.actionPoints == null) return;
        _localGameState.actionPoints[(int)team] = newAmount;
    }
    #endregion
}