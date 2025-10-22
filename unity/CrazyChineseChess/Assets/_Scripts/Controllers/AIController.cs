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
    private IAIStrategy strategy; // ���о��߲��Ե�����

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

    // --- MODIFICATION START ---
    /// <summary>
    /// �ϸ��� IPlayerController �ӿ�ʵ�� Initialize ������
    /// </summary>
    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
        ResetDecisionTimer();
        Debug.Log($"[AIController] AI��������Ϊ {assignedColor} ����ʼ����");
    }

    /// <summary>
    /// ���ø�AI������ʹ�õľ��߲��ԣ����ԣ���
    /// </summary>
    /// <param name="aiStrategy">Ҫע��Ĳ���ʵ��</param>
    public void SetStrategy(IAIStrategy aiStrategy)
    {
        this.strategy = aiStrategy;
        if (this.strategy != null)
        {
            Debug.Log($"[AIController] AI����������Ϊ: {aiStrategy.GetType().Name}��");
        }
    }
    // --- MODIFICATION END ---

    private void Update()
    {
        // --- MODIFICATION START ---
        // ���Ӷ� strategy �Ƿ�Ϊ�յļ��
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
        decisionTimer = Random.Range(0.5f, 5.5f); // ������߼��1-6��
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
            Debug.Log($"[AI] �������: �ƶ� {bestMove.PieceToMove.name} �� {bestMove.From} �� {bestMove.To}��");
            gameManager.RequestMove(assignedColor, bestMove.From, bestMove.To);
        }
    }
}