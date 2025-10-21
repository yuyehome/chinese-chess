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

    #region Core State Variables
    public bool IsDead { get; set; } = false;
    public bool IsMoving { get; set; } = false;
    public bool IsVulnerable { get; set; } = true;
    public bool IsAttacking { get; set; } = false;
    public MovementType CurrentMovementType { get; set; } = MovementType.Physical;
    #endregion

    #region Movement Tracking
    // �ƶ������Ĺ�һ������ (0.0 to 1.0)
    public float MoveProgress { get; set; } = 0f;
    // �ƶ��������յ��߼�����
    public Vector2Int MoveStartPos { get; set; }
    public Vector2Int MoveEndPos { get; set; }
    // �������ƶ������еĵ�ǰ�߼�����
    public Vector2Int LogicalPosition { get; set; }
    #endregion

    /// <summary>
    /// ��״̬����Ϊ���Ӿ�ֹʱ��Ĭ��ֵ��
    /// </summary>
    /// <param name="finalPosition">���Ӿ�ֹ�������λ��</param>
    public void ResetToDefault(Vector2Int finalPosition)
    {
        // ��������������򲻽����κ�״̬���ã���ֹ�����
        if (IsDead) return;

        IsMoving = false;
        IsVulnerable = true;
        IsAttacking = false;
        MoveProgress = 0f;
        LogicalPosition = finalPosition;
    }
}