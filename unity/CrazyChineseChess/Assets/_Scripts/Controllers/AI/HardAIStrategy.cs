// File: _Scripts/Controllers/AI/HardAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 困难AI的决策策略。
/// 行为模式：优先处理将军威胁，然后通过一个评估函数计算每个可能移动的得分，选择得分最高的移动。
/// </summary>
public class HardAIStrategy : BaseAIStrategy, IAIStrategy
{
    // --- 评估分数常量 ---
    private const int CAPTURE_MULTIPLIER = 10;      // 吃子基础分的乘数
    private const int THREAT_MULTIPLIER = 2;        // 威胁对方棋子的乘数
    private const int SAVING_MULTIPLIER = 8;        // 拯救己方棋子的乘数
    private const int CENTER_CONTROL_BONUS = 5;     // 占据中心位置的奖励
    private const int SAFE_MOVE_BONUS = 2;          // 移动到安全位置的奖励

    /// <summary>
    /// 困难AI决策的主入口。
    /// </summary>
    public virtual AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor)
    {
        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        // --- 第一层：危机检测 (90%概率) ---
        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            Debug.Log("[AI-Hard] 危机：王被将军！");
            if (Random.Range(0f, 1f) < 0.9f)
            {
                var savingMove = FindBestSavingMove(gameManager, assignedColor, logicalBoard, myPieces, kingPos);
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

        int highestScore = int.MinValue;
        var bestMoves = new List<AIController.MovePlan>();

        foreach (var move in allMoves)
        {
            // 为每个移动打分
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
            // 从所有得分最高的移动中随机选择一个
            var bestMove = bestMoves[Random.Range(0, bestMoves.Count)];
            Debug.Log($"[AI-Hard] 决策：选择最优移动 {bestMove.PieceToMove.name} -> {bestMove.To} (得分: {highestScore})");
            return bestMove;
        }

        return null;
    }

    /// <summary>
    /// 评估一个移动的综合得分。设为 protected virtual 以便子类复用和扩展。
    /// </summary>
    protected virtual int EvaluateMove(AIController.MovePlan move, PlayerColor myColor, BoardState board, PlayerColor opponentColor)
    {
        int score = 0;

        // 1. 进攻得分：吃掉对方棋子的价值
        score += move.TargetValue * CAPTURE_MULTIPLIER;

        // 模拟移动后的棋盘
        BoardState futureBoard = board.Clone();
        futureBoard.MovePiece(move.From, move.To);

        // 2. 主动威胁得分：移动后能攻击到哪些新的敌方棋子
        var newThreats = RuleEngine.GetValidMoves(move.PieceToMove.PieceData, move.To, futureBoard);
        foreach (var threatenedPos in newThreats)
        {
            Piece threatenedPiece = futureBoard.GetPieceAt(threatenedPos);
            if (threatenedPiece.Type != PieceType.None && threatenedPiece.Color == opponentColor)
            {
                score += PieceValue.GetValue(threatenedPiece.Type) * THREAT_MULTIPLIER;
            }
        }

        // 3. 安全性评估：移动后是否会立即被吃？
        if (RuleEngine.IsPositionUnderAttack(move.To, opponentColor, futureBoard))
        {
            int myValue = PieceValue.GetValue(move.PieceToMove.PieceData.Type);
            // 这是一个糟糕的移动，除非是为了兑掉更高价值的子
            score -= myValue * CAPTURE_MULTIPLIER;
        }
        else
        {
            score += SAFE_MOVE_BONUS; // 安全移动加分
        }

        // 4. 防守得分：是否正在拯救一个被威胁的棋子？
        if (RuleEngine.IsPositionUnderAttack(move.From, opponentColor, board))
        {
            int myValue = PieceValue.GetValue(move.PieceToMove.PieceData.Type);
            score += myValue * SAVING_MULTIPLIER;
        }

        // 5. 位置得分
        score += GetPositionalValue(move.PieceToMove, move.To, myColor);

        return score;
    }

    /// <summary>
    /// 获取一个棋子移动到特定位置的价值。设为 protected virtual 以便子类复用和扩展。
    /// </summary>
    protected virtual int GetPositionalValue(PieceComponent piece, Vector2Int pos, PlayerColor myColor)
    {
        int value = 0;

        // a. 中心控制: 占据中路(x=3,4,5)的棋子更有价值
        if (pos.x >= 3 && pos.x <= 5)
        {
            value += CENTER_CONTROL_BONUS;
        }

        // b. 兵线压制: 兵越过河，越深入，价值越高
        if (piece.PieceData.Type == PieceType.Soldier)
        {
            if (myColor == PlayerColor.Red && pos.y > 4)
            {
                value += pos.y * 3; // 深入敌阵的兵得分更高
            }
            else if (myColor == PlayerColor.Black && pos.y < 5)
            {
                value += (9 - pos.y) * 3;
            }
        }

        return value;
    }

    /// <summary>
    /// 困难AI的救驾逻辑：评估所有可能的救驾方式，选择最优解。
    /// </summary>
    protected AIController.MovePlan FindBestSavingMove(GameManager gameManager, PlayerColor color, BoardState board, List<PieceComponent> pieces, Vector2Int kingPos)
    {
        // 目前暂时复用简单AI的救驾逻辑：只移动王
        // TODO: 未来可以增加格挡和反击的救驾方式评估
        PieceComponent kingPiece = gameManager.BoardRenderer.GetPieceComponentAt(kingPos);
        if (kingPiece == null)
        {
            // 如果在BoardRenderer中找不到，可能是因为王正在移动中，这是一种边缘情况。
            // 此时我们应该从传入的pieces列表中查找
            kingPiece = pieces.FirstOrDefault(p => p.PieceData.Type == PieceType.General);
            if (kingPiece == null) return null;
        }

        PlayerColor opponentColor = (color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        var validKingMoves = RuleEngine.GetValidMoves(kingPiece.PieceData, kingPos, board);
        var safeKingMoves = validKingMoves.Where(move => !RuleEngine.IsPositionUnderAttack(move, opponentColor, board)).ToList();

        if (safeKingMoves.Count > 0)
        {
            return new AIController.MovePlan(kingPiece, kingPos, safeKingMoves[Random.Range(0, safeKingMoves.Count)], 10000); // 救驾得分极高
        }
        return null;
    }
}