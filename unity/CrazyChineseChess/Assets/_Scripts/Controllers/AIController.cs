// File: _Scripts/Controllers/AIController.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ��������AI��Ϊ�Ŀ�������
/// ����ΪAI�ġ����塱���������ʱ����������ľ����߼���ע���IAIStrategy���ԣ������ԡ�����ɡ�
/// </summary>
public class AIController : MonoBehaviour, IPlayerController
{
    private PlayerColor assignedColor;
    private GameManager gameManager;
    private float decisionTimer;
    private IAIStrategy strategy;
    private Vector2 decisionTimeRange;
    private bool isSetup = false; // ���AI�Ƿ����������

    /// <summary>
    /// �ƶ��ƻ������ݽṹ���ɲ����ഴ�������ء�
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
    /// ʵ����IPlayerController�ӿڵı�׼��ʼ��������
    /// </summary>
    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
    }

    /// <summary>
    /// AI���е����÷���������ע����߲��ԺͲ�����
    /// </summary>
    public void SetupAI(IAIStrategy aiStrategy, Vector2 timeRange)
    {
        this.strategy = aiStrategy;
        this.decisionTimeRange = timeRange;
        ResetDecisionTimer();
        isSetup = true;
        Debug.Log($"[AIController] AI��������Ϊ {assignedColor} ��������ɣ�ʹ�ò���: {aiStrategy.GetType().Name}������Ƶ��: {timeRange.x}-{timeRange.y}s��");
    }

    private void Update()
    {
        // ȷ��AI�������������Ϸ���ڽ���
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
            Debug.Log($"[AI] �������: �ƶ� {bestMove.PieceToMove.name} �� {bestMove.From} �� {bestMove.To}��");
            gameManager.RequestMove(assignedColor, bestMove.From, bestMove.To);
        }
    }
}