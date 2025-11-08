// 文件路径: Assets/Scripts/_Core/Command/CommandProcessor.cs

using System;
using System.Collections.Generic;
using UnityEngine;

public class CommandProcessor
{
    private GameState _authoritativeGameState;
    private IGameModeLogic _gameModeLogic;

    #region State Change Events (原子状态变更事件)

    // 当游戏阶段改变时触发 (例如：从Playing到GameOver)
    public event Action<GamePhase> OnGamePhaseChanged;

    // 当棋子被创建时触发 (例如：开局初始化，或技能召唤)
    public event Action<PieceData> OnPieceCreated;

    // 当棋子数据更新时触发 (位置，状态等)
    public event Action<PieceData> OnPieceUpdated;

    // 当棋子被移除时触发 (被吃)
    public event Action<int> OnPieceRemoved;

    // 当行动点更新时触发
    public event Action<PlayerTeam, float> OnActionPointsUpdated;

    // 当回合制模式下，当前回合玩家改变时触发
    public event Action<PlayerTeam> OnTurnChanged;

    #endregion

    public CommandProcessor(GameState initialState)
    {
        _authoritativeGameState = initialState;
    }

    public void SetGameMode(IGameModeLogic logic)
    {
        _gameModeLogic = logic;
    }

    public void ProcessCommand(ICommand command)
    {
        if (_gameModeLogic == null || !_gameModeLogic.ValidateCommand(command, _authoritativeGameState))
        {
            Debug.LogError($"指令 {command.GetType().Name} 验证失败！");
            return;
        }

        // 记录执行前的状态快照，用于对比变化
        var stateBefore = SnapshotState();

        // 执行指令，修改权威GameState
        command.Execute(_authoritativeGameState);
        _authoritativeGameState.tick++;

        // 对比执行前后的状态，并广播原子事件
        DetectAndBroadcastChanges(stateBefore, _authoritativeGameState);
    }

    public void Tick()
    {
        if (_gameModeLogic != null && _authoritativeGameState.phase == GamePhase.Playing)
        {
            var stateBefore = SnapshotState();
            _gameModeLogic.OnTick(_authoritativeGameState);
            DetectAndBroadcastChanges(stateBefore, _authoritativeGameState);
        }
    }

    public GameState GetCurrentState()
    {
        return _authoritativeGameState;
    }

    // --- 私有辅助方法 ---

    private void DetectAndBroadcastChanges(GameState stateBefore, GameState stateAfter)
    {
        // 1. 检测游戏阶段变化
        if (stateBefore.phase != stateAfter.phase)
        {
            OnGamePhaseChanged?.Invoke(stateAfter.phase);
        }

        // 2. 检测行动点变化
        for (int i = 0; i < 2; i++)
        {
            if (Math.Abs(stateBefore.actionPoints[i] - stateAfter.actionPoints[i]) > 0.001f)
            {
                OnActionPointsUpdated?.Invoke((PlayerTeam)i, stateAfter.actionPoints[i]);
            }
        }

        // 3. 检测回合变化 (回合制)
        if (stateBefore.currentTurn != stateAfter.currentTurn)
        {
            OnTurnChanged?.Invoke(stateAfter.currentTurn);
        }

        // 4. 检测棋子变化 (创建, 更新, 删除)
        var beforeKeys = new HashSet<int>(stateBefore.pieces.Keys);
        var afterKeys = new HashSet<int>(stateAfter.pieces.Keys);

        // 检测新增的棋子
        foreach (var key in afterKeys)
        {
            if (!beforeKeys.Contains(key))
            {
                OnPieceCreated?.Invoke(stateAfter.pieces[key]);
            }
        }

        // 检测被移除的棋子
        foreach (var key in beforeKeys)
        {
            if (!afterKeys.Contains(key))
            {
                OnPieceRemoved?.Invoke(key);
            }
        }

        // 检测被更新的棋子
        foreach (var key in afterKeys)
        {
            if (beforeKeys.Contains(key))
            {
                // 注意：由于PieceData是struct，直接比较可能不准确，最好是比较每个字段
                // 为简化起见，这里我们假设任何对字典的操作都是更新
                // 在实际项目中，可以比较struct的哈希值或关键字段
                if (!stateBefore.pieces[key].Equals(stateAfter.pieces[key]))
                {
                    OnPieceUpdated?.Invoke(stateAfter.pieces[key]);
                }
            }
        }
    }

    // 创建一个状态的深拷贝快照 (简化版)
    private GameState SnapshotState()
    {
        // 注意：这是一个简化的深拷贝。在实际项目中，需要确保所有引用类型都被正确地复制。
        // 由于我们的GameState目前主要由值类型和简单集合构成，这样是可行的。
        var snapshot = new GameState
        {
            tick = _authoritativeGameState.tick,
            phase = _authoritativeGameState.phase,
            currentTurn = _authoritativeGameState.currentTurn,
            actionPoints = (float[])_authoritativeGameState.actionPoints.Clone(),
            pieces = new Dictionary<int, PieceData>(_authoritativeGameState.pieces)
            // skillCooldowns等其他字典也需要类似处理
        };
        return snapshot;
    }
}