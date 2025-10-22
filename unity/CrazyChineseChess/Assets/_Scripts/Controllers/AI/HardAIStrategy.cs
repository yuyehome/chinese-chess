// File: _Scripts/Controllers/AI/HardAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ����AI�ľ��߲��ԡ�
/// ��Ϊģʽ�����ȴ�������в��90%���ʣ���Ȼ��ͨ��һ��������������ÿ�������ƶ��ĵ÷֣�ѡ��÷���ߵ��ƶ���
/// </summary>
public class HardAIStrategy : EasyAIStrategy, IAIStrategy // �̳���EasyAI�Ը���FindKingPosition�ȸ�������
{
    // --- ����Ȩ�س��� ---
    private const int CAPTURE_MULTIPLIER = 10; // ���ӵ÷� = ���Ӽ�ֵ * 10
    private const int SAVING_MULTIPLIER = 6;  // ��һ������в���ӵ÷� = ���Ӽ�ֵ * 6
    private const int THREATEN_MULTIPLIER = 2; // ��в�Է��߼�ֵ�ӵ÷� = ���Ӽ�ֵ * 2
    private const int SAFE_MOVE_BONUS = 5;     // �ƶ�����ȫλ�õĻ�����
    private const int POSITIONAL_BONUS = 20;   // λ�����Ʒ֣�������ӣ�

