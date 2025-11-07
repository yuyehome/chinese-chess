// 文件路径: Assets/Scripts/_Core/Networking/INetworkService.cs

using System;

public interface INetworkService
{
    // --- 事件 ---
    /// <summary>当成功连接到服务器或启动主机时触发</summary>
    event Action OnConnected;
    /// <summary>当连接断开时触发</summary>
    event Action OnDisconnected;
    /// <summary>当接收到来自服务器的权威游戏状态时触发</summary>
    event Action<GameState> OnGameStateUpdated;
    // event Action<LobbyState> OnLobbyStateUpdated; // 暂时注释，后续大厅系统实现

    // --- 属性 ---
    /// <summary>判断当前是否是主机(Host/Server)</summary>
    bool IsHost { get; }

    // --- 方法 ---
    /// <summary>以主机模式启动游戏</summary>
    void StartHost(); // 简化：暂时移除GameSetupData参数
    /// <summary>以客户端模式连接</summary>
    void StartClient(string address);
    /// <summary>断开连接</summary>
    void Disconnect();
    /// <summary>向服务器发送一个指令</summary>
    void SendCommand(ICommand command);
}