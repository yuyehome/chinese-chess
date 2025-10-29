// File: _Scripts/Network/GameNetworkManager.cs

using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using Steamworks;
using System;
using FishNet.Managing.Server;

/// <summary>
/// 游戏网络逻辑的中心枢纽。
/// 负责管理所有联网玩家的数据，并作为全局游戏状态（如阵营分配）的权威来源。
/// 这是一个网络对象，在游戏场景中应该是唯一的。
/// </summary>
public class GameNetworkManager : NetworkBehaviour
{
    public static GameNetworkManager Instance { get; private set; }
    public static event Action<GameNetworkManager> OnInstanceReady;

    // 使用SyncDictionary来同步所有玩家的数据
    // Key: 客户端的ConnectionId, Value: 玩家网络数据
    public readonly SyncDictionary<int, PlayerNetData> AllPlayers = new SyncDictionary<int, PlayerNetData>();

    [Header("网络对象 Prefabs")]
    [Tooltip("必须挂载了 NetworkObject 组件的棋子Prefab")]
    public GameObject networkPiecePrefab; // 引用我们改造过的棋子Prefab

    private void Awake()
    {
        // 使用Awake来设置单例，确保在OnStartNetwork之前Instance就有值
        // 但要注意，此时网络功能还未准备好，不能调用RPC或使用SyncVar
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[GameNetworkManager] Instance assigned in Awake.");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[GameNetworkManager] Duplicate instance found. Destroying {gameObject.name}.");
            Destroy(gameObject);
        }
    }

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        Instance = this;
        Debug.Log("[GameNetworkManager] OnStartNetwork called, Instance is set.");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[DIAG-1A] GameNetworkManager.OnStartServer CALLED. Firing OnInstanceReady...");
        OnInstanceReady?.Invoke(this);
        Debug.Log("[DIAG-1B] GameNetworkManager.OnInstanceReady FIRED.");
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[GameNetworkManager] OnStartClient: Firing OnInstanceReady.");
        OnInstanceReady?.Invoke(this);

    }

    public override void OnStopClient()
    {
        base.OnStopClient();
    }

    private void OnPlayersDictionaryChanged(SyncDictionaryOperation op, int key, PlayerNetData item, bool asServer)
    {
        // 这条日志会显示每一次SyncDict的变化
        Debug.Log($"[GNM-DIAGNOSTIC] OnPlayersDictionaryChanged triggered. asServer: {asServer}, Op: {op}, Key: {key}, Color: {item.Color}, MyClientId: {base.ClientManager.Connection.ClientId}");

        // 我们只关心客户端的逻辑，并且只在新的键值对被添加到字典时处理
        // item 参数现在代表的是被添加/修改/删除的那个 PlayerNetData
        if (asServer || op != SyncDictionaryOperation.Add)
            return;

        // 检查变化的key是否是自己的连接ID
        if (key == base.ClientManager.Connection.ClientId)
        {
            Debug.Log($"[Client] 接收到自己的玩家数据更新! 颜色: {item.Color}");
        }
    }

    /// <summary>
    /// [Server Only] 根据权威的 BoardState，在网络中生成所有棋子。
    /// </summary>
    [Server]
    public void Server_InitializeBoard(BoardState boardState)
    {
        if (!IsServer)
        {
            Debug.LogError("[GameNetworkManager] 只有服务器才能初始化棋盘状态！");
            return;
        }

        if (networkPiecePrefab == null)
        {
            Debug.LogError("[Server] GameNetworkManager上的 NetworkPiecePrefab 未被指定！无法生成棋子。");
            return;
        }

        Debug.Log("[Server] 开始在网络上生成棋子...");
        int spawnedCount = 0;

        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece piece = boardState.GetPieceAt(pos);

                if (piece.Type != PieceType.None)
                {
                    // 1. 在服务器上实例化Prefab
                    GameObject pieceInstance = Instantiate(networkPiecePrefab, BoardRenderer.Instance.transform);
                    pieceInstance.transform.localPosition = BoardRenderer.Instance.GetLocalPosition(x, y);

                    // 2. 获取组件并调用本地方法设置[SyncVar]的初始值
                    PieceComponent pc = pieceInstance.GetComponent<PieceComponent>();
                    pc.Initialize(piece, pos);

                    // 3. (核心) 在网络上生成该对象。
                    // FishNet会自动将pc上的[SyncVar]值同步给客户端。
                    base.ServerManager.Spawn(pieceInstance);

                    spawnedCount++;
                }
            }
        }

        Debug.Log($"[Server] 棋盘初始化完成，共生成了 {spawnedCount} 个网络化棋子。");
    }

    /// <summary>
    /// [Server Rpc] 客户端在进入游戏场景后，调用此方法向服务器注册自己的信息。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CmdRegisterPlayer(CSteamID steamId, string playerName, FishNet.Connection.NetworkConnection conn = null)
    {
        Debug.Log($"[DIAG-4A] CmdRegisterPlayer EXECUTED on server. Called by ConnectionId: {conn.ClientId}. IsLocalClient: {conn.IsLocalClient}.");

        int connectionId = conn.ClientId;

        bool redPlayerExists = false;
        foreach (var player in AllPlayers.Values)
        {
            if (player.Color == PlayerColor.Red)
            {
                redPlayerExists = true;
                break;
            }
        }
        PlayerColor color = redPlayerExists ? PlayerColor.Black : PlayerColor.Red;
        var playerData = new PlayerNetData(steamId, playerName, color);

        if (!AllPlayers.ContainsKey(connectionId))
        {
            AllPlayers.Add(connectionId, playerData);
            Debug.Log($"[DIAG-4B] Player registered: Id={connectionId}, Name={playerName}, Color={color}.");

            // --- 最终修复逻辑 ---
            // 直接判断连接ID是否为0，这是Host更可靠的标识
            // ServerManager.Clients[0] 是服务器/Host自己的连接
            if (connectionId == 0)
            {
                Debug.Log("[DIAG-4C-HOST] ConnectionId is 0. This is the Host. Attempting to initialize controller directly.");
                if (GameManager.Instance != null)
                {
                    Debug.Log("[DIAG-4D-HOST] GameManager.Instance is NOT null. Calling InitializeLocalPlayerController...");
                    GameManager.Instance.InitializeLocalPlayerController(playerData);
                }
                else
                {
                    Debug.LogError("[DIAG-4E-HOST-ERROR] FATAL: GameManager.Instance IS NULL here! Cannot initialize controller.");
                }
            }
            else
            {
                Debug.Log($"[DIAG-4C-CLIENT] ConnectionId is {connectionId}. This is a remote client. Sending TargetRpc...");
                Target_SetPlayerColor(conn, playerData);
            }
        }
        else
        {
            Debug.LogWarning($"[DIAG-4F] Player {connectionId} tried to register again.");
        }
    }

    /// <summary>
    /// [Target Rpc] 由服务器调用，专门用于通知一个特定的客户端它的玩家数据。
    /// </summary>
    [TargetRpc]
    private void Target_SetPlayerColor(FishNet.Connection.NetworkConnection target, PlayerNetData data)
    {
        Debug.Log($"[Client] 收到服务器指定的阵营信息! 我的颜色是: {data.Color}");
        // 直接通知 GameManager 初始化控制器
        if (GameManager.Instance != null)
        {
            GameManager.Instance.InitializeLocalPlayerController(data);
        }
        else
        {
            Debug.LogError("[Client] Target_SetPlayerColor 无法找到 GameManager.Instance！");
        }
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

    /// <summary>
    /// [Server Rpc] 客户端请求移动棋子。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CmdRequestMove(Vector2Int from, Vector2Int to, FishNet.Connection.NetworkConnection sender = null)
    {
        int connectionId = sender.ClientId;

        // --- 核心修复逻辑 ---
        // 如果ClientId是short.MaxValue (32767)，说明这是服务器/Host自己发起的调用。
        // 在我们的逻辑中，Host注册时使用的ID是0。所以这里需要做一个ID转换。
        if (connectionId == short.MaxValue)
        {
            connectionId = 0;
        }

        // 1. 安全性检查：根据修正后的ID，从已注册的玩家列表中查找数据
        if (!AllPlayers.TryGetValue(connectionId, out PlayerNetData playerData))
        {
            Debug.LogError($"[Server] 收到来自未注册玩家(ID: {sender.ClientId}, Mapped to: {connectionId})的移动请求，已忽略。");
            return;
        }

        Debug.Log($"[Server] 收到来自玩家 {playerData.PlayerName} (颜色: {playerData.Color}) 的移动请求: 从 {from} 到 {to}");

        // 2. 将请求转发给GameManager进行逻辑处理
        // 我们传递从服务器权威数据中获取的玩家颜色，而不是相信客户端
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Server_ProcessMoveRequest(playerData.Color, from, to);
        }
        else
        {
            Debug.LogError("[Server] CmdRequestMove 无法找到 GameManager.Instance！");
        }
    }


}