// File: _Scripts/Core/CombatManager.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ������ʵʱģʽ������ս����ص��߼��������з����ѷ�����ײ������˺��ж���
/// </summary>
public class CombatManager
{
    private BoardState boardState;
    private BoardRenderer boardRenderer;
    private GameManager gameManager;

    // ��ײ���ľ�����ֵ�������ƽ���������������Ż�
    private const float COLLISION_DISTANCE_SQUARED = 0.0175f * 0.0175f;

    public CombatManager(BoardState state, BoardRenderer renderer)
    {
        this.boardState = state;
        this.boardRenderer = renderer;
        this.gameManager = GameManager.Instance;
    }

    /// <summary>
    /// ÿ֡���õ����������������л�����ӽ���������Ե���ײ��⡣
    /// </summary>
    public void ProcessCombat(List<PieceComponent> allActivePieces)
    {
        for (int i = 0; i < allActivePieces.Count; i++)
        {
            for (int j = i + 1; j < allActivePieces.Count; j++)
            {
                PieceComponent pieceA = allActivePieces[i];
                PieceComponent pieceB = allActivePieces[j];

                if (pieceA == null || pieceB == null || pieceA.RTState.IsDead || pieceB.RTState.IsDead) continue;

                float sqrDist = Vector3.SqrMagnitude(pieceA.transform.localPosition - pieceB.transform.localPosition);
                if (sqrDist < COLLISION_DISTANCE_SQUARED)
                {
                    ResolveCollision(pieceA, pieceB);
                }
            }
        }
    }

    /// <summary>
    /// ��ײ�þ�����ڣ�������ײ˫������Ӫ�ַ�����ͬ�Ĵ�������
    /// </summary>
    private void ResolveCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        if (pieceA.PieceData.Color == pieceB.PieceData.Color)
        {
            ResolveFriendlyCollision(pieceA, pieceB);
        }
        else
        {
            ResolveEnemyCollision(pieceA, pieceB);
        }
    }

    /// <summary>
    /// ����з���λ����ײ�߼���
    /// </summary>
    private void ResolveEnemyCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        RealTimePieceState stateA = pieceA.RTState;
        RealTimePieceState stateB = pieceB.RTState;

        bool aCanDamageB = stateA.IsAttacking && stateB.IsVulnerable;
        bool bCanDamageA = stateB.IsAttacking && stateA.IsVulnerable;

        if (aCanDamageB && bCanDamageA)
        {
            Debug.Log($"[Combat-Enemy] ��ײ˫���� {pieceA.name} �� {pieceB.name} ͬ���ھ���");
            Kill(pieceA);
            Kill(pieceB);
        }
        else if (aCanDamageB)
        {
            Kill(pieceB);
        }
        else if (bCanDamageA)
        {
            Kill(pieceA);
        }
    }

    /// <summary>
    /// �����ѷ���λ����ײ�߼������������Ӽ�ֵ�þ����ơ�
    /// </summary>
    private void ResolveFriendlyCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        RealTimePieceState stateA = pieceA.RTState;
        RealTimePieceState stateB = pieceB.RTState;

        // �����ж�ǰ�᣺����һ�����ڹ���״̬����һ�����ڿɱ�����״̬
        if (stateA.IsAttacking && stateB.IsVulnerable || stateA.IsVulnerable && stateB.IsAttacking)
        {
            int valueA = PieceValue.GetValue(pieceA.PieceData.Type);
            int valueB = PieceValue.GetValue(pieceB.PieceData.Type);

            Debug.Log($"[Combat-Friendly] �ѷ���ײ�� {pieceA.name} (��ֵ:{valueA}) �� {pieceB.name} (��ֵ:{valueB}) ��ײ��");

            if (valueA > valueB)
            {
                Debug.Log($"[Combat-Friendly] {pieceA.name} ��ֵ���ߣ�{pieceB.name} ���ݻ١�");
                Kill(pieceB);
            }
            else if (valueB > valueA)
            {
                Debug.Log($"[Combat-Friendly] {pieceB.name} ��ֵ���ߣ�{pieceA.name} ���ݻ١�");
                Kill(pieceA);
            }
            else // ��ֵ���
            {
                Debug.Log($"[Combat-Friendly] ˫����ֵ��ȣ�ͬ���ھ���");
                Kill(pieceA);
                Kill(pieceB);
            }
        }
    }

    /// <summary>
    /// ��װ�ˡ�ɱ����һ�����ӵ����б�Ҫ������
    /// </summary>
    private void Kill(PieceComponent piece)
    {
        if (piece == null || piece.RTState.IsDead) return;

        Vector2Int currentLogicalPos = piece.RTState.LogicalPosition;
        Debug.Log($"[Combat] ���� {piece.name} ���߼����� {currentLogicalPos} ����ɱ��");

        piece.RTState.IsDead = true;

        // �������ɱ����һ����ֹ�����ӣ���Ҫ��BoardState���Ƴ�
        if (!piece.RTState.IsMoving)
        {
            boardState.RemovePieceAt(currentLogicalPos);
            Debug.Log($"[Combat-Kill] �Ѵ�BoardState�� {currentLogicalPos} �Ƴ�һ����ֹ�����ӡ�");
        }
        else
        {
            Debug.Log($"[Combat-Kill] һ���ƶ��е����ӱ���ɱ���������BoardState�����Ѳ������У���");
        }

        // ����GameObject���⽫�Զ���ֹ���ƶ�Э��
        GameObject.Destroy(piece.gameObject);

        // ����Ƿ��ɱ�˽�/˧��������򴥷���Ϸ����
        if (piece.PieceData.Type == PieceType.General)
        {
            GameStatus status = (piece.PieceData.Color == PlayerColor.Black)
                                ? GameStatus.RedWin
                                : GameStatus.BlackWin;
            gameManager.HandleEndGame(status);
        }
    }
}