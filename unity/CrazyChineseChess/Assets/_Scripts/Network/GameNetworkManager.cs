// File: _Scripts/Network/GameNetworkManager.cs

using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using Steamworks;

/// <summary>
/// ��Ϸ�����߼���������Ŧ��
/// �����������������ҵ����ݣ�����Ϊȫ����Ϸ״̬������Ӫ���䣩��Ȩ����Դ��
/// ����һ�������������Ϸ������Ӧ����Ψһ�ġ�
/// </summary>
public class GameNetworkManager : NetworkBehaviour
{
    public static GameNetworkManager Instance { get; private set; }

    // ʹ��SyncDictionary��ͬ��������ҵ�����
    // Key: �ͻ��˵�ConnectionId, Value: �����������
    public readonly SyncDictionary<int, PlayerNetData> AllPlayers = new SyncDictionary<int, PlayerNetData>();

    public override void OnStartNetwork()
    {
        base.OnStartNetwork();
        if (Instance == null)
        {
            Instance = this;
            // �������һ���־û��Ķ��󣬿������������ DontDestroyOnLoad(gameObject);
            Debug.Log("[GameNetworkManager] Instance registered.");
        }
        else if (Instance != this)
        {
            Debug.LogWarning($"[GameNetworkManager] Duplicate instance found. Destroying {gameObject.name}.");
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// [Server Rpc] �ͻ����ڽ�����Ϸ�����󣬵��ô˷����������ע���Լ�����Ϣ��
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CmdRegisterPlayer(CSteamID steamId, string playerName, FishNet.Connection.NetworkConnection conn = null)
    {
        // conn ������FishNet�Զ����ģ�������ô�RPC�Ŀͻ�������
        int connectionId = conn.ClientId;

        // ���������ɫ������(ClientId=0 in FishySteamworks)�Ǻ췽���������Ǻڷ�
        // ע�⣺FishNet��Host��ClientId���ܲ���0��������Ҫһ�����ɿ��ķ�ʽ��
        // һ���򵥿ɿ��ķ�ʽ�ǣ���һ��ע�������Ǻ췽��
        PlayerColor color = (AllPlayers.Count == 0) ? PlayerColor.Red : PlayerColor.Black;

        var playerData = new PlayerNetData(steamId, playerName, color);
        AllPlayers.Add(connectionId, playerData);

        Debug.Log($"[Server] ���ע��: Id={connectionId}, Name={playerName}, Color={color}");
    }

    /// <summary>
    /// ��ȡ������ҵ��������ݡ�
    /// </summary>
    public PlayerNetData? GetLocalPlayerData()
    {
        if (!IsClient) return null; // ������ǿͻ��ˣ���û�б�����ҵĸ���

        int localConnectionId = base.ClientManager.Connection.ClientId;
        if (AllPlayers.TryGetValue(localConnectionId, out PlayerNetData data))
        {
            return data;
        }
        return null;
    }
}