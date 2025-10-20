// File: _Scripts/Core/RealTimePieceState.cs

/// <summary>
/// �洢������ʵʱģʽ�µ����ж�̬״̬��
/// </summary>
public class RealTimePieceState
{
    // --- ״̬ö�ٶ��� ---
    public enum MovementType { Physical, Ethereal }

    // --- ����״̬���� ---
    public bool IsDead { get; set; } = false;
    public bool IsMoving { get; set; } = false;

    // ע�⣺����������ƣ�Ŀǰ�������Ӷ���ʵ�壬������д����Ϊδ������ϵͳԤ��
    public MovementType CurrentMovementType { get; set; } = MovementType.Physical;

    public bool IsVulnerable { get; set; } = true;  // �Ƿ�ɱ�����
    public bool IsAttacking { get; set; } = false; // �Ƿ������ڹ���״̬

    // --- �ƶ�����׷�� ---
    public float MoveProgress { get; set; } = 0f; // �ƶ������Ľ��� (0 to 1)

    /// <summary>
    /// ����״̬����ֹʱ��Ĭ��ֵ��
    /// </summary>
    public void ResetToDefault()
    {
        IsMoving = false;
        IsVulnerable = true;
        IsAttacking = false;
        MoveProgress = 0f;
    }
}