    /// <summary>
    /// ����AI���ߵ���ڷ�����
    /// </summary>
    public new AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor)
    {
        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        if (myPieces.Count == 0) return null;

        // --- ��һ�㣺Σ����� (90%����) ---
        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            Debug.Log("[AI-Hard] Σ��������������");
            if (Random.Range(0f, 1f) < 0.9f)
            {
                // ����AI���������оȼݷ�ʽ����ѡ�����Ž�
                var savingMove = FindBestSavingMove(gameManager, assignedColor, logicalBoard, myPieces, kingPos, opponentColor);
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

        AIController.MovePlan bestMove = null;
        int highestScore = int.MinValue;

        // �������п��ܵ��ƶ���Ϊÿһ���ƶ����
        foreach (var move in allMoves)
        {
            int score = EvaluateMove(move, assignedColor, logicalBoard, opponentColor);

            // Ϊ�˱���AI��ȫ��ֹ���������ƶ�һ��΢С�����ֵ��ʹ�÷���ͬʱ���в�ͬѡ��
            score += Random.Range(0, 4);

            if (score > highestScore)
            {
                highestScore = score;
                bestMove = move;
            }
        }

        if (bestMove != null)
        {
            Debug.Log($"[AI-Hard] ���ߣ�ѡ�������ƶ� {bestMove.PieceToMove.name} -> {bestMove.To} (�÷�: {highestScore})");
        }
        return bestMove;
    }

    /// <summary>
    /// ����һ���ƶ����ۺϵ÷֡�
    /// </summary>
    private int EvaluateMove(AIController.MovePlan move, PlayerColor myColor, BoardState board, PlayerColor opponentColor)
    {
        int score = 0;
        int myValue = PieceValue.GetValue(move.PieceToMove.PieceData.Type);

        // 1. �����÷֣��Ե��Է����ӵļ�ֵ
        score += move.TargetValue * CAPTURE_MULTIPLIER;

        // ģ���ƶ��������
        BoardState futureBoard = board.Clone();
        futureBoard.MovePiece(move.From, move.To);

        // 2. ��ȫ���������ƶ����Ƿ���������ԣ�
        bool isMoveSafe = !RuleEngine.IsPositionUnderAttack(move.To, opponentColor, futureBoard);
        if (isMoveSafe)
        {
            score += SAFE_MOVE_BONUS;
        }
        else
        {
            // �ƶ���һ���ᱻ�Ե���λ�ã�����һ���ǳ������ƶ���Ҫ�۳������ֵ�ķ���
            // �����AIѧ�ᡰ���ӡ���ֻ�е��Ե��Ӽ�ֵԶ�����Լ�ʱ�ſ�������
            score -= myValue * CAPTURE_MULTIPLIER;
        }

        // 3. ���ص÷֣��Ƿ���������һ����ǰ����в�����ӣ�
        if (RuleEngine.IsPositionUnderAttack(move.From, opponentColor, board) && isMoveSafe)
        {
            // ����ɹ���Σ��λ���ƶ����˰�ȫλ�ã���������ӷ�
            score += myValue * SAVING_MULTIPLIER;
        }

        // 4. ��в�÷֣��ƶ����Ƿ��ܽ�������в���Է��߼�ֵ���ӣ�
        var movesAfterMove = RuleEngine.GetValidMoves(move.PieceToMove.PieceData, move.To, futureBoard);
        foreach (var nextTarget in movesAfterMove)
        {
            Piece threatenedPiece = futureBoard.GetPieceAt(nextTarget);
            if (threatenedPiece.Color == opponentColor)
            {
                // ������Ȩ�����
                if (threatenedPiece.Type == PieceType.General)
                {
                    score += 50;
                }
                else
                {
                    // ��в�����ӵĵ÷� = �Է���ֵ * ��вϵ��
                    score += PieceValue.GetValue(threatenedPiece.Type) * THREATEN_MULTIPLIER;
                }
            }
        }

        // 5. λ�õ÷�
        if (move.PieceToMove.PieceData.Type == PieceType.Soldier &&
            ((myColor == PlayerColor.Red && move.To.y > 4) || (myColor == PlayerColor.Black && move.To.y < 5)))
        {
            score += POSITIONAL_BONUS; // �����Ӽӷ�
        }

        return score;
    }

    /// <summary>
    /// ����AI�ľȼ��߼����������п��ܵľȼݷ�ʽ��ѡ�����Ž⡣
    /// </summary>
    private AIController.MovePlan FindBestSavingMove(GameManager gameManager, PlayerColor color, BoardState board, List<PieceComponent> pieces, Vector2Int kingPos, PlayerColor opponentColor)
    {
        var savingMoves = new List<AIController.MovePlan>();

        // ����A: �ƶ�������ȫλ��
        PieceComponent kingPiece = gameManager.BoardRenderer.GetPieceComponentAt(kingPos);
        if (kingPiece != null)
        {
            var validKingMoves = RuleEngine.GetValidMoves(kingPiece.PieceData, kingPos, board);
            foreach (var move in validKingMoves)
            {
                if (!RuleEngine.IsPositionUnderAttack(move, opponentColor, board))
                {
                    savingMoves.Add(new AIController.MovePlan(kingPiece, kingPos, move, 0));
                }
            }
        }

        // ����B: �Ե����ڽ���������
        // (Ϊ�˼򻯣��������ҵ����й�����������)
        var attackers = FindAttackers(kingPos, opponentColor, board, gameManager);
        foreach (var attacker in attackers)
        {
            // ����ҷ�����Щ���ӿ��ԳԵ����������
            foreach (var myPiece in pieces)
            {
                var myMoves = RuleEngine.GetValidMoves(myPiece.PieceData, myPiece.BoardPosition, board);
                if (myMoves.Contains(attacker.BoardPosition))
                {
                    int captureValue = PieceValue.GetValue(attacker.PieceData.Type);
                    savingMoves.Add(new AIController.MovePlan(myPiece, myPiece.BoardPosition, attacker.BoardPosition, captureValue));
                }
            }
        }

        // ����C: ��
        // (���߼���Ϊ���ӣ���ʱ��ʵ�֣���Ϊδ��������չ��)

        // �����п��еľȼݷ����У�ѡ��һ�����ŵ�
        if (savingMoves.Count > 0)
        {
            // ʹ����������Ϊÿ���ȼݷ�����֣�ѡ�������ߵ�
            return savingMoves.OrderByDescending(move => EvaluateMove(move, color, board, opponentColor)).First();
        }

        return null; // ������ɱ���޽�
    }

    /// <summary>
    /// ������������ȡһ��λ�õ����й����ߡ�
    /// </summary>
    private List<PieceComponent> FindAttackers(Vector2Int position, PlayerColor attackerColor, BoardState boardState, GameManager gameManager)
    {
        var attackers = new List<PieceComponent>();
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                var pieceComp = gameManager.BoardRenderer.GetPieceComponentAt(new Vector2Int(x, y));
                if (pieceComp != null && pieceComp.PieceData.Color == attackerColor)
                {
                    var moves = RuleEngine.GetValidMoves(pieceComp.PieceData, pieceComp.BoardPosition, boardState);
                    if (moves.Contains(position))
                    {
                        attackers.Add(pieceComp);
                    }
                }
            }
        }
        return attackers;
    }

    /// <summary>
    /// ������������ȡ���п��ܵ��ƶ���
    /// </summary>
    private List<AIController.MovePlan> GetAllPossibleMoves(PlayerColor color, BoardState board, List<PieceComponent> pieces)
    {
        var allMoves = new List<AIController.MovePlan>();
        foreach (var piece in pieces)
        {
            if (piece.RTState != null && piece.RTState.IsMoving) continue;
            var validTargets = RuleEngine.GetValidMoves(piece.PieceData, piece.BoardPosition, board);
            foreach (var targetPos in validTargets)
            {
                Piece targetPiece = board.GetPieceAt(targetPos);
                int targetValue = (targetPiece.Type != PieceType.None && targetPiece.Color != color) ? PieceValue.GetValue(targetPiece.Type) : 0;
                allMoves.Add(new AIController.MovePlan(piece, piece.BoardPosition, targetPos, targetValue));
            }
        }
        return allMoves;
    }
}