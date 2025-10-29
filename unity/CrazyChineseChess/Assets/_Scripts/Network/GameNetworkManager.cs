// File: _Scripts/Network/GameNetworkManager.cs

using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using Steamworks;
using System; 

/// <summary>
/// ��Ϸ�����߼���������Ŧ��
/// �����������������ҵ����ݣ�����Ϊȫ����Ϸ״̬������Ӫ���䣩��Ȩ����Դ��
/// ����һ�������������Ϸ������Ӧ����Ψһ�ġ�
/// </summary>
public class GameNetworkManager : NetworkBehaviour
{
    public static GameNetworkManager Instance { get; private set; }
    public static event Action<GameNetworkManager> OnInstanceReady;

    // ʹ��SyncDictionary��ͬ��������ҵ�����
    // Key: �ͻ��˵�ConnectionId, Value: �����������
    public readonly SyncDictionary<int, PlayerNetData> AllPlayers = new SyncDictionary<int, PlayerNetData>();

    // ʹ��SyncList��ͬ���������������ӵ�����
    public readonly SyncList<NetworkPieceData> AllPieces = new SyncList<NetworkPieceData>();

    private void Awake()
    {
        // ʹ��Awake�����õ�����ȷ����OnStartNetwork֮ǰInstance����ֵ
        // ��Ҫע�⣬��ʱ���繦�ܻ�δ׼���ã����ܵ���RPC��ʹ��SyncVar
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[GameNetworkManager] Instance assigned in Awake.");
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
        Debug.Log("[GameNetworkManager] OnStartNetwork called, Instance is set.");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        Debug.Log("[GameNetworkManager] OnStartServer: Firing OnInstanceReady.");
        OnInstanceReady?.Invoke(this);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        Debug.Log("[GameNetworkManager] OnStartClient: Firing OnInstanceReady.");
        OnInstanceReady?.Invoke(this);
    }

    /// <summary>
    /// [Server Only] ����Ȩ���� BoardState ��ʼ������� AllPieces ͬ���б�
    /// </summary>
    [Server]
    public void Server_InitializeBoard(BoardState boardState)
    {
        if (!IsServer)
        {
            Debug.LogError("[GameNetworkManager] ֻ�з��������ܳ�ʼ������״̬��");
            return;
        }

        AllPieces.Clear(); // ��վ�����
        byte currentId = 0;

        // �����߼����̣�Ϊÿ�����Ӵ����������ݲ���ӵ�ͬ���б���
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece piece = boardState.GetPieceAt(pos);

                if (piece.Type != PieceType.None)
                {
                    var netPiece = new NetworkPieceData(currentId, piece.Type, piece.Color, pos);
                    AllPieces.Add(netPiece);
                    currentId++;
                }
            }
        }

        Debug.Log($"[Server] ����״̬�ѳ�ʼ������ {AllPieces.Count} �����ӱ���ӵ�ͬ���б�");
    }

    /// <summary>
    /// [Server Rpc] �ͻ����ڽ�����Ϸ�����󣬵��ô˷����������ע���Լ�����Ϣ��
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CmdRegisterPlayer(CSteamID steamId, string playerName, FishNet.Connection.NetworkConnection conn = null)
    {
        // conn ������FishNet�Զ����ģ�������ô�RPC�Ŀͻ�������
        int connectionId = conn.ClientId;

        // ������ɫ�����߼���
        // �����Ƿ��Ѿ��к췽�����
        bool redPlayerExists = false;
        foreach (var player in AllPlayers.Values)
        {
            if (player.Color == PlayerColor.Red)
            {
                redPlayerExists = true;
                break;
            }
        }

        PlayerColor color = redPlayerExists ? PlayerColor.Black : PlayerColor.Red;

        var playerData = new PlayerNetData(steamId, playerName, color);

        // ʹ�� TryAdd ��ֱ�Ӹ�ֵ����ֹ��Ϊ�ظ�ע�ᵼ�´���
        if (!AllPlayers.ContainsKey(connectionId))
        {
            AllPlayers.Add(connectionId, playerData);
            Debug.Log($"[Server] ���ע��: Id={connectionId}, Name={playerName}, Color={color}");
        }
        else
        {
            Debug.LogWarning($"[Server] ��� {connectionId} �����ظ�ע�ᡣ");
        }

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