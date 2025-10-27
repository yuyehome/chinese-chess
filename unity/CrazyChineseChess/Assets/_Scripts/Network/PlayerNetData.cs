// File: _Scripts/Network/PlayerNetData.cs

using FishNet.Object.Synchronizing;
using Steamworks;

/// <summary>
/// 一个可同步的结构体，用于存储玩家在网络会话中的核心数据。
/// </summary>
public struct PlayerNetData
{
    public CSteamID SteamId;
    public string PlayerName;
    public PlayerColor Color;

    public PlayerNetData(CSteamID steamId, string playerName, PlayerColor color)
    {
        SteamId = steamId;
        PlayerName = playerName;
        Color = color;
    }
}