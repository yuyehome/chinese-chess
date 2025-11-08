// 文件路径: Assets/Scripts/_Core/Networking/NetworkData.cs

using Mirror;
using UnityEngine;

// 指令类型
public enum CommandType
{
    Move,
    UseSkill,
    EndTurn
}

// 统一的指令容器，用于网络传输
// 它需要实现 NetworkMessage 接口才能被Mirror发送
public struct NetworkCommand : NetworkMessage
{
    public CommandType type;

    // --- Move Fields ---
    public int pieceId;
    public Vector2Int targetPosition;
    public PlayerTeam requestTeam;

    // --- UseSkill Fields ---
    // public int casterPieceId;
    // public int skillId;
    // ...
}