// File: _Scripts/Core/RealTimePieceState.cs

/// <summary>
/// 存储棋子在实时模式下的所有动态状态。
/// </summary>
public class RealTimePieceState
{
    // --- 状态枚举定义 ---
    public enum MovementType { Physical, Ethereal }

    // --- 核心状态变量 ---
    public bool IsDead { get; set; } = false;
    public bool IsMoving { get; set; } = false;

    // 注意：根据您的设计，目前所有棋子都是实体，这里先写死，为未来技能系统预留
    public MovementType CurrentMovementType { get; set; } = MovementType.Physical;

    public bool IsVulnerable { get; set; } = true;  // 是否可被攻击
    public bool IsAttacking { get; set; } = false; // 是否正处于攻击状态

    // --- 移动过程追踪 ---
    public float MoveProgress { get; set; } = 0f; // 移动动画的进度 (0 to 1)

    /// <summary>
    /// 重置状态到静止时的默认值。
    /// </summary>
    public void ResetToDefault()
    {
        IsMoving = false;
        IsVulnerable = true;
        IsAttacking = false;
        MoveProgress = 0f;
    }
}