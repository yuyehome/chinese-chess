// File: _Scripts/Controllers/AI/BaseAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ����AI���Եĳ�����ࡣ
/// �ṩ��Ѱ�����ӡ���ȡ�����ƶ���ͨ�ø���������������ÿ�����������ظ����롣
/// </summary>
public abstract class BaseAIStrategy
{
    /// <summary>
    /// ��������Ѱ��ָ����ɫ����λ�á�
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
    /// ��ȡһ����ɫ���������ӵ����кϷ��ƶ���
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