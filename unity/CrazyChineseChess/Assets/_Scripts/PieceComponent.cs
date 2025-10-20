using UnityEngine;

/// <summary>
/// 挂载在棋子Prefab上的“身份证”组件。
/// 它连接了棋子的视觉表现(GameObject)和逻辑数据(PieceData, BoardPosition)。
/// </summary>
public class PieceComponent : MonoBehaviour
{
    // 棋子在棋盘逻辑坐标系中的位置
    public Vector2Int BoardPosition { get; set; }

    // 棋子的纯数据定义（类型、颜色）
    public Piece PieceData { get; set; }

    /// <summary>
    /// 仅在实时模式下使用的棋子动态状态。
    /// 在回合制模式下，这个值将保持为 null，从而实现模式隔离。
    /// </summary>
    public RealTimePieceState RTState { get; set; }
}