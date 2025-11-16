// 文件路径: Assets/Scripts/_Core/Networking/MirrorService.cs 

using System;
using System.Linq;
using Mirror;
using Steamworks;
using UnityEngine;

public class MirrorService : NetworkManager, INetworkService
{
    [Header("Network Prefabs")]
    [SerializeField] private GameObject networkEventsPrefab;

    public event Action OnConnected;
    public event Action OnDisconnected;

    public bool IsHost => mode == NetworkManagerMode.Host;
    public bool IsClient => mode == NetworkManagerMode.ClientOnly;
    public bool IsConnected => mode == NetworkManagerMode.Host || mode == NetworkManagerMode.ClientOnly;

    public new void StartHost() => base.StartHost();
    public new void StartClient(string address)
    {
        networkAddress = address;
        base.StartClient();
    }
    public void StartClient(CSteamID hostId)
    {
        networkAddress = hostId.ToString();
        base.StartClient();
    }

    public void Disconnect()
    {
        if (mode == NetworkManagerMode.Host) base.StopHost();
        else if (mode == NetworkManagerMode.ClientOnly) base.StopClient();
    }
    public void SendCommandToServer(NetworkCommand command)
    {
        Debug.Log($"[MirrorService] SendCommandToServer: 发送指令 {command.type} 到服务器。PieceId: {command.pieceId}");
        NetworkClient.Send(command);
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[MirrorService] OnStartServer: 服务器已启动!");
        NetworkServer.RegisterHandler<NetworkCommand>(OnServerReceiveCommand);

        if (networkEventsPrefab != null)
        {
            Debug.Log("[MirrorService] OnStartServer: 正在生成 NetworkEvents Prefab...");
            GameObject eventsObj = Instantiate(networkEventsPrefab);

            // --- 诊断日志 #1 (HOST) ---
            // 在Spawn前，检查这个实例化的对象是否有效
            var identity = eventsObj.GetComponent<NetworkIdentity>();
            if (identity == null)
            {
                Debug.LogError("[MirrorService] OnStartServer: 致命错误! networkEventsPrefab没有NetworkIdentity组件!");
                Destroy(eventsObj);
                return;
            }
            Debug.Log($"[MirrorService] OnStartServer: 准备Spawn的对象 '{eventsObj.name}'，AssetId: {identity.assetId}");

            NetworkServer.Spawn(eventsObj);
            Debug.Log("[MirrorService] OnStartServer: NetworkEvents Prefab 生成并Spawn完毕。");
        }
        else
        {
            Debug.LogError("[MirrorService] OnStartServer: NetworkEvents Prefab 未在MirrorService中设置! 这是严重错误!");
        }

        GameLoopController.Instance.InitializeAsHost();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[MirrorService] OnClientConnect: 客户端已连接到服务器!");

        // --- 诊断日志 #2 (CLIENT) ---
        // 打印出当前客户端所有已注册的可生成Prefab
        Debug.Log("---------- [CLIENT] 已注册的Spawnable Prefabs 列表开始 ----------");
        if (spawnPrefabs == null || spawnPrefabs.Count == 0)
        {
            Debug.LogWarning("[CLIENT] spawnPrefabs列表为空! 这是导致无法生成网络对象的核心原因!");
        }
        else
        {
            foreach (var prefab in spawnPrefabs)
            {
                if (prefab == null)
                {
                    Debug.LogWarning("[CLIENT] Prefab列表中有一个空条目!");
                }
                else
                {
                    Debug.Log($"[CLIENT] 已注册 Prefab: '{prefab.name}'");
                }
            }
        }
        Debug.Log("---------- [CLIENT] 已注册的Spawnable Prefabs 列表结束 ----------");

        OnConnected?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("[MirrorService] OnClientDisconnect: 客户端已从服务器断开!");
        OnDisconnected?.Invoke();
    }

    [Server]
    private void OnServerReceiveCommand(NetworkConnectionToClient conn, NetworkCommand command)
    {
        // ... (这部分代码没有问题，保持不变) ...
        Debug.Log($"[MirrorService] OnServerReceiveCommand: 服务器收到来自客户端 {conn.connectionId} 的指令: {command.type}, PieceId: {command.pieceId}");
        ICommand logicCommand = null;
        switch (command.type)
        {
            case CommandType.Move:
                logicCommand = new MoveCommand(command.pieceId, command.targetPosition, command.requestTeam);
                break;
        }

        if (logicCommand != null && GameLoopController.Instance != null)
        {
            Debug.Log($"[MirrorService] OnServerReceiveCommand: 指令已解析，正在请求GameLoopController处理...");
            GameLoopController.Instance.RequestProcessCommand(logicCommand);
        }
        else
        {
            Debug.LogError($"[MirrorService] OnServerReceiveCommand: 解析指令失败或GameLoopController为null!");
        }
    }

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        base.OnServerAddPlayer(conn);

        Debug.Log($"[MirrorService] OnServerAddPlayer: 新客户端 (ConnectionId: {conn.connectionId}) 已加入。当前连接数: {NetworkServer.connections.Count}/{maxConnections}");

        // 检查是否所有预期的玩家都已连接
        // NetworkServer.connections 包含所有客户端连接（不包括Host自己）
        // 所以当连接数等于maxConnections - 1时，所有人都到齐了
        if (NetworkServer.connections.Count >= maxConnections - 1)
        {
            Debug.Log("[MirrorService] 所有玩家已通过P2P连接！Host正在广播开始备战指令...");
            NetworkEvents.Instance.RpcStartPreBattlePhase();
        }

        // --- 游戏状态同步逻辑保持不变 ---
        if (GameLoopController.Instance == null)
        {
            Debug.LogError("[MirrorService] OnServerAddPlayer: GameLoopController.Instance 为 null! 无法获取当前状态。");
            return;
        }
        GameState currentState = GameLoopController.Instance.GetCurrentState();
        if (NetworkEvents.Instance == null)
        {
            Debug.LogError("[MirrorService] OnServerAddPlayer: NetworkEvents.Instance 为 null! 无法发送TargetRpc。");
            return;
        }
        if (currentState != null && currentState.pieces != null && currentState.pieces.Count > 0)
        {
            var pieces = currentState.pieces.Values.ToArray();
            NetworkEvents.Instance.TargetRpcSyncInitialState(conn, pieces);
        }
    }

}