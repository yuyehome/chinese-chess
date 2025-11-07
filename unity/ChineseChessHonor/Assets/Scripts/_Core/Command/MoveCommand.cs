// 文件路径: Assets/Scripts/_Core/Command/MoveCommand.cs

using UnityEngine;

public struct MoveCommand : ICommand
{
    public readonly int pieceId;
    public readonly Vector2Int targetPosition;
    public readonly PlayerTeam requestTeam;

    public MoveCommand(int pieceId, Vector2Int targetPosition, PlayerTeam requestTeam)
    {
        this.pieceId = pieceId;
        this.targetPosition = targetPosition;
        this.requestTeam = requestTeam;
    }

    public void Execute(GameState state)
    {
        if (state.pieces.TryGetValue(pieceId, out PieceData pieceData))
        {
            pieceData.position = targetPosition;
            pieceData.status |= PieceStatus.IsMoving;
            state.pieces[pieceId] = pieceData;
            state.actionPoints[(int)requestTeam] -= 1;
        }
        else
        {
            Debug.LogWarning($"MoveCommand: 未在GameState中找到ID为 {pieceId} 的棋子。");
        }
    }
}