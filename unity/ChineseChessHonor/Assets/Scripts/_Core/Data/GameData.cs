// 文件路径: Assets/Scripts/_Core/Data/GameData.cs

using System.Collections.Generic;
using UnityEngine;

// 为了能在Inspector中显示，添加[System.Serializable]
[System.Serializable]
public struct PieceData // 使用struct更轻量，适合网络传输
{
    public int uniqueId;       // 全局唯一ID
    public PlayerTeam team;    // 归属阵营
    public PieceType type;     // 棋子类型
    public Vector2Int position;// 逻辑坐标 (x, y)
    public PieceStatus status; // 当前状态
    public int heroId;         // 附身的武将ID (-1为无)

    // TODO: 后续网络阶段实现 FishNet 的 INetworkSerializable 接口
}

[System.Serializable]
public class PlayerProfile
{
    public ulong steamId;
    public string nickname;
    public int eloRating;
    public long goldCoins;
    public List<int> unlockedHeroIds;
    public List<int> unlockedSkinIds;
}

[System.Serializable]
public class GameSetupData
{
    public PlayerProfile redPlayer;
    public PlayerProfile blackPlayer;
    public List<InitialPieceSetup> initialPieceSetups;
}

[System.Serializable]
public struct InitialPieceSetup
{
    public PieceType type;
    public Vector2Int initialPosition;
    public PlayerTeam team;
    public int heroId; // -1为无
}