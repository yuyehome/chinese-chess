// 文件路径: Assets/Scripts/_Core/Networking/FishNetService.cs

using System;
using UnityEngine;
using FishNet.Managing;
using FishNet.Transporting;
using FishNet.Connection;

// FishNet需要组件挂载在GameObject上
public class FishNetService : MonoBehaviour, INetworkService
{
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<GameState> OnGameStateUpdated;

    private NetworkManager _networkManager;

    public bool IsHost => _networkManager.IsServer;

    void Awake()
    {
        _networkManager = FindObjectOfType<NetworkManager>();
        if (_networkManager == null)
        {
            Debug.LogError("场景中未找到NetworkManager，请添加FishNet的NetworkManager组件。");
            return;
        }

        // 订阅FishNet的连接/断开事件
        _networkManager.ClientManager.OnClientConnectionState += OnClientStateChange;
        _networkManager.ServerManager.OnServerConnectionState += OnServerStateChange;
    }

    void OnDestroy()
    {
        if (_networkManager == null) return;
        _networkManager.ClientManager.OnClientConnectionState -= OnClientStateChange;
        _networkManager.ServerManager.OnServerConnectionState -= OnServerStateChange;
    }

    // --- INetworkService 实现 ---

    public void StartHost()
    {
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();
    }

    public void StartClient(string address)
    {
        _networkManager.ClientManager.StartConnection(address);
    }

    public void Disconnect()
    {
        if (IsHost)
            _networkManager.ServerManager.StopConnection(true);
        else
            _networkManager.ClientManager.StopConnection();
    }

    public void SendCommand(ICommand command)
    {
        // TODO: 在FishNet中实现序列化和RPC发送
        Debug.LogWarning("SendCommand to server not implemented yet.");
    }

    // --- FishNet 事件处理 ---

    private void OnServerStateChange(ServerConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("FishNetService: Host started successfully.");
            // 注意：作为Host，客户端也会触发OnClientStateChange，我们在那里统一处理OnConnected
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            Debug.Log("FishNetService: Host stopped.");
        }
    }

    private void OnClientStateChange(ClientConnectionStateArgs args)
    {
        if (args.ConnectionState == LocalConnectionState.Started)
        {
            Debug.Log("FishNetService: Client connected successfully.");
            OnConnected?.Invoke();
        }
        else if (args.ConnectionState == LocalConnectionState.Stopped)
        {
            Debug.Log("FishNetService: Client disconnected.");
            OnDisconnected?.Invoke();
        }
    }
}