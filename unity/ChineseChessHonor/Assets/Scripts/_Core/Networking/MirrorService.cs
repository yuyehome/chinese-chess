// 文件路径: Assets/Scripts/_Core/Networking/MirrorService.cs (最终修正版 3)

using Mirror;
using System;
using System.Linq;
using UnityEngine;

public class MirrorService : NetworkManager, INetworkService
{
    [Header("Network Prefabs")]
    [SerializeField] private GameObject networkEventsPrefab;

    public event Action OnConnected;
    public event Action OnDisconnected;

    // --- 属性实现 ---
    public bool IsHost => mode == NetworkManagerMode.Host;
    public bool IsClient => mode == NetworkManagerMode.ClientOnly;
    public bool IsConnected => mode == NetworkManagerMode.Host || mode == NetworkManagerMode.ClientOnly;

    // --- 方法实现 ---
    // 虽然基类有这些方法，但为了显式实现接口，我们需要重新声明它们
    public new void StartHost() => base.StartHost();
    public new void StartClient(string address)
    {
        networkAddress = address;
        base.StartClient();
    }
    public void Disconnect()
    {
        if (mode == NetworkManagerMode.Host) base.StopHost();
        else if (mode == NetworkManagerMode.ClientOnly) base.StopClient();
    }
    public void SendCommandToServer(NetworkCommand command) => NetworkClient.Send(command);

    // --- Mirror 回调 ---
    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("服务器已启动!");
        NetworkServer.RegisterHandler<NetworkCommand>(OnServerReceiveCommand);

        if (networkEventsPrefab != null)
        {
            GameObject eventsObj = Instantiate(networkEventsPrefab);
            NetworkServer.Spawn(eventsObj);
        }
        else
        {
            Debug.LogError("NetworkEvents Prefab 未在MirrorService中设置!");
        }

        GameLoopController.Instance.InitializeAsHost();
    }

    public override void OnClientConnect()
    {
        base.OnClientConnect();
        Debug.Log("已连接到服务器!");
        OnConnected?.Invoke();
    }

    public override void OnClientDisconnect()
    {
        base.OnClientDisconnect();
        Debug.Log("已从服务器断开!");
        OnDisconnected?.Invoke();
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

    public override void OnServerAddPlayer(NetworkConnectionToClient conn)
    {
        Debug.Log("新客户端已连接，正在同步当前游戏状态...");
        GameState currentState = GameLoopController.Instance.GetCurrentState();
        if (currentState != null && currentState.pieces.Count > 0 && NetworkEvents.Instance != null)
        {
            NetworkEvents.Instance.TargetRpcSyncInitialState(conn, currentState.pieces.Values.ToArray());
        }
    }
}