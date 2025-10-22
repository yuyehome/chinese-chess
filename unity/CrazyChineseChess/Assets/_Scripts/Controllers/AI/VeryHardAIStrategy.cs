// File: _Scripts/Controllers/AI/VeryHardAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 极难AI的决策策略。
/// 行为模式：
/// 1. 开局阶段，遵循预设的开局库。
/// 2. 脱离开局库后，优先处理将军威胁 (100% 概率)。
/// 3. 常规决策使用 Minimax 算法向前看一步，预判对手的最佳应对。
/// </summary>
public class VeryHardAIStrategy : HardAIStrategy, IAIStrategy // 继承自 HardAI 以复用评估函数
{
    // --- 开局库 ---
    private static List<List<Vector2Int>> openingBook;
    private List<Vector2Int> currentOpening;
    private int openingMoveIndex = 0;
    private bool useOpeningBook = true;

    // --- Minimax 算法深度 ---
    private const int SEARCH_DEPTH = 2; // 搜索深度：2代表(AI走一步, 玩家走一步)

    public VeryHardAIStrategy()
    {
        InitializeOpeningBook();
        SelectRandomOpening();
    }

    public override AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor)
    {
        // --- 1. 开局库阶段 ---
        if (useOpeningBook && openingMoveIndex < currentOpening.Count)
        {
            return ExecuteOpeningBookMove(gameManager, assignedColor);
        }

        // --- 脱离开局库后的逻辑 ---
        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        // --- 2. 危机检测 (100% 概率) ---
        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            Debug.Log("[AI-VeryHard] 危机：王被将军！必须救驾！");
            // 复用父类的最优救驾逻辑
            return FindBestSavingMove(gameManager, assignedColor, logicalBoard, myPieces, kingPos);
        }

        // --- 3. Minimax 常规决策 ---
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
            // 模拟我方走棋
            BoardState futureBoard = gameManager.GetLogicalBoardState().Clone();
            futureBoard.MovePiece(move.From, move.To);

            // 计算对手在我方走棋后的最佳应对所能达到的局面分数（对我们而言是最低分）
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
            Debug.Log($"[AI-VeryHard] Minimax决策：选择移动 {chosenMove.PieceToMove.name} -> {chosenMove.To} (预判得分: {bestScore})");
            return chosenMove;
        }
        return null;
    }

    private int Minimax(GameManager gameManager, BoardState board, int depth, bool isMaximizingPlayer, PlayerColor myColor)
    {
        // 基准情况：达到搜索深度或游戏结束
        if (depth == 0)
        {
            return EvaluateBoardState(board, myColor);
        }

        PlayerColor currentColor = isMaximizingPlayer ? myColor : (myColor == PlayerColor.Red ? PlayerColor.Black : PlayerColor.Red);
        var pieces = gameManager.GetAllPiecesOfColorFromBoard(currentColor, board); // 需要一个新的辅助方法
        var allMoves = GetAllPossibleMoves(currentColor, board, pieces);

        if (isMaximizingPlayer) // 我方（AI），寻求最大分
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
        else // 敌方（玩家），寻求最小分
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
    /// 评估整个棋盘的局面分数，分数越高对AI越有利。
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
                    // 注意：这里需要一个假的PieceComponent来调用GetPositionalValue，这是一个待优化的点
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
        if (openingBook != null) return; // 静态变量，只需初始化一次

        openingBook = new List<List<Vector2Int>>
        {
            // 开局1: 当头炮 (中炮) -> 跳马
            new List<Vector2Int>
            {
                new Vector2Int(1, 2), new Vector2Int(4, 2), // 黑方 炮2平5
                new Vector2Int(1, 0), new Vector2Int(2, 2)  // 黑方 马2进3
            },
            // 开局2: 飞相局 -> 跳马
            new List<Vector2Int>
            {
                new Vector2Int(2, 0), new Vector2Int(4, 2), // 黑方 相3进5
                new Vector2Int(7, 0), new Vector2Int(6, 2)  // 黑方 马8进7
            },
            // 开局3: 起马局
            new List<Vector2Int>
            {
                new Vector2Int(1, 0), new Vector2Int(2, 2)  // 黑方 马2进3
            }
            // 可以留空，或加入一个空列表来代表“随机开局”
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
        Debug.Log($"[AI-VeryHard] 已选择开局库，共 {currentOpening.Count / 2} 步。");
    }

    private AIController.MovePlan ExecuteOpeningBookMove(GameManager gameManager, PlayerColor assignedColor)
    {
        Vector2Int from = currentOpening[openingMoveIndex];
        Vector2Int to = currentOpening[openingMoveIndex + 1];

        PieceComponent pieceToMove = gameManager.BoardRenderer.GetPieceComponentAt(from);

        // 验证开局库移动是否合法 (例如，玩家走了非主流开局挡住了路)
        if (pieceToMove != null && pieceToMove.PieceData.Color == assignedColor && RuleEngine.GetValidMoves(pieceToMove.PieceData, from, gameManager.GetLogicalBoardState()).Contains(to))
        {
            Debug.Log($"[AI-VeryHard] 执行开局库第 {(openingMoveIndex / 2) + 1} 步: {from} -> {to}");
            openingMoveIndex += 2;
            return new AIController.MovePlan(pieceToMove, from, to, 0);
        }
        else
        {
            // 如果开局库移动不合法，则立即切换到思考模式
            Debug.LogWarning("[AI-VeryHard] 开局库移动不合法，脱离书本，开始自由思考！");
            useOpeningBook = false;
            return FindBestMoveWithMinimax(gameManager, assignedColor);
        }
    }
    #endregion
}