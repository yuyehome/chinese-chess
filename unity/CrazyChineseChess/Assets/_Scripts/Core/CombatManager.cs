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

    private const float COLLISION_DISTANCE_SQUARED = 0.0175f * 0.0175f;

    public CombatManager(BoardState state, BoardRenderer renderer)
    {
        this.boardState = state;
        this.boardRenderer = renderer;
        this.gameManager = GameManager.Instance; // ��ȡGameManager������
    }

    /// <summary>
    /// ÿ֡���õ��������������ڼ�Ⲣ�������п��ܵ�ս����
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

                // ���޸ġ��Ƴ��������ѷ���λ���ж�
                // if (pieceA.PieceData.Color == pieceB.PieceData.Color) continue;

                float sqrDist = Vector3.SqrMagnitude(pieceA.transform.localPosition - pieceB.transform.localPosition);
                if (sqrDist < COLLISION_DISTANCE_SQUARED)
                {
                    ResolveCollision(pieceA, pieceB);
                }
            }
        }
    }

    /// <summary>
    /// �����ع���������������֮�����ײ����������з����ѷ��߼���
    /// </summary>
    private void ResolveCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        // ������ѷ���λ��ײ
        if (pieceA.PieceData.Color == pieceB.PieceData.Color)
        {
            ResolveFriendlyCollision(pieceA, pieceB);
        }
        // ����ǵз���λ��ײ
        else
        {
            ResolveEnemyCollision(pieceA, pieceB);
        }
    }

    /// <summary>
    /// ������������з���λ����ײ�߼���
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
    /// �������������ѷ���λ����ײ�߼���
    /// </summary>
    private void ResolveFriendlyCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        RealTimePieceState stateA = pieceA.RTState;
        RealTimePieceState stateB = pieceB.RTState;

        //ֻҪ�����κ�һ�������������ʹ����߼��������Զ���������˧������ȥ����������˧��ֹ�����ڹ���״̬�����ᱻ��ɱ
        if (stateA.IsAttacking && stateB.IsVulnerable || stateA.IsVulnerable && stateB.IsAttacking) 
        {
            int valueA = PieceValue.GetValue(pieceA.PieceData.Type);
            int valueB = PieceValue.GetValue(pieceB.PieceData.Type);

            Debug.Log($"[Combat-Friendly] �ѷ���ײ�� {pieceA.name} (��ֵ:{valueA}) �� {pieceB.name} (��ֵ:{valueB}) ��ײ��");

            if (valueA > valueB)
            {
                // A��ֵ���ߣ�B����
                Debug.Log($"[Combat-Friendly] {pieceA.name} ��ֵ���ߣ�{pieceB.name} ���ݻ١�");
                Kill(pieceB);
            }
            else if (valueB > valueA)
            {
                // B��ֵ���ߣ�A����
                Debug.Log($"[Combat-Friendly] {pieceB.name} ��ֵ���ߣ�{pieceA.name} ���ݻ١�");
                Kill(pieceA);
            }
            else // ��ֵ���
            {
                // ��ֵ��ȣ���˫����ײ����ͬ���ھ�
                Debug.Log($"[Combat-Friendly] ˫����ֵ��ȣ�ͬ���ھ���");
                Kill(pieceA);
                Kill(pieceB);
            }
        }
    }

    /// <summary>
    /// ��װ��ɱ��һ�����ӵ����б�Ҫ������
    /// </summary>
    private void Kill(PieceComponent piece)
    {
        if (piece == null || piece.RTState.IsDead) return;

        Vector2Int currentLogicalPos = piece.RTState.LogicalPosition;
        Debug.Log($"[Combat] ���� {piece.name} ���߼����� {currentLogicalPos} ����ɱ��");

        piece.RTState.IsDead = true;


        // �������޸ġ�CombatManager����ֱ���޸�BoardState��
        // BoardStateֻ��¼��ֹ���ӡ�һ���ƶ��е����ӱ���ɱ��������Ҳ�Ͳ���BoardState�
        // �������ɱ����һ����ֹ�����ӣ�������Ҫ�Ƴ�����
        if (!piece.RTState.IsMoving)
        {
            boardState.RemovePieceAt(currentLogicalPos);
            Debug.Log($"[Combat-Kill] �Ѵ�BoardState�� {currentLogicalPos} �Ƴ�һ����ֹ�����ӡ�");
        }
        else
        {
            Debug.Log($"[Combat-Kill] һ���ƶ��е����ӱ���ɱ���������BoardState��");
        }

        // ����GameObject�Ĳ������ֲ��䣬�����ֹ����Э��
        GameObject.Destroy(piece.gameObject);

        // ����Ƿ��ɱ�˽�/˧��������������Ϸ
        if (piece.PieceData.Type == PieceType.General)
        {
            GameStatus status = (piece.PieceData.Color == PlayerColor.Black)
                                ? GameStatus.RedWin
                                : GameStatus.BlackWin;
            gameManager.HandleEndGame(status);
        }

        // TODO: ���������ﴥ��������Ч����Ч��
    }
}