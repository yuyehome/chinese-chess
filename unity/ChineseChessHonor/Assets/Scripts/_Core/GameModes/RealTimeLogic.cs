// 文件路径: Assets/Scripts/_Core/GameModes/RealTimeLogic.cs

using UnityEngine;

public class RealTimeLogic : IGameModeLogic
{
    public void Initialize(GameState state)
    {
        // 这里我们硬编码一个简单的初始布局用于测试
        // 红方
        state.pieces.Add(0, new PieceData { uniqueId = 0, team = PlayerTeam.Red, type = PieceType.Chariot, position = new Vector2Int(0, 0) });
        state.pieces.Add(1, new PieceData { uniqueId = 1, team = PlayerTeam.Red, type = PieceType.Horse, position = new Vector2Int(1, 0) });

        // 黑方
        state.pieces.Add(16, new PieceData { uniqueId = 16, team = PlayerTeam.Black, type = PieceType.Chariot, position = new Vector2Int(0, 9) });
        state.pieces.Add(17, new PieceData { uniqueId = 17, team = PlayerTeam.Black, type = PieceType.Horse, position = new Vector2Int(1, 9) });

        // 设置所有棋子的初始状态
        var keys = new System.Collections.Generic.List<int>(state.pieces.Keys);
        foreach (var key in keys)
        {
            var piece = state.pieces[key];
            piece.status = PieceStatus.IsAttackable | PieceStatus.IsObstacle;
            state.pieces[key] = piece;
        }

        // 初始化行动点
        state.actionPoints[(int)PlayerTeam.Red] = GameConstants.ACTION_POINT_MAX;
        state.actionPoints[(int)PlayerTeam.Black] = GameConstants.ACTION_POINT_MAX;

        state.phase = GamePhase.Playing;
    }

    public void OnTick(GameState state)
    {
        // 恢复行动点
        for (int i = 0; i < state.actionPoints.Length; i++)
        {
            if (state.actionPoints[i] < GameConstants.ACTION_POINT_MAX)
            {
                state.actionPoints[i] += Time.fixedDeltaTime / GameConstants.ACTION_POINT_RECOVERY_RATE;
                state.actionPoints[i] = Mathf.Min(state.actionPoints[i], GameConstants.ACTION_POINT_MAX);
            }
        }
    }

    public bool ValidateCommand(ICommand command, GameState state)
    {
        if (command is MoveCommand moveCommand)
        {
            // 验证：发起移动的队伍是否有足够的行动点
            if (state.actionPoints[(int)moveCommand.requestTeam] >= 1)
            {
                return true;
            }
            Debug.LogWarning($"{moveCommand.requestTeam} 行动点不足，无法移动。");
            return false;
        }

        // 其他指令暂时默认允许
        return true;
    }
}