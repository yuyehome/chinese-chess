// ÎÄ¼þÂ·¾¶: Assets/Scripts/_Core/Networking/INetworkService.cs

using System;
using Steamworks;

public interface INetworkService
{
    event Action OnConnected;
    event Action OnDisconnected;

    bool IsHost { get; }
    bool IsClient { get; }
    bool IsConnected { get; }

    void StartHostConnectionOnly();
    void StartClientConnectionOnly(CSteamID hostId);

    void StartHostAndGame();
    void StartClientAndGame(CSteamID hostId);

    void Disconnect();
    void SendCommandToServer(NetworkCommand command);
}