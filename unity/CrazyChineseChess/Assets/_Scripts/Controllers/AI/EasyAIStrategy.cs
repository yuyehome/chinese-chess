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

    public AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor)
    {
        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        // --- ��һ�㣺Σ����� ---
        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            Debug.Log("[AI-Easy] Σ��������������");
            if (Random.Range(0f, 1f) < 0.8f) // 80%���ʾȼ�
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

    // --- �������ȼ��߼� ---
    protected AIController.MovePlan FindSavingMove(GameManager gameManager, PlayerColor color, BoardState board, List<PieceComponent> pieces, Vector2Int kingPos)
    {
        // ��AI�ľȼ��߼���ֻ�����ƶ��������
        PieceComponent kingPiece = gameManager.BoardRenderer.GetPieceComponentAt(kingPos);
        if (kingPiece == null) return null;

        var validKingMoves = RuleEngine.GetValidMoves(kingPiece.PieceData, kingPos, board);
        var safeKingMoves = validKingMoves.Where(move => !RuleEngine.IsPositionUnderAttack(move, (color == PlayerColor.Red ? PlayerColor.Black : PlayerColor.Red), board)).ToList();

        if (safeKingMoves.Count > 0)
        {
            return new AIController.MovePlan(kingPiece, kingPos, safeKingMoves[Random.Range(0, safeKingMoves.Count)], 1000); // �ȼ���������ȼ�
        }
        return null; // ���޴����� (�������Ǿ�ɱ)
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