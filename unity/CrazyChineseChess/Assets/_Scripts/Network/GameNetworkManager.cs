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

    // �¼�������֪ͨ GameManager ���߼��ű�
    public static event Action<bool> OnNetworkStart; // ����: isServer
    public static event Action<PlayerNetData> OnLocalPlayerDataReceived;

    // ���ػ����������ݣ���Ҫ�� TargetRpc ���
    private PlayerNetData _localPlayerData;
    // �ṩһ��������ֻ�����ԣ��Ա��ⲿ����PlayerHUDManager�����԰�ȫ�ؼ�������Ƿ��ѵ���
    public PlayerNetData? LocalPlayerData => _localPlayerData.SteamId.IsValid() ? _localPlayerData : (PlayerNetData?)null;

    // ͬ��������ҵ�����
    public readonly SyncDictionary<int, PlayerNetData> AllPlayers = new SyncDictionary<int, PlayerNetData>();

    [Header("������� Prefabs")]
    [Tooltip("��������� NetworkObject ���������Prefab")]
    public GameObject networkPiecePrefab;

    [Header("��Ϸ״̬ͬ��")]
    [Tooltip("�������ֵ")]
    [SerializeField] private float maxEnergy = 4.0f;
    [Tooltip("����ÿ��ָ�����")]
    [SerializeField] private float energyRecoveryRate = 0.3f;
    [Tooltip("����ʱ�ĳ�ʼ����")]
    [SerializeField] private float startEnergy = 2.0f;

    // Ϊ���������ʹ���µı�����
    public readonly SyncVar<float> RedPlayerSyncedEnergy = new SyncVar<float>();
    public readonly SyncVar<float> BlackPlayerSyncedEnergy = new SyncVar<float>();


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
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
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // �����¼���֪ͨ���м����ߣ�����GameManager��������������
        OnNetworkStart?.Invoke(true);

        // ����������ʱ������Ϊ�Լ���Host��������ע��������ݡ�
        // ����ͬ��������û�������ӳ٣�ȷ��Host��Զ�ǵ�һ������Զ�Ǻ�ɫ��
        CSteamID hostSteamId = SteamManager.Instance.PlayerSteamId;
        string hostPlayerName = SteamManager.Instance.PlayerName;

        // Host������ID�ڷ������Ͼ���0��
        var hostPlayerData = new PlayerNetData(hostSteamId, hostPlayerName, PlayerColor.Red);
        AllPlayers.Add(0, hostPlayerData);
        Debug.Log($"[Server-Host] Host����������ڷ���������ֱ��ע��: ConnId=0, Name={hostPlayerName}, Color=Red");

        // �����ݻ������������������ﴥ���¼���
        // ���ǽ�ͨ��һ���ӳٵ����������¼���ȷ�������е������ű����㹻��ʱ����ɶ��ġ�
        _localPlayerData = hostPlayerData;
        Invoke("BroadcastLocalPlayerData", 0.1f);
    }

    private void Update()
    {
        // �������������������ָ��߼�
        if (base.IsServer)
        {
            // �ָ��췽����
            if (RedPlayerSyncedEnergy.Value < maxEnergy)
            {
                RedPlayerSyncedEnergy.Value += energyRecoveryRate * Time.deltaTime;
                RedPlayerSyncedEnergy.Value = Mathf.Min(RedPlayerSyncedEnergy.Value, maxEnergy);
            }

            // �ָ��ڷ�����
            if (BlackPlayerSyncedEnergy.Value < maxEnergy)
            {
                BlackPlayerSyncedEnergy.Value += energyRecoveryRate * Time.deltaTime;
                BlackPlayerSyncedEnergy.Value = Mathf.Min(BlackPlayerSyncedEnergy.Value, maxEnergy);
            }
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        // �����¼���֪ͨ�����߿ͻ���������
        OnNetworkStart?.Invoke(false);

        if (SteamManager.Instance != null && SteamManager.Instance.IsSteamInitialized)
        {
            CmdRegisterPlayer(SteamManager.Instance.PlayerSteamId, SteamManager.Instance.PlayerName);
        }
    }

    public override void OnStopClient()
    {
        base.OnStopClient();
        // ���������ﴦ��ͻ��˶Ͽ����ӵ��߼�
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
                    GameObject pieceInstance = Instantiate(networkPiecePrefab, BoardRenderer.Instance.transform);
                    pieceInstance.transform.localPosition = BoardRenderer.Instance.GetLocalPosition(x, y);

                    PieceComponent pc = pieceInstance.GetComponent<PieceComponent>();
                    pc.Initialize(piece, pos);

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

        // �ع�����߼����Ӽ��ͽ�׳
        int connectionId = conn.ClientId;

        // ����֮ǰ����Host��conn.ClientId������32767�����������߼�����Ҫ��������0��
        // ����������ͳһ���ID�������������Ҫ�����ĵط���
        if (connectionId == short.MaxValue)
        {
            connectionId = 0; // �����������������ID��׼��Ϊ0
        }

        // �������Ѿ�ע�ᣬ����ԣ���ֹ�ظ�����
        if (AllPlayers.ContainsKey(connectionId))
        {
            Debug.LogWarning($"[Server] ����ID {connectionId} ����ҳ����ظ�ע�ᡣ");
            return;
        }

        // ��ɫ�����߼����ڸ��򵥣�ֻҪ���Ǻ췽���ѱ�Hostռ�ã������Ǻڷ�
        PlayerColor assignedColor = PlayerColor.Black;

        var playerData = new PlayerNetData(steamId, playerName, assignedColor);
        AllPlayers.Add(connectionId, playerData);

        Debug.Log($"[Server] ���ע��ɹ�: ConnId={connectionId}, Name={playerName}, ������ɫ={assignedColor}");

        // �ؼ���������Host����Client����ͨ��TargetRpc������õ����ݷ��ظ���Ӧ�����ӡ�
        // ��ͳһ�˳�ʼ�����̣����������⴦��Host����Ҫ��
        Target_SetPlayerColor(conn, playerData);

    }

    /// <summary>
    /// [Target Rpc] �ɷ��������ã�ר������֪ͨһ���ض��Ŀͻ�������������ݡ�
    /// </summary>
    [TargetRpc]
    private void Target_SetPlayerColor(FishNet.Connection.NetworkConnection target, PlayerNetData data)
    {
        // �������ݲ������¼�
        _localPlayerData = data;
        Debug.Log($"[GameNetworkManager] TargetRpc: ����������������ã���ɫΪ {data.Color}��׼�������¼�...");
        OnLocalPlayerDataReceived?.Invoke(data);
    }

    /// <summary>
    /// [Server Rpc] �ͻ��������ƶ����ӡ�
    /// </summary>
    [ServerRpc(RequireOwnership = false)]
    public void CmdRequestMove(Vector2Int from, Vector2Int to, FishNet.Connection.NetworkConnection sender = null)
    {
        int connectionId = sender.ClientId;

        // ���ClientId��short.MaxValue (32767)��˵�����Ƿ�����/Host�Լ�����ĵ��á�
        // �����ǵ��߼��У�Hostע��ʱʹ�õ�ID��0������������Ҫ��һ��IDת����
        if (connectionId == short.MaxValue)
        {
            connectionId = 0;
        }

        // 1. ��ȫ�Լ�飺�����������ID������ע�������б��в�������
        if (!AllPlayers.TryGetValue(connectionId, out PlayerNetData playerData))
        {
            Debug.LogError($"[Server] �յ�����δע�����(ID: {sender.ClientId}, Mapped to: {connectionId})���ƶ������Ѻ��ԡ�");
            return;
        }

        Debug.Log($"[Server] �յ�������� {playerData.PlayerName} (��ɫ: {playerData.Color}) ���ƶ�����: �� {from} �� {to}");

        // 2. ������ת����GameManager�����߼�����
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