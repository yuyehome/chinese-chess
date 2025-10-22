// File: _Scripts/Controllers/AIController.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 负责驱动AI行为的控制器。
/// 它作为AI的“身体”，管理决策时机；而具体的决策逻辑由注入的IAIStrategy策略（“大脑”）完成。
/// </summary>
public class AIController : MonoBehaviour, IPlayerController
{
    private PlayerColor assignedColor;
    private GameManager gameManager;
    private float decisionTimer;
    private IAIStrategy strategy; // 持有决策策略的引用

    /// <summary>
    /// 移动计划的数据结构，由策略类创建并返回。
    /// </summary>
    public class MovePlan
    {
        public PieceComponent PieceToMove;
        public Vector2Int From;
        public Vector2Int To;
        public int TargetValue;

        public MovePlan(PieceComponent piece, Vector2Int from, Vector2Int to, int value)
        {
            PieceToMove = piece;
            From = from;
            To = to;
            TargetValue = value;
        }
    }

    // --- MODIFICATION START ---
    /// <summary>
    /// 严格按照 IPlayerController 接口实现 Initialize 方法。
    /// </summary>
    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
        ResetDecisionTimer();
        Debug.Log($"[AIController] AI控制器已为 {assignedColor} 方初始化。");
    }

    /// <summary>
    /// 设置该AI控制器使用的决策策略（大脑）。
    /// </summary>
    /// <param name="aiStrategy">要注入的策略实例</param>
    public void SetStrategy(IAIStrategy aiStrategy)
    {
        this.strategy = aiStrategy;
        if (this.strategy != null)
        {
            Debug.Log($"[AIController] AI策略已设置为: {aiStrategy.GetType().Name}。");
        }
    }
    // --- MODIFICATION END ---

    private void Update()
    {
        // --- MODIFICATION START ---
        // 增加对 strategy 是否为空的检查
        if (gameManager == null || gameManager.IsGameEnded || strategy == null) return;
        // --- MODIFICATION END ---

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0)
        {
            ResetDecisionTimer();
            MakeDecision();
        }
    }

    private void ResetDecisionTimer()
    {
        decisionTimer = Random.Range(0.5f, 5.5f); // 随机决策间隔1-6秒
    }

    private void MakeDecision()
    {
        if (!gameManager.EnergySystem.CanSpendEnergy(assignedColor))
        {
            return;
        }

        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);

        if (myPieces.Count == 0) return;

        MovePlan bestMove = strategy.FindBestMove(assignedColor, logicalBoard, myPieces);

        if (bestMove != null)
        {
            Debug.Log($"[AI] 决策完成: 移动 {bestMove.PieceToMove.name} 从 {bestMove.From} 到 {bestMove.To}。");
            gameManager.RequestMove(assignedColor, bestMove.From, bestMove.To);
        }
    }
}