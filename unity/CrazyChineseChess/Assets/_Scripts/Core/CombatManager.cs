// File: _Scripts/Core/CombatManager.cs

using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ������ʵʱģʽ������ս����ص��߼�������ײ�����˺��ж���
/// </summary>
public class CombatManager
{
    private BoardState boardState;
    private BoardRenderer boardRenderer;
    private GameManager gameManager;

    // ��ײ���ľ�����ֵ�����Ӱ뾶ԼΪ0.0175f����������Сһ���ֵ
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

        // ִ��������Ե���ײ���
        for (int i = 0; i < allActivePieces.Count; i++)
        {
            for (int j = i + 1; j < allActivePieces.Count; j++)
            {
                PieceComponent pieceA = allActivePieces[i];
                PieceComponent pieceB = allActivePieces[j];

                // ������Ч�������������ӣ�������ͬһ��������
                if (pieceA == null || pieceB == null || pieceA.RTState.IsDead || pieceB.RTState.IsDead) continue;
                if (pieceA.PieceData.Color == pieceB.PieceData.Color) continue;

                // �����������Ƿ��㹻���Է�����ײ
                float sqrDist = Vector3.SqrMagnitude(pieceA.transform.position - pieceB.transform.position);

                //if (Time.frameCount % 120 == 0)
                //{
                //    // ����һ�� if �жϣ�ֻ�����ǹ��ĵ���������֮���ӡ��־
                //    if ((pieceA.name == "Red_Cannon_7_2" && pieceB.name == "Black_Horse_7_9") ||
                //    (pieceA.name == "Black_Horse_7_9" && pieceB.name == "Red_Cannon_7_2"))
                //    {
                //        // ��������ǿ�����������ӡ��������Ҫ�����ж�
                //        Debug.Log($"[Debug-Distance] " +
                //                  $"{pieceA.name} vs {pieceB.name}. " +
                //                  $"����ƽ��: {sqrDist}. " +
                //                  $"A����: {pieceA.transform.position}, " +
                //                  $"B����: {pieceB.transform.position}");
                //    }

                //}

                if (sqrDist < COLLISION_DISTANCE_SQUARED)
                {
                    Debug.Log($"[Combat-Check] ��⵽��ײ! ����A: {pieceA.name}, ����B: {pieceB.name}, ����ƽ��: {sqrDist}");
                    ResolveCollision(pieceA, pieceB);
                }
            }
        }
    }

    /// <summary>
    /// ������������֮�����ײ�����
    /// </summary>
    private void ResolveCollision(PieceComponent pieceA, PieceComponent pieceB)
    {
        RealTimePieceState stateA = pieceA.RTState;
        RealTimePieceState stateB = pieceB.RTState;

        Debug.Log($"[Combat-Resolve] -- ����A ({pieceA.name}) ״̬: IsAttacking={stateA.IsAttacking}, IsVulnerable={stateA.IsVulnerable}");
        Debug.Log($"[Combat-Resolve] -- ����B ({pieceB.name}) ״̬: IsAttacking={stateB.IsAttacking}, IsVulnerable={stateB.IsVulnerable}");

        // ����˫���Ĺ���������״̬���ж��˺�
        bool aCanDamageB = stateA.IsAttacking && stateB.IsVulnerable;
        bool bCanDamageA = stateB.IsAttacking && stateA.IsVulnerable;

        if (aCanDamageB && bCanDamageA)
        {
            // ˫��������������ͬ���ھ�
            Debug.Log($"[Combat] ��ײ˫���� {pieceA.name} �� {pieceB.name} ͬ���ھ���");
            Kill(pieceA);
            Kill(pieceB);
        }
        else if (aCanDamageB)
        {
            // ֻ��A���˺�B
            Kill(pieceB);
        }
        else if (bCanDamageA)
        {
            // ֻ��B���˺�A
            Kill(pieceA);
        }
    }

    /// <summary>
    /// ��װ��ɱ��һ�����ӵ����б�Ҫ������
    /// </summary>
    private void Kill(PieceComponent piece)
    {
        if (piece == null || piece.RTState.IsDead) return;

        // �������޸ġ��������߼�����֮ǰ��������GameObject��
        // ��ᴥ��Э���е�null��飬�Ӷ�����ֹͣ������
        // ������Ҫ���ӵ�ǰ���߼�λ�����Ƴ���������δ����Ŀ��λ�á�
        Vector2Int currentLogicalPos = piece.RTState.LogicalPosition;

        Debug.Log($"[Combat] ���� {piece.name} ������ {piece.BoardPosition} ����ɱ��");

        // 1. ���Ϊ����״̬
        piece.RTState.IsDead = true;

        // 2. �Ƴ��Ӿ�����
        // ע�⣺����ֱ������GameObject��������ͨ��BoardRenderer�ķ���
        // ��ΪBoardRenderer�ķ�������pieceObjects���飬���Ǹ������ڷ����в�׼
        GameObject.Destroy(piece.gameObject);

        // 3. ���������߼����� (ʹ����ȷ�ĵ�ǰλ��)
        boardState.RemovePieceAt(currentLogicalPos);
        Debug.Log($"[Combat-Kill] �Ѵ�BoardState�� {currentLogicalPos} �Ƴ����ӡ�");


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