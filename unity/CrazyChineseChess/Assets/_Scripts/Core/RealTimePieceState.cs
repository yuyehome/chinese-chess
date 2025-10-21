
// File: _Scripts/Core/RealTimePieceState.cs

using UnityEngine;

/// <summary>
/// �洢������ʵʱģʽ�µ����ж�̬״̬��
/// ÿ�����ӵ�PieceComponent��ʵʱģʽ�¶������һ�������ʵ����
/// </summary>
public class RealTimePieceState
{
    /// <summary>
    /// �������ӵ��ƶ����ͣ�Ϊδ�����ܣ�������Ԥ����
    /// </summary>
    public enum MovementType { Physical, Ethereal }

    // --- ����״̬���� ---
    public bool IsDead { get; set; } = false;
    public bool IsMoving { get; set; } = false;

    // ��ǰ����У��������Ӷ���ʵ���ƶ���������Ϊδ����չ������
    public MovementType CurrentMovementType { get; set; } = MovementType.Physical;

    public bool IsVulnerable { get; set; } = true;  // �Ƿ�ɱ�����
    public bool IsAttacking { get; set; } = false; // �Ƿ������ڹ���״̬

    // --- �ƶ�����׷�� ---
    public float MoveProgress { get; set; } = 0f; // �ƶ������Ľ��� (0.0 to 1.0)

    // ��¼�ƶ��������յ�
    public Vector2Int MoveStartPos { get; set; }
    public Vector2Int MoveEndPos { get; set; }

    /// <summary>
    /// �������ƶ������еĵ�ǰ�߼����ꡣ
    /// ���ھ�ֹ���ӣ�����������BoardState�е�λ�á�
    /// </summary>
    public Vector2Int LogicalPosition { get; set; }

    /// <summary>
    /// ��״̬����Ϊ���Ӿ�ֹʱ��Ĭ��ֵ��
    /// </summary>
    public void ResetToDefault(Vector2Int finalPosition) // ���޸������ȱʧ�� finalPosition ����
    {
        IsMoving = false;
        IsVulnerable = true;
        IsAttacking = false;
        MoveProgress = 0f;
        LogicalPosition = finalPosition;
    }
}