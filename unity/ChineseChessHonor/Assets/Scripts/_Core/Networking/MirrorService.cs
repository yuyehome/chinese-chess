// 文件路径: Assets/Scripts/_Core/Networking/MirrorService.cs 

using System;
using System.Collections.Generic; 
using System.Linq;
using Mirror;
using Steamworks;
using UnityEngine;

public class MirrorService : NetworkManager, INetworkService
{
    [Header("Network Prefabs")]
    [SerializeField] private GameObject networkEventsPrefab;

    // 新增: 用于追踪所有在房间中的玩家网络对象
    private readonly List<NetworkPlayerRoom> _roomPlayers = new List<NetworkPlayerRoom>();

    public event Action OnConnected;
    public event Action OnDisconnected;

    public bool IsHost => mode == NetworkManagerMode.Host;
    public bool IsClient => mode == NetworkManagerMode.ClientOnly;
    public bool IsConnected => mode == NetworkManagerMode.Host || mode == NetworkManagerMode.ClientOnly;

    #region INetworkService Implementation

    public void StartHostConnectionOnly()
    {
        Debug.Log("[MirrorService] Starting Host Connection Only...");
        base.StartHost();
    }

    public void StartClientConnectionOnly(CSteamID hostId)
    {
        Debug.Log($"[MirrorService] Starting Client Connection Only to {hostId}...");
        networkAddress = hostId.ToString();
        base.StartClient();
    }

    public void StartHostAndGame()
    {
        Debug.Log("[MirrorService] Starting Host and Game...");
        base.StartHost();
        GameLoopController.Instance.InitializeAsHost();
    }

    public void StartClientAndGame(CSteamID hostId)
    {
        Debug.Log($"[MirrorService] Starting Client and Game to {hostId}...");
        networkAddress = hostId.ToString();
        base.StartClient();
    }

    public void Disconnect()
    {
        if (mode == NetworkManagerMode.Host) base.StopHost();
        else if (mode == NetworkManagerMode.ClientOnly) base.StopClient();
        _roomPlayers.Clear(); // 清理玩家列表
    }

    public void SendCommandToServer(NetworkCommand command)
    {
        Debug.Log($"[MirrorService] SendCommandToServer: 发送指令 {command.type} 到服务器。PieceId: {command.pieceId}");
        NetworkClient.Send(command);
    }

    #endregion

    #region Mirror Overrides

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[MirrorService] OnStartServer: 服务器已启动!");
        NetworkServer.RegisterHandler<NetworkCommand>(OnServerReceiveCommand);

        if (networkEventsPrefab != null)
        {
            GameObject eventsObj = Instantiate(networkEventsPrefab);
            NetworkServer.Spawn(eventsObj);
        }
        else
        {
            Debug.LogError("[MirrorService] OnStartServer: NetworkEvents Prefab 未在MirrorService中设置!");
        }
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("[MirrorService] OnClientConnect: 客户端已连接到服务器!");
        OnConnected?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("[MirrorService] OnClientDisconnect: 客户端已从服务器断开!");
        _roomPlayers.Clear();
        OnDisconnected?.Invoke();
    }

    // 关键修改点
    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        // 1. 调用base.OnServerAddPlayer()。
        //    这会使用我们在Inspector中设置的 playerPrefab (即 NetworkPlayerRoomPrefab)
        //    为这个连接创建一个玩家对象，并将其与连接关联。
        base.OnServerAddPlayer(conn);

        // 2. 此时，玩家对象已经被创建。我们可以通过 conn.identity 获取它。
        NetworkPlayerRoom player = conn.identity.GetComponent<NetworkPlayerRoom>();
        if (player != null)
        {
            // 3. 将玩家添加到我们的追踪列表
            _roomPlayers.Add(player);

            // 4. 从连接的验证数据中获取SteamID并同步
            if (conn.authenticationData is CSteamID steamId)
            {
                player.SteamId = steamId;
            }

            Debug.Log($"[MirrorService] NetworkPlayerRoom为连接 {conn.connectionId} 创建并添加。当前玩家数: {_roomPlayers.Count}/{maxConnections}");

            // 5. 检查是否所有人都已加入
            CheckIfAllPlayersAreReady();
        }
        else
        {
            Debug.LogError($"[MirrorService] 严重错误: 为连接 {conn.connectionId} 创建的玩家对象上没有找到 NetworkPlayerRoom 组件!");
        }
    }

    // 当有玩家断开连接时，将其从列表中移除
    public override void OnServerDisconnect(NetworkConnectionToClient conn)
    {
        if (conn.identity != null)
        {
            var player = conn.identity.GetComponent<NetworkPlayerRoom>();
            _roomPlayers.Remove(player);
            Debug.Log($"[MirrorService] 玩家已断开，从列表中移除。剩余玩家数: {_roomPlayers.Count}");
        }
        base.OnServerDisconnect(conn);
    }

    [Server]
    private void CheckIfAllPlayersAreReady()
    {
        if (_roomPlayers.Count >= maxConnections)
        {
            Debug.Log("[MirrorService] 所有玩家已到齐！Host正在广播开始备战指令...");
            NetworkEvents.Instance.RpcStartPreBattlePhase();
        }
    }

    [Server]
    private void OnServerReceiveCommand(NetworkConnectionToClient conn, NetworkCommand command)
    {
        ICommand logicCommand = null;
        switch (command.type)
        {
            case CommandType.Move:
                logicCommand = new MoveCommand(command.pieceId, command.targetPosition, command.requestTeam);
                break;
        }

        if (logicCommand != null && GameLoopController.Instance != null)
        {
            GameLoopController.Instance.RequestProcessCommand(logicCommand);
        }
    }

    #endregion
}