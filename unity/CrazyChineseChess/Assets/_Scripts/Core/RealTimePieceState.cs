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

    #region Core State Variables
    public bool IsDead { get; set; } = false;
    public bool IsMoving { get; set; } = false;
    public bool IsVulnerable { get; set; } = true;
    public bool IsAttacking { get; set; } = false;
    public MovementType CurrentMovementType { get; set; } = MovementType.Physical;
    #endregion

    #region Movement Tracking
    // 移动动画的归一化进度 (0.0 to 1.0)
    public float MoveProgress { get; set; } = 0f;
    // 移动的起点和终点逻辑坐标
    public Vector2Int MoveStartPos { get; set; }
    public Vector2Int MoveEndPos { get; set; }
    // 棋子在移动过程中的当前逻辑坐标
    public Vector2Int LogicalPosition { get; set; }
    #endregion

    /// <summary>
    /// 将状态重置为棋子静止时的默认值。
    /// </summary>
    /// <param name="finalPosition">棋子静止后的最终位置</param>
    public void ResetToDefault(Vector2Int finalPosition)
    {
        // 如果棋子已死，则不进行任何状态重置，防止“复活”
        if (IsDead) return;

        IsMoving = false;
        IsVulnerable = true;
        IsAttacking = false;
        MoveProgress = 0f;
        LogicalPosition = finalPosition;
    }
}