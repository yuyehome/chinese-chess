// File: _Scripts/Controllers/AI/EasyAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ��AI�ľ��߲��ԡ�
/// ��Ϊģʽ��������ѡ���������ܻ�����ƶ���
/// </summary>
public class EasyAIStrategy : IAIStrategy
{
    public AIController.MovePlan FindBestMove(PlayerColor assignedColor, BoardState logicalBoard, List<PieceComponent> myPieces)
    {
        // 1. �����ӣ��������غϵ���Ҫ��ͼ
        float intentionRoll = Random.Range(0f, 1f);

        if (intentionRoll < 0.5f) // 50% �������ȿ��ǽ���
        {
            Debug.Log("[AI-Easy] ��ͼ��������");
            var attackMove = FindAttackMove(assignedColor, logicalBoard, myPieces);
            if (attackMove != null) return attackMove;
        }
        else if (intentionRoll < 0.8f) // 30% �������ȿ��Ƕ��
        {
            Debug.Log("[AI-Easy] ��ͼ����ܣ�");
            var evadeMove = FindEvadeMove(assignedColor, logicalBoard, myPieces);
            if (evadeMove != null) return evadeMove;
        }

        // 20% ���ʻ�������ͼʧ��ʱ��ִ������ƶ�
        Debug.Log("[AI-Easy] ��ͼ������ƶ���");
        return FindRandomMove(assignedColor, logicalBoard, myPieces);
    }

    /// <summary>
    /// �������п��еĳ����ƶ��������ѡ��һ����
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
        return null; // û���ҵ����ӻ���
    }

    /// <summary>
    /// ����һ��������Σ���е����ӣ���Ϊ���ҵ�һ����ȫ���ƶ�λ�á�
    /// </summary>
    private AIController.MovePlan FindEvadeMove(PlayerColor color, BoardState board, List<PieceComponent> pieces)
    {
        var piecesInDanger = pieces.Where(p => RuleEngine.IsPositionUnderAttack(p.BoardPosition, (color == PlayerColor.Red ? PlayerColor.Black : PlayerColor.Red), board)).ToList();

        if (piecesInDanger.Count > 0)
        {
            // ��Σ�յ����������ѡһ��������
            var pieceToSave = piecesInDanger[Random.Range(0, piecesInDanger.Count)];

            // �ҳ����������ƶ����İ�ȫλ��
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
                // �����а�ȫλ�������ѡһ��
                var safeTarget = safeMoves[Random.Range(0, safeMoves.Count)];
                return new AIController.MovePlan(pieceToSave, pieceToSave.BoardPosition, safeTarget, 0);
            }
        }
        return null; // û�����Ӵ���Σ���У���Σ���е������޴�����
    }

    /// <summary>
    /// �������п��е���ͨ�ƶ��������ѡ��һ����
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