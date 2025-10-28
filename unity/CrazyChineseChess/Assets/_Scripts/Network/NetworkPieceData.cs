// File: _Scripts/Network/NetworkPieceData.cs

using UnityEngine;

/// <summary>
/// 一个轻量级的结构体，用于在网络间高效地传输单个棋子的核心状态。
/// 它不包含任何Unity组件或复杂对象，因此可以被FishNet高效序列化。
/// </summary>
public struct NetworkPieceData
{
    // 棋子唯一标识 (在本次游戏中)
    public readonly byte PieceId;
    // 棋子身份
    public readonly PieceType Type;
    public readonly PlayerColor Color;
    // 棋子在棋盘上的逻辑位置
    public readonly Vector2Int Position;

    public NetworkPieceData(byte pieceId, PieceType type, PlayerColor color, Vector2Int position)
    {
        PieceId = pieceId;
        Type = type;
        Color = color;
        Position = position;
    }
}