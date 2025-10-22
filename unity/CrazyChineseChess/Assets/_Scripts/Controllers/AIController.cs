// File: _Scripts/Controllers/AIController.cs
// (���� _Scripts/Controllers �ļ�����)

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ��������AI��Ϊ�Ŀ�������
/// ��������趨�ľ���Ƶ�ʣ�Ϊ�����Ƶ������ҵ�����ƶ��������ύ��GameManager��
/// </summary>
public class AIController : MonoBehaviour, IPlayerController
{
    // --- �ڲ�״̬ ---
    private PlayerColor assignedColor;
    private GameManager gameManager;
    private float decisionTimer; // ���߼�ʱ��

    /// <summary>
    /// һ���򵥵����ݽṹ�����ڴ洢һ���������ƶ��ƻ�����������ֵ��
    /// </summary>
    private class MovePlan
    {
        public PieceComponent PieceToMove;
        public Vector2Int From;
        public Vector2Int To;
        public int TargetValue; // ������ֵ�����ڳ�����˵���Ǳ������ӵļ�ֵ

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
        Debug.Log($"[AIController] AI��������Ϊ {assignedColor} ����ʼ����");
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
    /// ���þ��߼�ʱ����һ�������ʱ������
    /// </summary>
    private void ResetDecisionTimer()
    {
        decisionTimer = Random.Range(1.0f, 2.0f); // ������߼��1-2��
    }

    /// <summary>
    /// AI���ߵĺ�����ڡ�
    /// </summary>
    private void MakeDecision()
    {
        // 1. ����Ƿ����㹻�������ж�
        if (!gameManager.EnergySystem.CanSpendEnergy(assignedColor))
        {
            // Debug.Log($"[AI] �������� ({gameManager.EnergySystem.GetEnergy(assignedColor):F1})���������־��ߡ�");
            return;
        }

        // 2. Ѱ������ƶ�
        MovePlan bestMove = FindBestMove();

        // 3. ����ҵ��˿��е��ƶ������ύ����
        if (bestMove != null)
        {
            Debug.Log($"[AI] �������: �ƶ� {bestMove.PieceToMove.name} �� {bestMove.From} �� {bestMove.To}��");
            gameManager.RequestMove(assignedColor, bestMove.From, bestMove.To);
        }
        else
        {
            // Debug.Log("[AI] ������ߡ�");
        }
    }

    /// <summary>
    /// �������м������ӣ����ݲ��ԣ����ȳ��ӣ��ҵ�����ƶ�������
    /// </summary>
    /// <returns>һ��MovePlan���������������򷵻�null��</returns>
    private MovePlan FindBestMove()
    {
        var allPossibleMoves = new List<MovePlan>();
        var captureMoves = new List<MovePlan>();

        // ��ȡ��������AI������
        List<PieceComponent> myPieces = gameManager.GetAllPiecesOfColor(assignedColor);
        if (myPieces.Count == 0) return null;

        BoardState logicalBoard = gameManager.GetLogicalBoardState();

        foreach (var piece in myPieces)
        {
            // AI�����������ƶ��е�����
            if (piece.RTState != null && piece.RTState.IsMoving) continue;

            // ��������ӵ����кϷ��ƶ�
            var validTargets = RuleEngine.GetValidMoves(piece.PieceData, piece.BoardPosition, logicalBoard);
            if (validTargets.Count == 0) continue;

            foreach (var targetPos in validTargets)
            {
                Piece targetPiece = logicalBoard.GetPieceAt(targetPos);
                int targetValue = 0;

                // ���Ŀ����ǵз����ӣ�������һ�������ƶ�
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

        // --- �����߼� ---
        // 1. ����г��ӵĻ��ᣬ����ѡ��Ե���ֵ��ߵĵз�����
        if (captureMoves.Count > 0)
        {
            Debug.Log($"[AI] ���� {captureMoves.Count} �����ӻ��ᡣ");
            return captureMoves.OrderByDescending(m => m.TargetValue).First();
        }

        // 2. ���û�г��ӻ��ᣬ��������ͨ�ƶ������ѡ��һ��
        if (allPossibleMoves.Count > 0)
        {
            int randomIndex = Random.Range(0, allPossibleMoves.Count);
            return allPossibleMoves[randomIndex];
        }

        // 3. ��·����
        return null;
    }
}