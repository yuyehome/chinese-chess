// 文件路径: Assets/Scripts/_App/GameManagement/GameLoopController.cs

using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Mirror;

public class GameLoopController : PersistentSingleton<GameLoopController>
{
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
        _networkService = NetworkServiceProvider.Instance;

        // --- START: 临时测试代码 ---
        // 自动以单机模式启动游戏，方便测试
        if (_networkService != null && !_networkService.IsConnected)
        {
            Debug.Log("--- 自动启动单机测试模式 ---");
            NetworkServiceProvider.IsOnlineMode = false;
            _networkService.StartHost(); // 这会调用 OfflineService.StartHost
        }

        // 启动后立即播放战斗场景的背景音乐
        // 我们加一个小的延迟调用，确保AudioManager已经初始化完毕
        Invoke(nameof(PlayBattleMusic), 1.0f);
        // --- END: 临时测试代码 ---

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