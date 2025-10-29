// File: _Scripts/PieceComponent.cs

using UnityEngine;

/// <summary>
/// 挂载在棋子Prefab上的“身份证”组件。
/// 它作为桥梁，连接了棋子的视觉表现(GameObject)和其背后的多种逻辑数据。
/// </summary>
public class PieceComponent : MonoBehaviour
{
    /// <summary>
    /// 棋子在棋盘逻辑坐标系中的位置。
    /// 对于静止棋子，这是它在BoardState中的位置；对于移动中棋子，这是它的目标位置。
    /// </summary>
    public Vector2Int BoardPosition { get; set; }

    /// <summary>
    /// 棋子的纯数据定义（类型、颜色）。
    /// </summary>
    public Piece PieceData { get; set; }

    /// <summary>
    /// 仅在实时模式下使用的棋子动态状态对象。
    /// 在回合制模式下，这个值将保持为 null，从而实现模式隔离。
    /// </summary>
    public RealTimePieceState RTState { get; set; }
}