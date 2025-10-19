// File: _Scripts/Core/PieceData.cs

// 使用枚举，代码清晰且不易出错
public enum PlayerColor { None, Red, Black }
public enum PieceType { None, Chariot, Horse, Elephant, Advisor, General, Cannon, Soldier }

// 这是一个纯数据结构，不继承任何Unity的东西 (like MonoBehaviour)
// 它代表了一个棋子的逻辑状态，而不是它的视觉表现
public struct Piece
{
    public PieceType Type;
    public PlayerColor Color;

    // 构造函数，方便创建
    public Piece(PieceType type, PlayerColor color)
    {
        this.Type = type;
        this.Color = color;
    }
}


#region Game State Enums

/// <summary>
/// 枚举，用于表示游戏当前的几种状态。
/// </summary>
public enum GameStatus
{
    Ongoing,    // 游戏中
    RedWin,     // 红方胜
    BlackWin,   // 黑方胜
    Stalemate   // 和棋
}

#endregion