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
    private IAIStrategy strategy;
    private Vector2 decisionTimeRange;
    private bool isSetup = false; // 标记AI是否已配置完成

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

    /// <summary>
    /// 实现了IPlayerController接口的标准初始化方法。
    /// </summary>
    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
    }

    /// <summary>
    /// AI特有的配置方法，用于注入决策策略和参数。
    /// </summary>
    public void SetupAI(IAIStrategy aiStrategy, Vector2 timeRange)
    {
        this.strategy = aiStrategy;
        this.decisionTimeRange = timeRange;
        ResetDecisionTimer();
        isSetup = true;
        Debug.Log($"[AIController] AI控制器已为 {assignedColor} 方配置完成，使用策略: {aiStrategy.GetType().Name}，决策频率: {timeRange.x}-{timeRange.y}s。");
    }

    private void Update()
    {
        // 确保AI已配置完成且游戏正在进行
        if (!isSetup || gameManager == null || gameManager.IsGameEnded) return;

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0)
        {
            ResetDecisionTimer();
            MakeDecision();
        }
    }

    private void ResetDecisionTimer()
    {
        if (decisionTimeRange == Vector2.zero) return;
        decisionTimer = Random.Range(decisionTimeRange.x, decisionTimeRange.y);
    }

    private void MakeDecision()
    {
        if (!gameManager.EnergySystem.CanSpendEnergy(assignedColor))
        {
            return;
        }

        MovePlan bestMove = strategy.FindBestMove(gameManager, assignedColor);

        if (bestMove != null)
        {
            Debug.Log($"[AI] 决策完成: 移动 {bestMove.PieceToMove.name} 从 {bestMove.From} 到 {bestMove.To}。");
            gameManager.RequestMove(assignedColor, bestMove.From, bestMove.To);
        }
    }
}