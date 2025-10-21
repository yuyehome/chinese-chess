// File: _Scripts/Core/PieceData.cs

/// <summary>
/// 玩家颜色枚举。
/// </summary>
public enum PlayerColor { None, Red, Black }

/// <summary>
/// 棋子类型枚举。
/// </summary>
public enum PieceType { None, Chariot, Horse, Elephant, Advisor, General, Cannon, Soldier }

/// <summary>
/// 代表一个棋子逻辑状态的纯数据结构体。
/// 它只定义了棋子的“身份”（类型和颜色），不包含任何行为或Unity组件。
/// </summary>
public struct Piece
{
    public PieceType Type;
    public PlayerColor Color;

    /// <summary>
    /// 构造一个新的棋子实例。
    /// </summary>
    public Piece(PieceType type, PlayerColor color)
    {
        this.Type = type;
        this.Color = color;
    }
}


/// <summary>
/// 枚举，用于表示游戏当前的几种结束状态。
/// </summary>
public enum GameStatus
{
    Ongoing,    // 游戏中
    RedWin,     // 红方胜
    BlackWin,   // 黑方胜
    Stalemate   // 和棋
}