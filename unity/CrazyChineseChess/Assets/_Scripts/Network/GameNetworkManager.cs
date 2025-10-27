// File: _Scripts/Network/GameNetworkManager.cs

using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using Steamworks;

/// <summary>
/// 游戏网络逻辑的中心枢纽。
/// 负责管理所有联网玩家的数据，并作为全局游戏状态（如阵营分配）的权威来源。
/// 这是一个网络对象，在游戏场景中应该是唯一的。
/// </summary>
public class GameNetworkManager : NetworkBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    // 使用SyncDictionary来同步所有玩家的数据
    // Key: 客户端的ConnectionId, Value: 玩家网络数据
    public readonly SyncDictionary<int, PlayerNetData> AllPlayers = new SyncDictionary<int, PlayerNetData>();

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        if (Instance == null)
        {
            Instance = this;
            // 如果这是一个持久化的对象，可以在这里加上 DontDestroyOnLoad(gameObject);
            Debug.Log("[GameNetworkManager] Instance registered.");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[GameNetworkManager] Duplicate instance found. Destroying {gameObject.name}.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// [Server Rpc] 客户端在进入游戏场景后，调用此方法向服务器注册自己的信息。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CmdRegisterPlayer(CSteamID steamId, string playerName, FishNet.Connection.NetworkConnection conn = null)
    {
        // conn 参数是FishNet自动填充的，代表调用此RPC的客户端连接
        int connectionId = conn.ClientId;

        // 决定玩家颜色：房主(ClientId=0 in FishySteamworks)是红方，其他人是黑方
        // 注意：FishNet中Host的ClientId可能不是0，我们需要一个更可靠的方式。
        // 一个简单可靠的方式是：第一个注册的玩家是红方。
        PlayerColor color = (AllPlayers.Count == 0) ? PlayerColor.Red : PlayerColor.Black;

        var playerData = new PlayerNetData(steamId, playerName, color);
        AllPlayers.Add(connectionId, playerData);

        Debug.Log($"[Server] 玩家注册: Id={connectionId}, Name={playerName}, Color={color}");
    }

    /// <summary>
    /// 获取本地玩家的网络数据。
    /// </summary>
    public PlayerNetData? GetLocalPlayerData()
    {
        if (!IsClient) return null; // 如果不是客户端，就没有本地玩家的概念

        int localConnectionId = base.ClientManager.Connection.ClientId;
        if (AllPlayers.TryGetValue(localConnectionId, out PlayerNetData data))
        {
            return data;
        }
        return null;
    }
}