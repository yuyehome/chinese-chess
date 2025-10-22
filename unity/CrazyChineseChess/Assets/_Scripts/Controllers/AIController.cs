// File: _Scripts/Controllers/AIController.cs
// (放在 _Scripts/Controllers 文件夹下)

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 负责驱动AI行为的控制器。
/// 它会根据设定的决策频率，为所控制的棋子找到最佳移动方案并提交给GameManager。
/// </summary>
public class AIController : MonoBehaviour, IPlayerController
{
    // --- 内部状态 ---
    private PlayerColor assignedColor;
    private GameManager gameManager;
    private float decisionTimer; // 决策计时器

    /// <summary>
    /// 一个简单的数据结构，用于存储一次完整的移动计划及其评估价值。
    /// </summary>
    private class MovePlan
    {
        public PieceComponent PieceToMove;
        public Vector2Int From;
        public Vector2Int To;
        public int TargetValue; // 评估价值，对于吃子来说就是被吃棋子的价值

        public MovePlan(PieceComponent piece, Vector2Int from, Vector2Int to, int value)
        {
            PieceToMove = piece;
            From = from;
            To = to;
            TargetValue = value;
        }
    }

    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
        ResetDecisionTimer();
        Debug.Log($"[AIController] AI控制器已为 {assignedColor} 方初始化。");
    }

    private void Update()
    {
        if (gameManager == null || gameManager.IsGameEnded) return;

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0)
        {
            ResetDecisionTimer();
            MakeDecision();
        }
    }

    /// <summary>
    /// 重置决策计时器到一个随机的时间间隔。
    /// </summary>
    private void ResetDecisionTimer()
    {
        decisionTimer = Random.Range(1.0f, 2.0f); // 随机决策间隔1-2秒
    }

    /// <summary>
    /// AI决策的核心入口。
    /// </summary>
    private void MakeDecision()
    {
        // 1. 检查是否有足够的能量行动
        if (!gameManager.EnergySystem.CanSpendEnergy(assignedColor))
        {
            // Debug.Log($"[AI] 能量不足 ({gameManager.EnergySystem.GetEnergy(assignedColor):F1})，跳过本轮决策。");
            return;
        }

        // 2. 寻找最佳移动
        MovePlan bestMove = FindBestMove();

        // 3. 如果找到了可行的移动，则提交请求
        if (bestMove != null)
        {
            Debug.Log($"[AI] 决策完成: 移动 {bestMove.PieceToMove.name} 从 {bestMove.From} 到 {bestMove.To}。");
            gameManager.RequestMove(assignedColor, bestMove.From, bestMove.To);
        }
        else
        {
            // Debug.Log("[AI] 无棋可走。");
        }
    }

    /// <summary>
    /// 遍历所有己方棋子，根据策略（优先吃子）找到最佳移动方案。
    /// </summary>
    /// <returns>一个MovePlan对象，如果无棋可走则返回null。</returns>
    private MovePlan FindBestMove()
    {
        var allPossibleMoves = new List<MovePlan>();
        var captureMoves = new List<MovePlan>();

        // 获取所有属于AI的棋子
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        if (myPieces.Count == 0) return null;

        BoardState logicalBoard = gameManager.GetLogicalBoardState();

        foreach (var piece in myPieces)
        {
            // AI不操作正在移动中的棋子
            if (piece.RTState != null && piece.RTState.IsMoving) continue;

            // 计算该棋子的所有合法移动
            var validTargets = RuleEngine.GetValidMoves(piece.PieceData, piece.BoardPosition, logicalBoard);
            if (validTargets.Count == 0) continue;

            foreach (var targetPos in validTargets)
            {
                Piece targetPiece = logicalBoard.GetPieceAt(targetPos);
                int targetValue = 0;

                // 如果目标点是敌方棋子，则这是一个吃子移动
                if (targetPiece.Type != PieceType.None && targetPiece.Color != assignedColor)
                {
                    targetValue = PieceValue.GetValue(targetPiece.Type);
                    captureMoves.Add(new MovePlan(piece, piece.BoardPosition, targetPos, targetValue));
                }
                else
                {
                    allPossibleMoves.Add(new MovePlan(piece, piece.BoardPosition, targetPos, 0));
                }
            }
        }

        // --- 决策逻辑 ---
        // 1. 如果有吃子的机会，优先选择吃掉价值最高的敌方棋子
        if (captureMoves.Count > 0)
        {
            Debug.Log($"[AI] 发现 {captureMoves.Count} 个吃子机会。");
            return captureMoves.OrderByDescending(m => m.TargetValue).First();
        }

        // 2. 如果没有吃子机会，从所有普通移动中随机选择一个
        if (allPossibleMoves.Count > 0)
        {
            int randomIndex = Random.Range(0, allPossibleMoves.Count);
            return allPossibleMoves[randomIndex];
        }

        // 3. 无路可走
        return null;
    }
}