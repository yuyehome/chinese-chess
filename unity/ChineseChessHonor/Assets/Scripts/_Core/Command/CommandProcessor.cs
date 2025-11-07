// 文件路径: Assets/Scripts/_Core/Command/CommandProcessor.cs

using System;
using UnityEngine;

public class CommandProcessor
{
    private GameState _currentGameState;
    private IGameModeLogic _gameModeLogic; // 引入游戏模式逻辑

    public event Action<GameState> OnGameStateUpdated;

    public CommandProcessor(GameState initialState)
    {
        _currentGameState = initialState;
    }

    // 设置当前游戏模式的规则集
    public void SetGameMode(IGameModeLogic logic)
    {
        _gameModeLogic = logic;
    }

    public void ProcessCommand(ICommand command)
    {
        // 步骤 1: 使用GameModeLogic验证指令
        if (_gameModeLogic == null || !_gameModeLogic.ValidateCommand(command, _currentGameState))
        {
            Debug.LogError($"指令 {command.GetType().Name} 验证失败！");
            return;
        }

        // 步骤 2: 执行指令
        command.Execute(_currentGameState);
        _currentGameState.tick++;

        // 步骤 3: 广播状态更新
        OnGameStateUpdated?.Invoke(_currentGameState);
    }

    // 由GameLoopController在FixedUpdate中调用
    public void Tick()
    {
        if (_gameModeLogic != null && _currentGameState.phase == GamePhase.Playing)
        {
            _gameModeLogic.OnTick(_currentGameState);
        }
    }

    public GameState GetCurrentState()
    {
        return _currentGameState;
    }
}