// File: _Scripts/Controllers/AI/VeryHardAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ����AI�ľ��߲��ԡ�
/// ��Ϊģʽ��
/// 1. ���ֽ׶Σ���ѭԤ��Ŀ��ֿ⡣
/// 2. ���뿪�ֿ�����ȴ�������в (100% ����)��
/// 3. �������ʹ�� Minimax �㷨��ǰ��һ����Ԥ�ж��ֵ����Ӧ�ԡ�
/// </summary>
public class VeryHardAIStrategy : HardAIStrategy, IAIStrategy // �̳��� HardAI �Ը�����������
{
    // --- ���ֿ� ---
    private static List<List<Vector2Int>> openingBook;
    private List<Vector2Int> currentOpening;
    private int openingMoveIndex = 0;
    private bool useOpeningBook = true;

    // --- Minimax �㷨��� ---
    private const int SEARCH_DEPTH = 2; // ������ȣ�2����(AI��һ��, �����һ��)

    public VeryHardAIStrategy()
    {
        InitializeOpeningBook();
        SelectRandomOpening();
    }

    public override AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor)
    {
        // --- 1. ���ֿ�׶� ---
        if (useOpeningBook && openingMoveIndex < currentOpening.Count)
        {
            return ExecuteOpeningBookMove(gameManager, assignedColor);
        }

        // --- ���뿪�ֿ����߼� ---
        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        // --- 2. Σ����� (100% ����) ---
        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            Debug.Log("[AI-VeryHard] Σ������������������ȼݣ�");
            // ���ø�������žȼ��߼�
            return FindBestSavingMove(gameManager, assignedColor, logicalBoard, myPieces, kingPos);
        }

        // --- 3. Minimax ������� ---
        return FindBestMoveWithMinimax(gameManager, assignedColor);
    }

    #region Minimax Implementation

    private AIController.MovePlan FindBestMoveWithMinimax(GameManager gameManager, PlayerColor assignedColor)
    {
        var allMoves = GetAllPossibleMoves(assignedColor, gameManager.GetLogicalBoardState(), gameManager.GetAllPiecesOfColor(assignedColor));
        if (allMoves.Count == 0) return null;

        int bestScore = int.MinValue;
        var bestMoves = new List<AIController.MovePlan>();

        foreach (var move in allMoves)
        {
            // ģ���ҷ�����
            BoardState futureBoard = gameManager.GetLogicalBoardState().Clone();
            futureBoard.MovePiece(move.From, move.To);

            // ����������ҷ����������Ӧ�����ܴﵽ�ľ�������������Ƕ�������ͷ֣�
            int score = Minimax(gameManager, futureBoard, SEARCH_DEPTH - 1, false, assignedColor);

            if (score > bestScore)
            {
                bestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (score == bestScore)
            {
                bestMoves.Add(move);
            }
        }

        if (bestMoves.Count > 0)
        {
            var chosenMove = bestMoves[Random.Range(0, bestMoves.Count)];
            Debug.Log($"[AI-VeryHard] Minimax���ߣ�ѡ���ƶ� {chosenMove.PieceToMove.name} -> {chosenMove.To} (Ԥ�е÷�: {bestScore})");
            return chosenMove;
        }
        return null;
    }

    private int Minimax(GameManager gameManager, BoardState board, int depth, bool isMaximizingPlayer, PlayerColor myColor)
    {
        // ��׼������ﵽ������Ȼ���Ϸ����
        if (depth == 0)
        {
            return EvaluateBoardState(board, myColor);
        }

        PlayerColor currentColor = isMaximizingPlayer ? myColor : (myColor == PlayerColor.Red ? PlayerColor.Black : PlayerColor.Red);
        var pieces = gameManager.GetAllPiecesOfColorFromBoard(currentColor, board); // ��Ҫһ���µĸ�������
        var allMoves = GetAllPossibleMoves(currentColor, board, pieces);

        if (isMaximizingPlayer) // �ҷ���AI����Ѱ������
        {
            int maxEval = int.MinValue;
            foreach (var move in allMoves)
            {
                BoardState futureBoard = board.Clone();
                futureBoard.MovePiece(move.From, move.To);
                int eval = Minimax(gameManager, futureBoard, depth - 1, false, myColor);
                maxEval = Mathf.Max(maxEval, eval);
            }
            return maxEval;
        }
        else // �з�����ң���Ѱ����С��
        {
            int minEval = int.MaxValue;
            foreach (var move in allMoves)
            {
                BoardState futureBoard = board.Clone();
                futureBoard.MovePiece(move.From, move.To);
                int eval = Minimax(gameManager, futureBoard, depth - 1, true, myColor);
                minEval = Mathf.Min(minEval, eval);
            }
            return minEval;
        }
    }

    /// <summary>
    /// �����������̵ľ������������Խ�߶�AIԽ������
    /// </summary>
    private int EvaluateBoardState(BoardState board, PlayerColor myColor)
    {
        int totalScore = 0;
        PlayerColor opponentColor = (myColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece pieceData = board.GetPieceAt(pos);
                if (pieceData.Type != PieceType.None)
                {
                    // ע�⣺������Ҫһ���ٵ�PieceComponent������GetPositionalValue������һ�����Ż��ĵ�
                    var tempPieceComp = new PieceComponent { PieceData = pieceData };
                    int pieceScore = PieceValue.GetValue(pieceData.Type) + GetPositionalValue(tempPieceComp, pos, pieceData.Color);

                    if (pieceData.Color == myColor)
                        totalScore += pieceScore;
                    else
                        totalScore -= pieceScore;
                }
            }
        }
        return totalScore;
    }

    #endregion

    #region Opening Book Implementation
    private void InitializeOpeningBook()
    {
        if (openingBook != null) return; // ��̬������ֻ���ʼ��һ��

        openingBook = new List<List<Vector2Int>>
        {
            // ����1: ��ͷ�� (����) -> ����
            new List<Vector2Int>
            {
                new Vector2Int(1, 2), new Vector2Int(4, 2), // �ڷ� ��2ƽ5
                new Vector2Int(1, 0), new Vector2Int(2, 2)  // �ڷ� ��2��3
            },
            // ����2: ����� -> ����
            new List<Vector2Int>
            {
                new Vector2Int(2, 0), new Vector2Int(4, 2), // �ڷ� ��3��5
                new Vector2Int(7, 0), new Vector2Int(6, 2)  // �ڷ� ��8��7
            },
            // ����3: �����
            new List<Vector2Int>
            {
                new Vector2Int(1, 0), new Vector2Int(2, 2)  // �ڷ� ��2��3
            }
            // �������գ������һ�����б�������������֡�
            // new List<Vector2Int>() 
        };
    }

    private void SelectRandomOpening()
    {
        if (openingBook.Count == 0)
        {
            useOpeningBook = false;
            currentOpening = new List<Vector2Int>();
            return;
        }
        currentOpening = openingBook[Random.Range(0, openingBook.Count)];
        Debug.Log($"[AI-VeryHard] ��ѡ�񿪾ֿ⣬�� {currentOpening.Count / 2} ����");
    }

    private AIController.MovePlan ExecuteOpeningBookMove(GameManager gameManager, PlayerColor assignedColor)
    {
        Vector2Int from = currentOpening[openingMoveIndex];
        Vector2Int to = currentOpening[openingMoveIndex + 1];

        PieceComponent pieceToMove = gameManager.BoardRenderer.GetPieceComponentAt(from);

        // ��֤���ֿ��ƶ��Ƿ�Ϸ� (���磬������˷��������ֵ�ס��·)
        if (pieceToMove != null && pieceToMove.PieceData.Color == assignedColor && RuleEngine.GetValidMoves(pieceToMove.PieceData, from, gameManager.GetLogicalBoardState()).Contains(to))
        {
            Debug.Log($"[AI-VeryHard] ִ�п��ֿ�� {(openingMoveIndex / 2) + 1} ��: {from} -> {to}");
            openingMoveIndex += 2;
            return new AIController.MovePlan(pieceToMove, from, to, 0);
        }
        else
        {
            // ������ֿ��ƶ����Ϸ����������л���˼��ģʽ
            Debug.LogWarning("[AI-VeryHard] ���ֿ��ƶ����Ϸ��������鱾����ʼ����˼����");
            useOpeningBook = false;
            return FindBestMoveWithMinimax(gameManager, assignedColor);
        }
    }
    #endregion
}