// File: _Scripts/Controllers/AI/BaseAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 所有AI策略的抽象基类。
/// 提供了寻找棋子、获取所有移动等通用辅助方法，避免在每个策略类中重复代码。
/// </summary>
public abstract class BaseAIStrategy
{
    /// <summary>
    /// 在棋盘上寻找指定颜色王的位置。
    /// </summary>
    protected Vector2Int FindKingPosition(BoardState boardState, PlayerColor kingColor)
    {
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Piece piece = boardState.GetPieceAt(new Vector2Int(x, y));
                if (piece.Type == PieceType.General && piece.Color == kingColor)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    /// <summary>
    /// 获取一个颜色的所有棋子的所有合法移动。
    /// </summary>
    protected List<AIController.MovePlan> GetAllPossibleMoves(PlayerColor color, BoardState board, List<PieceComponent> pieces)
    {
        var allMoves = new List<AIController.MovePlan>();
        foreach (var piece in pieces)
        {
            if (piece.RTState != null && piece.RTState.IsMoving) continue;

            var validTargets = RuleEngine.GetValidMoves(piece.PieceData, piece.BoardPosition, board);
            foreach (var targetPos in validTargets)
            {
                int targetValue = 0;
                Piece targetPiece = board.GetPieceAt(targetPos);
                if (targetPiece.Type != PieceType.None && targetPiece.Color != color)
                {
                    targetValue = PieceValue.GetValue(targetPiece.Type);
                }
                allMoves.Add(new AIController.MovePlan(piece, piece.BoardPosition, targetPos, targetValue));
            }
        }
        return allMoves;
    }
}