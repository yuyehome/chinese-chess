// 文件路径: Assets/Scripts/_Core/Data/DataEnums.cs

using System;

// 玩家阵营
public enum PlayerTeam
{
    None = -1, // 用于表示和棋或无归属
    Red,
    Black,
    Blue,   // 为2v2预留，黑队友
    Purple   // 为2v2预留，红队友
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
    IsAlive = 1,        // 棋子存活在场上
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
