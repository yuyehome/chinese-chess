// 文件路径: Assets/Scripts/_App/GameManagement/GameLoopController.cs

using UnityEngine;

public class GameLoopController : PersistentSingleton<GameLoopController>
{
    [Header("场景引用")]
    [SerializeField] private BoardView boardView;
    [SerializeField] private InputController inputController;

    [Header("游戏设置")]
    [SerializeField] private GameModeType initialGameMode = GameModeType.RealTime_Fair;

    private CommandProcessor _commandProcessor;
    private GameState _currentGameState;
    private IGameModeLogic _gameModeLogic;

    // 允许外部（如InputController）发送指令
    public void RequestProcessCommand(ICommand command)
    {
        _commandProcessor.ProcessCommand(command);
    }

    void Start()
    {
        InitializeGame();
    }

    void FixedUpdate()
    {
        // 游戏逻辑的心跳，以固定的时间间隔驱动
        if (_commandProcessor != null)
        {
            _commandProcessor.Tick();
        }
    }

    private void InitializeGame()
    {
        // 1. 创建核心系统实例
        _currentGameState = new GameState();
        _commandProcessor = new CommandProcessor(_currentGameState);
        _gameModeLogic = GameModeManager.CreateLogic(initialGameMode);

        // 2. 注入依赖
        _commandProcessor.SetGameMode(_gameModeLogic);

        // 3. 初始化游戏状态 (由GameModeLogic负责)
        _gameModeLogic.Initialize(_currentGameState);

        // 4. 连接事件：当逻辑状态更新时，通知视图更新
        _commandProcessor.OnGameStateUpdated += boardView.OnGameStateUpdated;

        // 5. 手动触发一次初始渲染
        boardView.OnGameStateUpdated(_currentGameState);

        Debug.Log($"游戏初始化完成! 模式: {initialGameMode}");
    }

    private void OnDestroy()
    {
        if (_commandProcessor != null && boardView != null)
        {
            _commandProcessor.OnGameStateUpdated -= boardView.OnGameStateUpdated;
        }
    }
}