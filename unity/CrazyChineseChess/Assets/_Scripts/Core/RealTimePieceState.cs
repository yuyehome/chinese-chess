
// File: _Scripts/Core/RealTimePieceState.cs

using UnityEngine;

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

    // 记录移动的起点和终点
    public Vector2Int MoveStartPos { get; set; }
    public Vector2Int MoveEndPos { get; set; }

    /// <summary>
    /// 棋子在移动过程中的当前逻辑坐标。
    /// 对于静止棋子，它等于其在BoardState中的位置。
    /// </summary>
    public Vector2Int LogicalPosition { get; set; }

    /// <summary>
    /// 将状态重置为棋子静止时的默认值。
    /// </summary>
    public void ResetToDefault(Vector2Int finalPosition) // 【修复】添加缺失的 finalPosition 参数
    {
        IsMoving = false;
        IsVulnerable = true;
        IsAttacking = false;
        MoveProgress = 0f;
        LogicalPosition = finalPosition;
    }
}