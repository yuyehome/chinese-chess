// File: _Scripts/Core/PieceValue.cs

/// <summary>
/// 提供与棋子价值相关的静态数据和方法。
/// </summary>
public static class PieceValue
{
    /// <summary>
    /// 获取一个棋子类型的价值。数值越高，价值越大。
    /// </summary>
    public static int GetValue(PieceType type)
    {
        switch (type)
        {
            case PieceType.General: return 100; // 将/帅 
            case PieceType.Chariot: return 90;   // 车
            case PieceType.Cannon: return 70;   // 炮
            case PieceType.Horse: return 50;   // 马
            case PieceType.Elephant: return 30;   // 象
            case PieceType.Advisor: return 20;   // 士
            case PieceType.Soldier: return 10;   // 兵
            default: return 0;   // 无
        }
    }
}