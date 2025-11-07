// 文件路径: Assets/Scripts/_Core/Data/DataEnums.cs

using System;

// 玩家阵营
public enum PlayerTeam
{
    Red,
    Black
}

// 棋子类型
public enum PieceType
{
    General,   // 将/帅
    Advisor,   // 士
    Elephant,  // 象
    Horse,     // 马
    Chariot,   // 车
    Cannon,    // 炮
    Soldier    // 兵
}

/// <summary>
/// 使用Flags特性，允许棋子状态进行位运算组合。
/// 例如: status = PieceStatus.IsMoving | PieceStatus.IsAttacking;
/// </summary>
[Flags]
public enum PieceStatus
{
    // 基础状态
    None = 0,           // 默认/死亡状态
    IsMoving = 1 << 1,  // 正在移动
    IsFlying = 1 << 2,  // 处于“空中”阶段 (马、象、炮移动时)

    // 属性状态 (可组合)
    IsAttackable = 1 << 8,  // 可被攻击
    IsAttacking = 1 << 9,   // 正在进行攻击判定
    IsObstacle = 1 << 10, // 是一个实体障碍物

    // 效果状态 (可组合)
    Stunned = 1 << 16,  // 被眩晕
    Invisible = 1 << 17 // 隐身
}
