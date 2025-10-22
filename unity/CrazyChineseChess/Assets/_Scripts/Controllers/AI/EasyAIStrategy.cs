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

    public AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor)
    {
        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        // --- 第一层：危机检测 ---
        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            Debug.Log("[AI-Easy] 危机：王被将军！");
            if (Random.Range(0f, 1f) < 0.8f) // 80%概率救驾
            {
                var savingMove = FindSavingMove(gameManager, assignedColor, logicalBoard, myPieces, kingPos);
                if (savingMove != null)
                {
                    Debug.Log("[AI-Easy] 决策：执行救驾！");
                    return savingMove;
                }
            }
            else
            {
                Debug.Log("[AI-Easy] 决策：忽略了将军！(20%概率)");
            }
        }

        // --- 第三层：常规策略 ---
        float intentionRoll = Random.Range(0f, 1f);
        if (intentionRoll < 0.5f)
        {
            var move = FindAttackMove(assignedColor, logicalBoard, myPieces);
            if (move != null) return move;
        }
        else if (intentionRoll < 0.8f)
        {
            var move = FindEvadeMove(assignedColor, logicalBoard, myPieces);
            if (move != null) return move;
        }

        return FindRandomMove(assignedColor, logicalBoard, myPieces);
    }

    // --- 新增：救驾逻辑 ---
    protected AIController.MovePlan FindSavingMove(GameManager gameManager, PlayerColor color, BoardState board, List<PieceComponent> pieces, Vector2Int kingPos)
    {
        // 简单AI的救驾逻辑：只考虑移动王来躲避
        PieceComponent kingPiece = gameManager.BoardRenderer.GetPieceComponentAt(kingPos);
        if (kingPiece == null) return null;

        var validKingMoves = RuleEngine.GetValidMoves(kingPiece.PieceData, kingPos, board);
        var safeKingMoves = validKingMoves.Where(move => !RuleEngine.IsPositionUnderAttack(move, (color == PlayerColor.Red ? PlayerColor.Black : PlayerColor.Red), board)).ToList();

        if (safeKingMoves.Count > 0)
        {
            return new AIController.MovePlan(kingPiece, kingPos, safeKingMoves[Random.Range(0, safeKingMoves.Count)], 1000); // 救驾是最高优先级
        }
        return null; // 王无处可逃 (理论上是绝杀)
    }

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