// File: _Scripts/Controllers/AI/HardAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 困难AI的决策策略。
/// 行为模式：优先处理将军威胁（90%概率），然后通过一个评估函数计算每个可能移动的得分，选择得分最高的移动。
/// </summary>
public class HardAIStrategy : EasyAIStrategy, IAIStrategy // 继承自EasyAI以复用FindKingPosition等辅助方法
{
    // --- 评估权重常量 ---
    private const int CAPTURE_MULTIPLIER = 10; // 吃子得分 = 棋子价值 * 10
    private const int SAVING_MULTIPLIER = 6;  // 救一个被威胁的子得分 = 棋子价值 * 6
    private const int THREATEN_MULTIPLIER = 2; // 威胁对方高价值子得分 = 棋子价值 * 2
    private const int SAFE_MOVE_BONUS = 5;     // 移动到安全位置的基础分
    private const int POSITIONAL_BONUS = 20;   // 位置优势分（如兵过河）

    /// <summary>
    /// 困难AI决策的入口方法。
    /// </summary>
    public new AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor)
    {
        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        if (myPieces.Count == 0) return null;

        // --- 第一层：危机检测 (90%概率) ---
        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            Debug.Log("[AI-Hard] 危机：王被将军！");
            if (Random.Range(0f, 1f) < 0.9f)
            {
                // 困难AI会评估所有救驾方式，并选择最优解
                var savingMove = FindBestSavingMove(gameManager, assignedColor, logicalBoard, myPieces, kingPos, opponentColor);
                if (savingMove != null)
                {
                    Debug.Log("[AI-Hard] 决策：执行最优救驾！");
                    return savingMove;
                }
            }
            else
            {
                Debug.Log("[AI-Hard] 决策：忽略了将军！(10%概率)");
            }
        }

        // --- 第三层：常规策略 (基于得分评估) ---
        List<AIController.MovePlan> allMoves = GetAllPossibleMoves(assignedColor, logicalBoard, myPieces);
        if (allMoves.Count == 0) return null;

        AIController.MovePlan bestMove = null;
        int highestScore = int.MinValue;

        // 遍历所有可能的移动，为每一个移动打分
        foreach (var move in allMoves)
        {
            int score = EvaluateMove(move, assignedColor, logicalBoard, opponentColor);

            // 为了避免AI完全静止，给所有移动一个微小的随机值，使得分相同时能有不同选择
            score += Random.Range(0, 4);

            if (score > highestScore)
            {
                highestScore = score;
                bestMove = move;
            }
        }

        if (bestMove != null)
        {
            Debug.Log($"[AI-Hard] 决策：选择最优移动 {bestMove.PieceToMove.name} -> {bestMove.To} (得分: {highestScore})");
        }
        return bestMove;
    }

    /// <summary>
    /// 评估一个移动的综合得分。
    /// </summary>
    private int EvaluateMove(AIController.MovePlan move, PlayerColor myColor, BoardState board, PlayerColor opponentColor)
    {
        int score = 0;
        int myValue = PieceValue.GetValue(move.PieceToMove.PieceData.Type);

        // 1. 进攻得分：吃掉对方棋子的价值
        score += move.TargetValue * CAPTURE_MULTIPLIER;

        // 模拟移动后的棋盘
        BoardState futureBoard = board.Clone();
        futureBoard.MovePiece(move.From, move.To);

        // 2. 安全性评估：移动后是否会立即被吃？
        bool isMoveSafe = !RuleEngine.IsPositionUnderAttack(move.To, opponentColor, futureBoard);
        if (isMoveSafe)
        {
            score += SAFE_MOVE_BONUS;
        }
        else
        {
            // 移动到一个会被吃掉的位置，这是一个非常糟糕的移动，要扣除自身价值的分数
            // 这会让AI学会“兑子”：只有当吃的子价值远高于自己时才考虑牺牲
            score -= myValue * CAPTURE_MULTIPLIER;
        }

        // 3. 防守得分：是否正在拯救一个当前被威胁的棋子？
        if (RuleEngine.IsPositionUnderAttack(move.From, opponentColor, board) && isMoveSafe)
        {
            // 如果成功从危险位置移动到了安全位置，给予大量加分
            score += myValue * SAVING_MULTIPLIER;
        }

        // 4. 威胁得分：移动后是否能将军或威胁到对方高价值棋子？
        var movesAfterMove = RuleEngine.GetValidMoves(move.PieceToMove.PieceData, move.To, futureBoard);
        foreach (var nextTarget in movesAfterMove)
        {
            Piece threatenedPiece = futureBoard.GetPieceAt(nextTarget);
            if (threatenedPiece.Color == opponentColor)
            {
                // 将军的权重最高
                if (threatenedPiece.Type == PieceType.General)
                {
                    score += 50;
                }
                else
                {
                    // 威胁其他子的得分 = 对方价值 * 威胁系数
                    score += PieceValue.GetValue(threatenedPiece.Type) * THREATEN_MULTIPLIER;
                }
            }
        }

        // 5. 位置得分
        if (move.PieceToMove.PieceData.Type == PieceType.Soldier &&
            ((myColor == PlayerColor.Red && move.To.y > 4) || (myColor == PlayerColor.Black && move.To.y < 5)))
        {
            score += POSITIONAL_BONUS; // 兵过河加分
        }

        return score;
    }

    /// <summary>
    /// 困难AI的救驾逻辑：评估所有可能的救驾方式，选择最优解。
    /// </summary>
    private AIController.MovePlan FindBestSavingMove(GameManager gameManager, PlayerColor color, BoardState board, List<PieceComponent> pieces, Vector2Int kingPos, PlayerColor opponentColor)
    {
        var savingMoves = new List<AIController.MovePlan>();

        // 方案A: 移动王到安全位置
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

        // 方案B: 吃掉正在将军的棋子
        // (为了简化，我们先找到所有攻击王的棋子)
        var attackers = FindAttackers(kingPos, opponentColor, board, gameManager);
        foreach (var attacker in attackers)
        {
            // 检查我方有哪些棋子可以吃掉这个攻击者
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

        // 方案C: 格挡
        // (格挡逻辑较为复杂，暂时不实现，但为未来留出扩展点)

        // 从所有可行的救驾方案中，选择一个最优的
        if (savingMoves.Count > 0)
        {
            // 使用评估函数为每个救驾方案打分，选择分数最高的
            return savingMoves.OrderByDescending(move => EvaluateMove(move, color, board, opponentColor)).First();
        }

        return null; // 王被绝杀，无解
    }

    /// <summary>
    /// 辅助方法：获取一个位置的所有攻击者。
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
    /// 辅助方法：获取所有可能的移动。
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