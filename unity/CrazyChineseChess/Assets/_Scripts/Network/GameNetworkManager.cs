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

    // 事件，用于通知 GameManager 等逻辑脚本
    public static event Action<bool> OnNetworkStart; // 参数: isServer
    public static event Action<PlayerNetData> OnLocalPlayerDataReceived;

    // 本地缓存的玩家数据，主要由 TargetRpc 填充
    private PlayerNetData _localPlayerData;

    // 同步所有玩家的数据
    public readonly SyncDictionary<int, PlayerNetData> AllPlayers = new SyncDictionary<int, PlayerNetData>();

    [Header("网络对象 Prefabs")]
    [Tooltip("必须挂载了 NetworkObject 组件的棋子Prefab")]
    public GameObject networkPiecePrefab;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 触发事件，通知所有监听者（比如GameManager）服务器已启动
        OnNetworkStart?.Invoke(true);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // 触发事件，通知监听者客户端已启动
        OnNetworkStart?.Invoke(false);

        // 客户端准备好后，立即向服务器发起注册
        if (SteamManager.Instance != null && SteamManager.Instance.IsSteamInitialized)
        {
            CmdRegisterPlayer(SteamManager.Instance.PlayerSteamId, SteamManager.Instance.PlayerName);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        // 可以在这里处理客户端断开连接的逻辑
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
                    GameObject pieceInstance = Instantiate(networkPiecePrefab, BoardRenderer.Instance.transform);
                    pieceInstance.transform.localPosition = BoardRenderer.Instance.GetLocalPosition(x, y);

                    PieceComponent pc = pieceInstance.GetComponent<PieceComponent>();
                    pc.Initialize(piece, pos);

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

        // 重构后的逻辑更加简洁和健壮
        int connectionId = conn.ClientId;

        // 如果玩家已经注册，则忽略，防止重复处理
        if (AllPlayers.ContainsKey(connectionId))
        {
            Debug.LogWarning($"[Server] 连接ID {connectionId} 的玩家尝试重复注册。");
            return;
        }

        // 决定玩家颜色：第一个连接的总是红方
        PlayerColor assignedColor = (AllPlayers.Count == 0) ? PlayerColor.Red : PlayerColor.Black;

        var playerData = new PlayerNetData(steamId, playerName, assignedColor);
        AllPlayers.Add(connectionId, playerData);

        Debug.Log($"[Server] 玩家注册成功: ConnId={connectionId}, Name={playerName}, 分配颜色={assignedColor}");

        // 关键：无论是Host还是Client，都通过TargetRpc将分配好的数据发回给对应的连接。
        // 这统一了初始化流程，消除了特殊处理Host的需要。
        Target_SetPlayerColor(conn, playerData);

    }

    /// <summary>
    /// [Target Rpc] 由服务器调用，专门用于通知一个特定的客户端它的玩家数据。
    /// </summary>
    [TargetRpc]
    private void Target_SetPlayerColor(FishNet.Connection.NetworkConnection target, PlayerNetData data)
    {
        // 缓存数据并触发事件
        _localPlayerData = data;
        OnLocalPlayerDataReceived?.Invoke(data);
    }

    /// <summary>
    /// [Server Rpc] 客户端请求移动棋子。
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CmdRequestMove(Vector2Int from, Vector2Int to, FishNet.Connection.NetworkConnection sender = null)
    {
        int connectionId = sender.ClientId;

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