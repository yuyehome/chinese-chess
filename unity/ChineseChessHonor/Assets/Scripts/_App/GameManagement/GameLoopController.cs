// 文件路径: Assets/Scripts/_App/GameManagement/GameLoopController.cs (修正版)

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
    }

    public void InitializeAsHost()
    {
        Debug.Log("[GameLoopController] InitializeAsHost: 作为Host初始化游戏...");
        _localGameState = new GameState();
        _commandProcessor = new CommandProcessor(_localGameState);

        // --- 修复点：将 GameManager 修改为 GameModeManager ---
        _gameModeLogic = GameModeManager.CreateLogic(GameModeType.RealTime_Fair);
        _commandProcessor.SetGameMode(_gameModeLogic);

        SubscribeToProcessorEvents();

        _gameModeLogic.Initialize(_localGameState);
        Debug.Log($"[GameLoopController] InitializeAsHost: 逻辑状态已初始化，棋子数量: {_localGameState.pieces.Count}");

        boardView.OnPieceCreated(_localGameState.pieces);
    }

    public void InitializeAsClient(PieceData[] initialPieces)
    {
        Debug.Log($"[GameLoopController] InitializeAsClient: 作为Client初始化，收到 {initialPieces.Length} 个棋子。");
        _localGameState = new GameState();
        var dict = initialPieces.ToDictionary(p => p.uniqueId, p => p);
        _localGameState.pieces = dict;
        boardView.OnPieceCreated(dict);
    }

    // ... (文件的其余部分保持不变) ...
    void FixedUpdate()
    {
        if (IsAuthority && _commandProcessor != null)
        {
            _commandProcessor.Tick();
        }
    }

    public void RequestProcessCommand(ICommand command)
    {
        if (IsAuthority)
        {
            _commandProcessor.ProcessCommand(command);
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
    private void HandlePieceUpdated(PieceData pieceData) => NetworkEvents.Instance?.RpcOnPieceUpdated(pieceData);
    private void HandlePieceRemoved(int pieceId) => NetworkEvents.Instance?.RpcOnPieceRemoved(pieceId);
    private void HandleActionPointsUpdated(PlayerTeam team, float newAmount) => NetworkEvents.Instance?.RpcOnActionPointsUpdated(team, newAmount);
    #endregion

    #region Handlers for Network Events (Client Side)
    public void HandlePieceUpdated_FromNet(PieceData updatedPiece)
    {
        if (_localGameState == null) return;
        _localGameState.pieces[updatedPiece.uniqueId] = updatedPiece;
        boardView.OnPieceUpdated(updatedPiece);
    }
    public void HandlePieceRemoved_FromNet(int pieceId)
    {
        if (_localGameState == null) return;
        _localGameState.pieces.Remove(pieceId);
        boardView.OnPieceRemoved(pieceId);
    }
    public void HandleActionPointsUpdated_FromNet(PlayerTeam team, float newAmount)
    {
        if (_localGameState == null) return;
        _localGameState.actionPoints[(int)team] = newAmount;
    }
    #endregion
}