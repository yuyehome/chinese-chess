// File: _Scripts/Controllers/AI/HardAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ����AI�ľ��߲��ԡ�
/// ��Ϊģʽ�����ȴ�������в��Ȼ��ͨ��һ��������������ÿ�������ƶ��ĵ÷֣�ѡ��÷���ߵ��ƶ���
/// </summary>
public class HardAIStrategy : BaseAIStrategy, IAIStrategy
{
    // --- ������������ ---
    private const int CAPTURE_MULTIPLIER = 10;      // ���ӻ����ֵĳ���
    private const int THREAT_MULTIPLIER = 2;        // ��в�Է����ӵĳ���
    private const int SAVING_MULTIPLIER = 8;        // ���ȼ������ӵĳ���
    private const int CENTER_CONTROL_BONUS = 5;     // ռ������λ�õĽ���
    private const int SAFE_MOVE_BONUS = 2;          // �ƶ�����ȫλ�õĽ���

    /// <summary>
    /// ����AI���ߵ�����ڡ�
    /// </summary>
    public virtual AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor)
    {
        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        // --- ��һ�㣺Σ����� (90%����) ---
        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            Debug.Log("[AI-Hard] Σ��������������");
            if (Random.Range(0f, 1f) < 0.9f)
            {
                var savingMove = FindBestSavingMove(gameManager, assignedColor, logicalBoard, myPieces, kingPos);
                if (savingMove != null)
                {
                    Debug.Log("[AI-Hard] ���ߣ�ִ�����žȼݣ�");
                    return savingMove;
                }
            }
            else
            {
                Debug.Log("[AI-Hard] ���ߣ������˽�����(10%����)");
            }
        }

        // --- �����㣺������� (���ڵ÷�����) ---
        List<AIController.MovePlan> allMoves = GetAllPossibleMoves(assignedColor, logicalBoard, myPieces);
        if (allMoves.Count == 0) return null;

        int highestScore = int.MinValue;
        var bestMoves = new List<AIController.MovePlan>();

        foreach (var move in allMoves)
        {
            // Ϊÿ���ƶ����
            int score = EvaluateMove(move, assignedColor, logicalBoard, opponentColor);

            if (score > highestScore)
            {
                highestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (score == highestScore)
            {
                bestMoves.Add(move);
            }
        }

        if (bestMoves.Count > 0)
        {
            // �����е÷���ߵ��ƶ������ѡ��һ��
            var bestMove = bestMoves[Random.Range(0, bestMoves.Count)];
            Debug.Log($"[AI-Hard] ���ߣ�ѡ�������ƶ� {bestMove.PieceToMove.name} -> {bestMove.To} (�÷�: {highestScore})");
            return bestMove;
        }

        return null;
    }

    /// <summary>
    /// ����һ���ƶ����ۺϵ÷֡���Ϊ protected virtual �Ա����ิ�ú���չ��
    /// </summary>
    protected virtual int EvaluateMove(AIController.MovePlan move, PlayerColor myColor, BoardState board, PlayerColor opponentColor)
    {
        int score = 0;

        // 1. �����÷֣��Ե��Է����ӵļ�ֵ
        score += move.TargetValue * CAPTURE_MULTIPLIER;

        // ģ���ƶ��������
        BoardState futureBoard = board.Clone();
        futureBoard.MovePiece(move.From, move.To);

        // 2. ������в�÷֣��ƶ����ܹ�������Щ�µĵз�����
        var newThreats = RuleEngine.GetValidMoves(move.PieceToMove.PieceData, move.To, futureBoard);
        foreach (var threatenedPos in newThreats)
        {
            Piece threatenedPiece = futureBoard.GetPieceAt(threatenedPos);
            if (threatenedPiece.Type != PieceType.None && threatenedPiece.Color == opponentColor)
            {
                score += PieceValue.GetValue(threatenedPiece.Type) * THREAT_MULTIPLIER;
            }
        }

        // 3. ��ȫ���������ƶ����Ƿ���������ԣ�
        if (RuleEngine.IsPositionUnderAttack(move.To, opponentColor, futureBoard))
        {
            int myValue = PieceValue.GetValue(move.PieceToMove.PieceData.Type);
            // ����һ�������ƶ���������Ϊ�˶ҵ����߼�ֵ����
            score -= myValue * CAPTURE_MULTIPLIER;
        }
        else
        {
            score += SAFE_MOVE_BONUS; // ��ȫ�ƶ��ӷ�
        }

        // 4. ���ص÷֣��Ƿ���������һ������в�����ӣ�
        if (RuleEngine.IsPositionUnderAttack(move.From, opponentColor, board))
        {
            int myValue = PieceValue.GetValue(move.PieceToMove.PieceData.Type);
            score += myValue * SAVING_MULTIPLIER;
        }

        // 5. λ�õ÷�
        score += GetPositionalValue(move.PieceToMove, move.To, myColor);

        return score;
    }

    /// <summary>
    /// ��ȡһ�������ƶ����ض�λ�õļ�ֵ����Ϊ protected virtual �Ա����ิ�ú���չ��
    /// </summary>
    protected virtual int GetPositionalValue(PieceComponent piece, Vector2Int pos, PlayerColor myColor)
    {
        int value = 0;

        // a. ���Ŀ���: ռ����·(x=3,4,5)�����Ӹ��м�ֵ
        if (pos.x >= 3 && pos.x <= 5)
        {
            value += CENTER_CONTROL_BONUS;
        }

        // b. ����ѹ��: ��Խ���ӣ�Խ���룬��ֵԽ��
        if (piece.PieceData.Type == PieceType.Soldier)
        {
            if (myColor == PlayerColor.Red && pos.y > 4)
            {
                value += pos.y * 3; // �������ı��÷ָ���
            }
            else if (myColor == PlayerColor.Black && pos.y < 5)
            {
                value += (9 - pos.y) * 3;
            }
        }

        return value;
    }

    /// <summary>
    /// ����AI�ľȼ��߼����������п��ܵľȼݷ�ʽ��ѡ�����Ž⡣
    /// </summary>
    protected AIController.MovePlan FindBestSavingMove(GameManager gameManager, PlayerColor color, BoardState board, List<PieceComponent> pieces, Vector2Int kingPos)
    {
        // Ŀǰ��ʱ���ü�AI�ľȼ��߼���ֻ�ƶ���
        // TODO: δ���������Ӹ񵲺ͷ����ľȼݷ�ʽ����
        PieceComponent kingPiece = gameManager.BoardRenderer.GetPieceComponentAt(kingPos);
        if (kingPiece == null)
        {
            // �����BoardRenderer���Ҳ�������������Ϊ�������ƶ��У�����һ�ֱ�Ե�����
            // ��ʱ����Ӧ�ôӴ����pieces�б��в���
            kingPiece = pieces.FirstOrDefault(p => p.PieceData.Type == PieceType.General);
            if (kingPiece == null) return null;
        }

        PlayerColor opponentColor = (color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        var validKingMoves = RuleEngine.GetValidMoves(kingPiece.PieceData, kingPos, board);
        var safeKingMoves = validKingMoves.Where(move => !RuleEngine.IsPositionUnderAttack(move, opponentColor, board)).ToList();

        if (safeKingMoves.Count > 0)
        {
            return new AIController.MovePlan(kingPiece, kingPos, safeKingMoves[Random.Range(0, safeKingMoves.Count)], 10000); // �ȼݵ÷ּ���
        }
        return null;
    }
}