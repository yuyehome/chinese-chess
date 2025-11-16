// 文件路径: Assets/Scripts/_Core/Networking/INetworkService.cs

using System;
using Steamworks;

public interface INetworkService
{
    // Events
    event Action OnConnected;
    event Action OnDisconnected;
    // event Action<LobbyState> OnLobbyStateUpdated; // 暂时注释，Steam阶段再启用

    // Properties
    bool IsHost { get; }
    bool IsClient { get; }
    bool IsConnected { get; }

    // Methods
    void StartHost(); // 简化参数
    void StartClient(string address);
    void StartClient(CSteamID hostId);

    void Disconnect();
    void SendCommandToServer(NetworkCommand command);
}