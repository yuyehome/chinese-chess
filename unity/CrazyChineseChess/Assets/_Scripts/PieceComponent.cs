// File: _Scripts/PieceComponent.cs

using UnityEngine; // <--- 必须有这一行

public class PieceComponent : MonoBehaviour
{
    public Vector2Int BoardPosition { get; set; }
    public Piece PieceData { get; set; }

    // ================== 新增代码开始 ==================
    /// <summary>
    /// 仅在实时模式下使用的棋子动态状态。
    /// 在回合制模式下，这个值将是 null。
    /// </summary>
    public RealTimePieceState RTState { get; set; }
    // ================== 新增代码结束 ==================
}