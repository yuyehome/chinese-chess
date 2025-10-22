// File: _Scripts/Controllers/AI/EasyAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 简单AI的决策策略。
/// 行为模式：按概率选择进攻、躲避或随机移动。
/// </summary>
public class EasyAIStrategy : IAIStrategy
{
    public AIController.MovePlan FindBestMove(PlayerColor assignedColor, BoardState logicalBoard, List<PieceComponent> myPieces)
    {
        // 1. 掷骰子，决定本回合的主要意图
        float intentionRoll = Random.Range(0f, 1f);

        if (intentionRoll < 0.5f) // 50% 概率优先考虑进攻
        {
            Debug.Log("[AI-Easy] 意图：进攻！");
            var attackMove = FindAttackMove(assignedColor, logicalBoard, myPieces);
            if (attackMove != null) return attackMove;
        }
        else if (intentionRoll < 0.8f) // 30% 概率优先考虑躲避
        {
            Debug.Log("[AI-Easy] 意图：躲避！");
            var evadeMove = FindEvadeMove(assignedColor, logicalBoard, myPieces);
            if (evadeMove != null) return evadeMove;
        }

        // 20% 概率或以上意图失败时，执行随机移动
        Debug.Log("[AI-Easy] 意图：随机移动。");
        return FindRandomMove(assignedColor, logicalBoard, myPieces);
    }

    /// <summary>
    /// 查找所有可行的吃子移动，并随机选择一个。
    /// </summary>
    private AIController.MovePlan FindAttackMove(PlayerColor color, BoardState board, List<PieceComponent> pieces)
    {
        var captureMoves = new List<AIController.MovePlan>();
        foreach (var piece in pieces)
        {
            if (piece.RTState != null && piece.RTState.IsMoving) continue;
            var validTargets = RuleEngine.GetValidMoves(piece.PieceData, piece.BoardPosition, board);
            foreach (var targetPos in validTargets)
            {
                Piece targetPiece = board.GetPieceAt(targetPos);
                if (targetPiece.Type != PieceType.None && targetPiece.Color != color)
                {
                    captureMoves.Add(new AIController.MovePlan(piece, piece.BoardPosition, targetPos, 0));
                }
            }
        }
        if (captureMoves.Count > 0)
        {
            return captureMoves[Random.Range(0, captureMoves.Count)];
        }
        return null; // 没有找到吃子机会
    }

    /// <summary>
    /// 查找一个正处于危险中的棋子，并为它找到一个安全的移动位置。
    /// </summary>
    private AIController.MovePlan FindEvadeMove(PlayerColor color, BoardState board, List<PieceComponent> pieces)
    {
        var piecesInDanger = pieces.Where(p => RuleEngine.IsPositionUnderAttack(p.BoardPosition, (color == PlayerColor.Red ? PlayerColor.Black : PlayerColor.Red), board)).ToList();

        if (piecesInDanger.Count > 0)
        {
            // 从危险的棋子中随机选一个来拯救
            var pieceToSave = piecesInDanger[Random.Range(0, piecesInDanger.Count)];

            // 找出它所有能移动到的安全位置
            var safeMoves = new List<Vector2Int>();
            var validTargets = RuleEngine.GetValidMoves(pieceToSave.PieceData, pieceToSave.BoardPosition, board);
            foreach (var targetPos in validTargets)
            {
                if (!RuleEngine.IsPositionUnderAttack(targetPos, (color == PlayerColor.Red ? PlayerColor.Black : PlayerColor.Red), board))
                {
                    safeMoves.Add(targetPos);
                }
            }

            if (safeMoves.Count > 0)
            {
                // 从所有安全位置中随机选一个
                var safeTarget = safeMoves[Random.Range(0, safeMoves.Count)];
                return new AIController.MovePlan(pieceToSave, pieceToSave.BoardPosition, safeTarget, 0);
            }
        }
        return null; // 没有棋子处于危险中，或危险中的棋子无处可逃
    }

    /// <summary>
    /// 查找所有可行的普通移动，并随机选择一个。
    /// </summary>
    private AIController.MovePlan FindRandomMove(PlayerColor color, BoardState board, List<PieceComponent> pieces)
    {
        var allMoves = new List<AIController.MovePlan>();
        foreach (var piece in pieces)
        {
            if (piece.RTState != null && piece.RTState.IsMoving) continue;
            var validTargets = RuleEngine.GetValidMoves(piece.PieceData, piece.BoardPosition, board);
            foreach (var targetPos in validTargets)
            {
                allMoves.Add(new AIController.MovePlan(piece, piece.BoardPosition, targetPos, 0));
            }
        }
        if (allMoves.Count > 0)
        {
            return allMoves[Random.Range(0, allMoves.Count)];
        }
        return null;
    }
}