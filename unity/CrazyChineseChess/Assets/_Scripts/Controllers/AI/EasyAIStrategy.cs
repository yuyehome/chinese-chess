// File: _Scripts/Controllers/AI/EasyAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EasyAIStrategy : BaseAIStrategy, IAIStrategy
{
    public AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor)
    {
        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        // --- ��һ�㣺Σ����� (80%����) ---
        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            Debug.Log("[AI-Easy] Σ��������������");
            if (Random.Range(0f, 1f) < 0.8f)
            {
                var savingMove = FindSavingMove(gameManager, assignedColor, logicalBoard, myPieces, kingPos);
                if (savingMove != null)
                {
                    Debug.Log("[AI-Easy] ���ߣ�ִ�оȼݣ�");
                    return savingMove;
                }
            }
            else
            {
                Debug.Log("[AI-Easy] ���ߣ������˽�����(20%����)");
            }
        }

        // --- �����㣺������� ---
        float intentionRoll = Random.Range(0f, 1f);
        AIController.MovePlan chosenMove = null;

        if (intentionRoll < 0.6f) // 60% �������ȿ��ǽ���
        {
            Debug.Log("[AI-Easy] ��ͼ��������");
            chosenMove = FindAttackMove(assignedColor, logicalBoard, myPieces);
        }

        if (chosenMove == null && intentionRoll < 0.9f) // 30% ���ʻ����ʧ��ʱ�����Ƕ��
        {
            Debug.Log("[AI-Easy] ��ͼ����ܣ�");
            chosenMove = FindEvadeMove(assignedColor, logicalBoard, myPieces);
        }

        // 10% ���ʻ�������ͼ��ʧ��ʱ��ִ������ƶ�
        if (chosenMove == null)
        {
            Debug.Log("[AI-Easy] ��ͼ������ƶ���");
            chosenMove = FindRandomMove(assignedColor, logicalBoard, myPieces);
        }

        return chosenMove;
    }

    private AIController.MovePlan FindSavingMove(GameManager gameManager, PlayerColor color, BoardState board, List<PieceComponent> pieces, Vector2Int kingPos)
    {
        PieceComponent kingPiece = gameManager.BoardRenderer.GetPieceComponentAt(kingPos);
        if (kingPiece == null) return null;

        PlayerColor opponentColor = (color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        var validKingMoves = RuleEngine.GetValidMoves(kingPiece.PieceData, kingPos, board);
        var safeKingMoves = validKingMoves.Where(move => !RuleEngine.IsPositionUnderAttack(move, opponentColor, board)).ToList();

        if (safeKingMoves.Count > 0)
        {
            return new AIController.MovePlan(kingPiece, kingPos, safeKingMoves[Random.Range(0, safeKingMoves.Count)], 1000);
        }
        return null;
    }

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
        return null;
    }

    private AIController.MovePlan FindEvadeMove(PlayerColor color, BoardState board, List<PieceComponent> pieces)
    {
        PlayerColor opponentColor = (color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        var piecesInDanger = pieces.Where(p => RuleEngine.IsPositionUnderAttack(p.BoardPosition, opponentColor, board)).ToList();

        if (piecesInDanger.Count > 0)
        {
            var pieceToSave = piecesInDanger[Random.Range(0, piecesInDanger.Count)];
            var validTargets = RuleEngine.GetValidMoves(pieceToSave.PieceData, pieceToSave.BoardPosition, board);
            var safeMoves = validTargets.Where(targetPos => !RuleEngine.IsPositionUnderAttack(targetPos, opponentColor, board)).ToList();

            if (safeMoves.Count > 0)
            {
                var safeTarget = safeMoves[Random.Range(0, safeMoves.Count)];
                return new AIController.MovePlan(pieceToSave, pieceToSave.BoardPosition, safeTarget, 0);
            }
        }
        return null;
    }

    private AIController.MovePlan FindRandomMove(PlayerColor color, BoardState board, List<PieceComponent> pieces)
    {
        var allMoves = GetAllPossibleMoves(color, board, pieces);
        if (allMoves.Count > 0)
        {
            return allMoves[Random.Range(0, allMoves.Count)];
        }
        return null;
    }
}