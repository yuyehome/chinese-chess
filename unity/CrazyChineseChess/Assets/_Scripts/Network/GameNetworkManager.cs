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

    /// <summary>
    /// 当本地玩家的数据准备好时触发。参数是本地玩家的网络数据。
    /// </summary>
    public event Action<PlayerNetData> OnLocalPlayerDataReady;

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
        Debug.Log("[GameNetworkManager] OnStartServer: Firing OnInstanceReady.");
        OnInstanceReady?.Invoke(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[GameNetworkManager] OnStartClient: Firing OnInstanceReady.");
        OnInstanceReady?.Invoke(this);

        // 订阅SyncDictionary的变化事件，这是接收到自己阵营信息的关键
        AllPlayers.OnChange += OnPlayersDictionaryChanged;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        // 良好习惯：断开连接时取消订阅
        if (AllPlayers != null)
        {
            AllPlayers.OnChange -= OnPlayersDictionaryChanged;
        }
    }

    private void OnPlayersDictionaryChanged(SyncDictionaryOperation op, int key, PlayerNetData oldItem, PlayerNetData newItem, bool asServer)
    {
        // 我们只关心客户端的逻辑，并且只关心有新玩家数据被添加或更新时
        if (asServer || (op != SyncDictionaryOperation.Add && op != SyncDictionaryOperation.Set))
            return;

        // 检查变化的key是否是自己的连接ID
        if (key == base.ClientManager.Connection.ClientId)
        {
            Debug.Log($"[Client] 接收到自己的玩家数据更新! 颜色: {newItem.Color}");
            // 触发事件，通知GameManager初始化玩家控制器
            OnLocalPlayerDataReady?.Invoke(newItem);
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
        // conn 参数是FishNet自动填充的，代表调用此RPC的客户端连接
        int connectionId = conn.ClientId;

        // 修正颜色分配逻辑：
        // 查找是否已经有红方玩家了
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

        // 使用 TryAdd 或直接赋值，防止因为重复注册导致错误
        if (!AllPlayers.ContainsKey(connectionId))
        {
            AllPlayers.Add(connectionId, playerData);
            Debug.Log($"[Server] 玩家注册: Id={connectionId}, Name={playerName}, Color={color}");
        }
        else
        {
            Debug.LogWarning($"[Server] 玩家 {connectionId} 尝试重复注册。");
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
        // 1. 安全性检查：根据发送者ID，从已注册的玩家列表中查找数据
        if (!AllPlayers.TryGetValue(sender.ClientId, out PlayerNetData playerData))
        {
            Debug.LogError($"[Server] 收到来自未注册玩家(ID: {sender.ClientId})的移动请求，已忽略。");
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