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
    // 提供一个公共的只读属性，以便外部（如PlayerHUDManager）可以安全地检查数据是否已到达
    public PlayerNetData? LocalPlayerData => _localPlayerData.SteamId.IsValid() ? _localPlayerData : (PlayerNetData?)null;

    // 同步所有玩家的数据
    public readonly SyncDictionary<int, PlayerNetData> AllPlayers = new SyncDictionary<int, PlayerNetData>();

    [Header("网络对象 Prefabs")]
    [Tooltip("必须挂载了 NetworkObject 组件的棋子Prefab")]
    public GameObject networkPiecePrefab;

    [Header("游戏状态同步")]
    [Tooltip("能量最大值")]
    [SerializeField] private float maxEnergy = 4.0f;
    [Tooltip("能量每秒恢复速率")]
    [SerializeField] private float energyRecoveryRate = 0.3f;
    [Tooltip("开局时的初始能量")]
    [SerializeField] private float startEnergy = 2.0f;

    // 为避免混淆，使用新的变量名
    public readonly SyncVar<float> RedPlayerSyncedEnergy = new SyncVar<float>();
    public readonly SyncVar<float> BlackPlayerSyncedEnergy = new SyncVar<float>();


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

        // 服务器启动时，立即为自己（Host）创建并注册玩家数据。
        // 这是同步操作，没有网络延迟，确保Host永远是第一个，永远是红色。
        CSteamID hostSteamId = SteamManager.Instance.PlayerSteamId;
        string hostPlayerName = SteamManager.Instance.PlayerName;

        // Host的连接ID在服务器上就是0。
        var hostPlayerData = new PlayerNetData(hostSteamId, hostPlayerName, PlayerColor.Red);
        AllPlayers.Add(0, hostPlayerData);
        Debug.Log($"[Server-Host] Host玩家数据已在服务器本地直接注册: ConnId=0, Name={hostPlayerName}, Color=Red");

        // 将数据缓存起来，但不在这里触发事件。
        // 我们将通过一个延迟调用来触发事件，确保场景中的其他脚本有足够的时间完成订阅。
        _localPlayerData = hostPlayerData;
        Invoke("BroadcastLocalPlayerData", 0.1f);
    }

    private void Update()
    {
        // 服务器负责驱动能量恢复逻辑
        if (base.IsServer)
        {
            // 恢复红方能量
            if (RedPlayerSyncedEnergy.Value < maxEnergy)
            {
                RedPlayerSyncedEnergy.Value += energyRecoveryRate * Time.deltaTime;
                RedPlayerSyncedEnergy.Value = Mathf.Min(RedPlayerSyncedEnergy.Value, maxEnergy);
            }

            // 恢复黑方能量
            if (BlackPlayerSyncedEnergy.Value < maxEnergy)
            {
                BlackPlayerSyncedEnergy.Value += energyRecoveryRate * Time.deltaTime;
                BlackPlayerSyncedEnergy.Value = Mathf.Min(BlackPlayerSyncedEnergy.Value, maxEnergy);
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // 触发事件，通知监听者客户端已启动
        OnNetworkStart?.Invoke(false);

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

        // 我们之前发现Host的conn.ClientId可能是32767，而服务器逻辑里需要把它当成0。
        // 我们在这里统一这个ID。这才是真正需要修正的地方。
        if (connectionId == short.MaxValue)
        {
            connectionId = 0; // 将服务器自身的连接ID标准化为0
        }

        // 如果玩家已经注册，则忽略，防止重复处理
        if (AllPlayers.ContainsKey(connectionId))
        {
            Debug.LogWarning($"[Server] 连接ID {connectionId} 的玩家尝试重复注册。");
            return;
        }

        // 颜色分配逻辑现在更简单：只要不是红方（已被Host占用），就是黑方
        PlayerColor assignedColor = PlayerColor.Black;

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
        Debug.Log($"[GameNetworkManager] TargetRpc: 本地玩家数据已设置，颜色为 {data.Color}。准备触发事件...");
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