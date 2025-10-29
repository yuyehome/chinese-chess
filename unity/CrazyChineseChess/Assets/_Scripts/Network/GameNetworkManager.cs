// File: _Scripts/Network/GameNetworkManager.cs

using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;
using System.Collections.Generic;
using Steamworks;
using System;
using FishNet.Managing.Server;

/// <summary>
/// ��Ϸ�����߼���������Ŧ��
/// �����������������ҵ����ݣ�����Ϊȫ����Ϸ״̬������Ӫ���䣩��Ȩ����Դ��
/// ����һ�������������Ϸ������Ӧ����Ψһ�ġ�
/// </summary>
public class GameNetworkManager : NetworkBehaviour
{
    public static GameNetworkManager Instance { get; private set; }
    public static event Action<GameNetworkManager> OnInstanceReady;

    /// <summary>
    /// ��������ҵ�����׼����ʱ�����������Ǳ�����ҵ��������ݡ�
    /// </summary>
    public event Action<PlayerNetData> OnLocalPlayerDataReady;

    // ʹ��SyncDictionary��ͬ��������ҵ�����
    // Key: �ͻ��˵�ConnectionId, Value: �����������
    public readonly SyncDictionary<int, PlayerNetData> AllPlayers = new SyncDictionary<int, PlayerNetData>();

    [Header("������� Prefabs")]
    [Tooltip("��������� NetworkObject ���������Prefab")]
    public GameObject networkPiecePrefab; // �������Ǹ����������Prefab

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

        // ����SyncDictionary�ı仯�¼������ǽ��յ��Լ���Ӫ��Ϣ�Ĺؼ�
        AllPlayers.OnChange += OnPlayersDictionaryChanged;
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        // ����ϰ�ߣ��Ͽ�����ʱȡ������
        if (AllPlayers != null)
        {
            AllPlayers.OnChange -= OnPlayersDictionaryChanged;
        }
    }

    private void OnPlayersDictionaryChanged(SyncDictionaryOperation op, int key, PlayerNetData oldItem, PlayerNetData newItem, bool asServer)
    {
        // ����ֻ���Ŀͻ��˵��߼�������ֻ��������������ݱ���ӻ����ʱ
        if (asServer || (op != SyncDictionaryOperation.Add && op != SyncDictionaryOperation.Set))
            return;

        // ���仯��key�Ƿ����Լ�������ID
        if (key == base.ClientManager.Connection.ClientId)
        {
            Debug.Log($"[Client] ���յ��Լ���������ݸ���! ��ɫ: {newItem.Color}");
            // �����¼���֪ͨGameManager��ʼ����ҿ�����
            OnLocalPlayerDataReady?.Invoke(newItem);
        }
    }

    /// <summary>
    /// [Server Only] ����Ȩ���� BoardState���������������������ӡ�
    /// </summary>
    [Server]
    public void Server_InitializeBoard(BoardState boardState)
    {
        if (!IsServer)
        {
            Debug.LogError("[GameNetworkManager] ֻ�з��������ܳ�ʼ������״̬��");
            return;
        }

        if (networkPiecePrefab == null)
        {
            Debug.LogError("[Server] GameNetworkManager�ϵ� NetworkPiecePrefab δ��ָ�����޷��������ӡ�");
            return;
        }

        Debug.Log("[Server] ��ʼ����������������...");
        int spawnedCount = 0;

        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                var pos = new Vector2Int(x, y);
                Piece piece = boardState.GetPieceAt(pos);

                if (piece.Type != PieceType.None)
                {
                    // 1. �ڷ�������ʵ����Prefab
                    GameObject pieceInstance = Instantiate(networkPiecePrefab, BoardRenderer.Instance.transform);
                    pieceInstance.transform.localPosition = BoardRenderer.Instance.GetLocalPosition(x, y);

                    // 2. ��ȡ��������ñ��ط�������[SyncVar]�ĳ�ʼֵ
                    PieceComponent pc = pieceInstance.GetComponent<PieceComponent>();
                    pc.Initialize(piece, pos);

                    // 3. (����) �����������ɸö���
                    // FishNet���Զ���pc�ϵ�[SyncVar]ֵͬ�����ͻ��ˡ�
                    base.ServerManager.Spawn(pieceInstance);

                    spawnedCount++;
                }
            }
        }

        Debug.Log($"[Server] ���̳�ʼ����ɣ��������� {spawnedCount} �����绯���ӡ�");
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

    /// <summary>
    /// [Server Rpc] �ͻ��������ƶ����ӡ�
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CmdRequestMove(Vector2Int from, Vector2Int to, FishNet.Connection.NetworkConnection sender = null)
    {
        // 1. ��ȫ�Լ�飺���ݷ�����ID������ע�������б��в�������
        if (!AllPlayers.TryGetValue(sender.ClientId, out PlayerNetData playerData))
        {
            Debug.LogError($"[Server] �յ�����δע�����(ID: {sender.ClientId})���ƶ������Ѻ��ԡ�");
            return;
        }

        Debug.Log($"[Server] �յ�������� {playerData.PlayerName} (��ɫ: {playerData.Color}) ���ƶ�����: �� {from} �� {to}");

        // 2. ������ת����GameManager�����߼�����
        // ���Ǵ��ݴӷ�����Ȩ�������л�ȡ�������ɫ�����������ſͻ���
        if (GameManager.Instance != null)
        {
            GameManager.Instance.Server_ProcessMoveRequest(playerData.Color, from, to);
        }
        else
        {
            Debug.LogError("[Server] CmdRequestMove �޷��ҵ� GameManager.Instance��");
        }
    }


}