/// <summary>
/// 存储棋子在实时模式下的所有动态状态。
/// 每个棋子的PieceComponent在实时模式下都会持有一个该类的实例。
/// </summary>
public class RealTimePieceState
{
    /// <summary>
    /// 定义棋子的移动类型，为未来技能（如隐身）预留。
    /// </summary>
    public enum MovementType { Physical, Ethereal }

    // --- 核心状态变量 ---
    public bool IsDead { get; set; } = false;
    public bool IsMoving { get; set; } = false;

    // 当前设计中，所有棋子都是实体移动。此属性为未来扩展保留。
    public MovementType CurrentMovementType { get; set; } = MovementType.Physical;

    public bool IsVulnerable { get; set; } = true;  // 是否可被攻击
    public bool IsAttacking { get; set; } = false; // 是否正处于攻击状态

    // --- 移动过程追踪 ---
    public float MoveProgress { get; set; } = 0f; // 移动动画的进度 (0.0 to 1.0)

    /// <summary>
    /// 将状态重置为棋子静止时的默认值。
    /// </summary>
    public void ResetToDefault()
    {
        IsMoving = false;
        IsVulnerable = true;
        IsAttacking = false;
        MoveProgress = 0f;
    }
}