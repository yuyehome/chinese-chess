// 文件路径: Assets/Scripts/_App/GameManagement/GameLoopController.cs

using System.Collections.Generic;
using UnityEngine;

public class GameLoopController : PersistentSingleton<GameLoopController>
{
    [Header("场景引用")]
    [SerializeField] private BoardView boardView;
    // UI的引用可以在这里添加，例如：[SerializeField] private HudView hudView;

    [Header("游戏设置")]
    [SerializeField] private GameModeType initialGameMode = GameModeType.RealTime_Fair;

    // 逻辑核心
    private CommandProcessor _commandProcessor;
    private GameState _localGameState; // 所有客户端和Host都有一个本地状态
    private IGameModeLogic _gameModeLogic;

    // 标记当前是否为权威端 (Host或单机)
    private bool _isAuthority = true; // 在单机模式下，我们总是权威端

    // 允许外部（如InputController）请求执行一个指令
    public void RequestProcessCommand(ICommand command)
    {
        if (_isAuthority)
        {
            _commandProcessor.ProcessCommand(command);
        }
        else
        {
            // 在网络模式下，这里会调用 INetworkService.SendCommandToServer(command);
            Debug.Log("非权威端，将发送指令到服务器...");
        }
    }

    void Start()
    {
        InitializeGame();
    }

    void FixedUpdate()
    {
        // 只有权威端才需要驱动游戏逻辑心跳
        if (_isAuthority && _commandProcessor != null)
        {
            _commandProcessor.Tick();
        }
    }

    private void InitializeGame()
    {
        _localGameState = new GameState();

        if (_isAuthority)
        {
            // 只有权威端才需要创建和设置CommandProcessor
            _commandProcessor = new CommandProcessor(_localGameState);
            _gameModeLogic = GameModeManager.CreateLogic(initialGameMode);
            _commandProcessor.SetGameMode(_gameModeLogic);

            // 订阅所有来自CommandProcessor的原子事件
            SubscribeToProcessorEvents();

            // 由GameModeLogic负责初始化游戏状态
            _gameModeLogic.Initialize(_localGameState);
            // 手动触发一次初始棋子创建
            boardView.OnPieceCreated(_localGameState.pieces);
        }

        Debug.Log($"游戏初始化完成! 模式: {initialGameMode}, 是否权威端: {_isAuthority}");
    }

    private void OnDestroy()
    {
        if (_commandProcessor != null)
        {
            UnsubscribeFromProcessorEvents();
        }
    }

    #region Event Subscription (订阅/取消订阅事件)

    private void SubscribeToProcessorEvents()
    {
        _commandProcessor.OnPieceCreated += HandlePieceCreated;
        _commandProcessor.OnPieceUpdated += HandlePieceUpdated;
        _commandProcessor.OnPieceRemoved += HandlePieceRemoved;
        _commandProcessor.OnActionPointsUpdated += HandleActionPointsUpdated;
        // ... 订阅其他事件
    }

    private void UnsubscribeFromProcessorEvents()
    {
        _commandProcessor.OnPieceCreated -= HandlePieceCreated;
        _commandProcessor.OnPieceUpdated -= HandlePieceUpdated;
        _commandProcessor.OnPieceRemoved -= HandlePieceRemoved;
        _commandProcessor.OnActionPointsUpdated -= HandleActionPointsUpdated;
        // ... 取消订阅其他事件
    }

    #endregion

    #region Event Handlers (事件处理与转发)
    // 这些处理器在单机模式下直接调用View层。
    // 在网络模式下，Host端的这些方法会调用RPC，而Client端的这些方法会被RPC调用。

    // 注意：Initialize里的初始创建比较特殊，我们单独处理。
    // 这里的OnPieceCreated主要用于游戏过程中的召唤。
    private void HandlePieceCreated(PieceData pieceData)
    {
        Debug.Log($"[Event] Piece Created: ID {pieceData.uniqueId}");
        boardView.OnPieceCreated(new Dictionary<int, PieceData> { { pieceData.uniqueId, pieceData } });
    }

    private void HandlePieceUpdated(PieceData pieceData)
    {
        Debug.Log($"[Event] Piece Updated: ID {pieceData.uniqueId}, Pos: {pieceData.position}");
        boardView.OnPieceUpdated(pieceData);
    }

    private void HandlePieceRemoved(int pieceId)
    {
        Debug.Log($"[Event] Piece Removed: ID {pieceId}");
        boardView.OnPieceRemoved(pieceId);
    }

    private void HandleActionPointsUpdated(PlayerTeam team, float newAmount)
    {
        Debug.Log($"[Event] AP Updated: Team {team}, New AP: {newAmount}");
        // 这里可以转发给UI层
        // hudView.UpdateActionPoints(team, newAmount);
    }

    #endregion
}