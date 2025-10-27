// File: _Scripts/Network/PlayerNetData.cs

using FishNet.Object.Synchronizing;
using Steamworks;

/// <summary>
/// һ����ͬ���Ľṹ�壬���ڴ洢���������Ự�еĺ������ݡ�
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