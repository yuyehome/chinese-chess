// 文件路径: Assets/Scripts/_Core/Networking/OfflineService.cs

using System;
using Steamworks;
using UnityEngine; // 仅用于Debug.Log

/// <summary>
/// INetworkService的单机模式实现。
/// 它模拟了网络服务的行为，但所有操作都在本地立即执行，
/// 实现了游戏逻辑与网络层的完全解耦，便于单机开发和测试。
/// </summary>
public class OfflineService : INetworkService
{
    public event Action OnConnected;
    public event Action OnDisconnected;

    private bool _isConnected = false;

    // --- 属性实现 ---

    // 在单机模式下，我们永远是"Host"
    public bool IsHost => _isConnected;
    // 在单机模式下，我们永远不是一个纯粹的"Client"
    public bool IsClient => false;
    public bool IsConnected => _isConnected;

    // --- 方法实现 ---

    public void StartHost()
    {
        if (_isConnected)
        {
            Debug.LogWarning("[OfflineService] Host already started.");
            return;
        }

        Debug.Log("[OfflineService] Starting Host in Offline Mode...");
        _isConnected = true;

        // 模拟连接成功，触发事件，让GameLoopController等系统开始初始化
        OnConnected?.Invoke();

        // 在单机模式下，连接成功后立即初始化Host的游戏逻辑
        GameLoopController.Instance.InitializeAsHost();
    }

    public void StartClient(string address)
    {
        // 单机模式下不支持作为客户端连接
        Debug.LogError("[OfflineService] Cannot start as a client in offline mode. Use StartHost() instead.");
    }

    public void StartClient(CSteamID hostId)
    {
        // 单机模式下不支持作为客户端连接
        Debug.LogError("[OfflineService] Cannot start as a client in offline mode. Use StartHost() instead.");
    }


    public void Disconnect()
    {
        if (!_isConnected) return;

        Debug.Log("[OfflineService] Disconnecting in Offline Mode...");
        _isConnected = false;

        // 模拟断开连接
        OnDisconnected?.Invoke();
    }

    /// <summary>
    /// 这是OfflineService的核心。
    /// 它接收一个网络指令，不通过网络发送，而是直接在本地将其“翻译”回
    /// 逻辑层的ICommand，并立即交给GameLoopController处理。
    /// 这完美地模拟了客户端发送指令 -> Host接收并处理 的完整流程。
    /// </summary>
    public void SendCommandToServer(NetworkCommand command)
    {
        if (!IsHost)
        {
            Debug.LogWarning("[OfflineService] Cannot send command, not connected as host.");
            return;
        }

        // --- 本地“反序列化”，与MirrorService.OnServerReceiveCommand逻辑完全一致 ---
        ICommand logicCommand = null;
        switch (command.type)
        {
            case CommandType.Move:
                logicCommand = new MoveCommand(command.pieceId, command.targetPosition, command.requestTeam);
                break;

                // case CommandType.UseSkill:
                //     logicCommand = new UseSkillCommand(...); // 未来添加
                //     break;
        }

        if (logicCommand != null && GameLoopController.Instance != null)
        {
            // 将翻译好的逻辑指令直接交给权威处理器执行
            GameLoopController.Instance.RequestProcessCommand(logicCommand);
        }
    }
}