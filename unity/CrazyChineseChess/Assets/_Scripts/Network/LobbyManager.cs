// File: _Scripts/Network/LobbyManager.cs
using UnityEngine;
using Steamworks;
using FishNet.Managing;
using System.Collections.Generic;
using System; // ����System�����ռ���ʹ��Action
using FishNet; // ����FishNet
using FishNet.Managing.Scened; // ���볡������
using FishNet.Object; // ��Ҫ��������� NetworkObject

/// <summary>
/// ����ģ�飬����������Steam Lobby��صĲ��������������ҡ����롢�뿪��״̬����
/// ������Lobby��ص�UI����л���
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    #region Lobby Configuration
    [Header("Lobby����")]
    // ����LobbyԪ���ݵ�Key������ͳһ������������д���ַ���
    public const string GameIdKey = "game_id";
    public const string StatusKey = "status";
    public const string LobbyNameKey = "name";
    public const string GameModeKey = "game_mode";
    public const string RoomLevelKey = "room_level";

    // ����LobbyԪ���ݵ�Value
    public const string GameIdValue = "ChineseChessHonor"; // �����ϷΨһ��ʶ
    public const string StatusWaiting = "waiting";
    public const string StatusInGame = "ingame";
    #endregion

    #region UI References
    [Header("UI���� (Lobby�б�)")]
    [Header("Network Prefabs")]
    public GameObject gameNetworkManagerPrefab; // ���㴴���� GameNetworkManager Prefab �ϵ�����
    [Tooltip("Lobby�б����Prefab")]
    public GameObject lobbyItemPrefab;
    [Tooltip("���ڷ���Lobby�б������������ (Content)")]
    public Transform lobbyListContent;
    #endregion

    #region Private State
    private NetworkManager _networkManager;
    public CSteamID _currentLobbyId;
    private List<GameObject> _currentLobbyListItems = new List<GameObject>();
    public Dictionary<string, string> CurrentLobbyData { get; private set; } = new Dictionary<string, string>();
    #endregion

    private bool _isTryingToStartGame = false; // ����һ����־λ
    private bool _isLoadingScene = false; // ���������� IsLoading

    #region Steam Callbacks
    // Steam�ص����
    protected Callback<LobbyCreated_t> _lobbyCreated;
    protected Callback<LobbyEnter_t> _lobbyEntered;
    protected Callback<LobbyDataUpdate_t> _lobbyDataUpdate;
    protected Callback<LobbyMatchList_t> _lobbyMatchList;
    protected Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested; // ͨ�������������
    protected Callback<LobbyChatUpdate_t> _lobbyChatUpdate; // ��Ҽ���/�뿪/�Ͽ�����
    #endregion

    #region C# Events
    /// <summary>
    /// ���ɹ�����һ��Lobbyʱ�����������Ǵ������Ǽ��룩
    /// </summary>
    public static event Action<CSteamID> OnEnteredLobby;
    /// <summary>
    /// ��Lobby�����ݸ���ʱ����
    /// </summary>
    public static event Action OnLobbyDataUpdatedEvent;
    #endregion

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // �����һ�У�ȷ�����ڳ����л�ʱ���ᱻ����
    }

    private void Start()
    {
        if (!SteamManager.Instance.IsSteamInitialized)
        {
            Debug.LogError("[LobbyManager] Steam��δ��ʼ����Lobby���ܽ������á�");
            this.enabled = false;
            return;
        }

        _networkManager = FindObjectOfType<NetworkManager>();
        if (_networkManager == null)
        {
            Debug.LogError("[LobbyManager] �������Ҳ���NetworkManager�����");
            this.enabled = false;
            return;
        }

        // ���ķ������¼���ֻ�е���Ϊ������ʱ����Щ�¼��Żᱻ������
        _networkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;

        // ���Ŀͻ��˵�״̬�仯�¼�
        _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

        // ע��������Ҫ��Steam�ص�
        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        _lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

        _networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
    }

    private void OnDestroy()
    {
        // ��LobbyManager������ʱ��ȡ�������Է�ֹ�ڴ�й©
        if (_networkManager != null)
        {
            _networkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;

            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;

            _networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
        }
    }

    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        // ����ֻ���ķ������ˣ�����ֻ�� "Game" ����������ɺ����
        if (!InstanceFinder.IsServer || args.LoadedScenes.Length == 0 || args.LoadedScenes[0].name != "Game")
        {
            return;
        }

        Debug.Log("[LobbyManager] 'Game' scene loaded on server. Spawning GameNetworkManager.");

        // ʵ���� GameNetworkManager Prefab
        GameObject gnmInstance = Instantiate(gameNetworkManagerPrefab);

        // ͨ���������������ʵ�����������пͻ��˶���ͬ��������
        InstanceFinder.ServerManager.Spawn(gnmInstance);
    }

    /// <summary>
    /// �����ؿͻ��˵�����״̬�����仯ʱ���˷����ᱻ���á�
    /// </summary>
    private void ClientManager_OnClientConnectionState(FishNet.Transporting.ClientConnectionStateArgs args)
    {
        Debug.Log($"[CLIENT-LOG] ���ؿͻ�������״̬�仯: {args.ConnectionState}");
        if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Stopped)
        {
            // �������ֹͣ����������Ϊ����ʧ�ܻ򱻷������߳�
            // �������������UI��ʾ�����硰��������ʧ�ܡ�
            Debug.LogError("[CLIENT-LOG] ������ֹͣ������ԭ���޷����ӵ������������رա��������⡣");
        }
    }

    /// <summary>
    /// ��һ��Զ�̿ͻ��˵�����״̬�����仯ʱ���˷����ᱻ���������á�
    /// </summary>
    private void ServerManager_OnRemoteConnectionState(FishNet.Connection.NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        // �ڷ����������Ӹ���ϸ����־
        Debug.Log($"[SERVER-LOG] Զ�̿ͻ��� {conn.ClientId} ����״̬�仯: {args.ConnectionState}");

        if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
        {
            Debug.Log($"[Server] �ͻ��� {conn.ClientId} ����ȫ���ӡ�");
            // ��������ǿ��Լ���Ƿ������˶��ѵ���
            CheckIfAllPlayersAreReadyAndStartGame();
        }
        else if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
        {
            Debug.Log($"[Server] �ͻ��� {conn.ClientId} �ѶϿ����ӡ�");
        }
    }

    #region Public UI-facing Methods

    /// <summary>
    /// UI���ã����󴴽�һ��Lobby
    /// </summary>
    public void CreateLobby(bool isPublic, string lobbyName, string gameMode, string roomLevel)
    {
        Debug.Log($"[LobbyManager] ���󴴽�Lobby... ����: {isPublic}, ����: {lobbyName}");
        ELobbyType lobbyType = isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly;

        // ȷ��ÿ�δ���ʱ����ȫ�µ�����
        CurrentLobbyData = new Dictionary<string, string>
        {
            [GameIdKey] = GameIdValue,
            [StatusKey] = StatusWaiting,
            [LobbyNameKey] = lobbyName,
            [GameModeKey] = gameMode,
            [RoomLevelKey] = roomLevel
        };

        SteamMatchmaking.CreateLobby(lobbyType, 2);
    }

    /// <summary>
    /// UI���ã�����ˢ��Lobby�б�
    /// </summary>
    public void RefreshLobbyList()
    {
        if (!SteamManager.Instance.IsSteamInitialized) return;

        Debug.Log("[LobbyManager] ��������Lobby�б�...");
        ClearLobbyListUI();

        Debug.Log($"[LobbyManager] �������������: Key='{GameIdKey}', Value='{GameIdValue}'");

        SteamMatchmaking.AddRequestLobbyListStringFilter(GameIdKey, GameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);

        // ǿ�ƽ������������������Ϊȫ��Χ���ų�����λ������
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        Debug.Log("[LobbyManager] ����� Worldwide �����������");

        SteamMatchmaking.RequestLobbyList();
    }

    /// <summary>
    /// ��LobbyItem���ⲿ���ã�����һ��ָ����Lobby
    /// </summary>
    public void JoinLobby(CSteamID lobbyId)
    {
        Debug.Log($"[LobbyManager] ���ڳ��Լ���Lobby: {lobbyId}");
        SteamMatchmaking.JoinLobby(lobbyId);
        // �����߼���OnLobbyEntered�ص��д���
    }

    /// <summary>
    /// UI���ã��뿪��ǰLobby
    /// </summary>
    public void LeaveLobby()
    {
        if (_currentLobbyId.IsValid())
        {
            _isLoadingScene = false; // ����״̬
            _isTryingToStartGame = false; // ͬ�����ÿ�ʼ��ͼ
            Debug.Log($"[LobbyManager] �����뿪Lobby: {_currentLobbyId}");
            SteamMatchmaking.LeaveLobby(_currentLobbyId);
            _currentLobbyId = CSteamID.Nil;

            // ������Host����Client���ر���������
            if (_networkManager.IsServer) _networkManager.ServerManager.StopConnection(true);
            if (_networkManager.IsClient) _networkManager.ClientManager.StopConnection();
        }

        // TODO: ����UI�������˵�
        MainMenuController.Instance.ShowMainPanel();
    }

    /// <summary>
    /// UI���ã����������ʼ��Ϸ
    /// </summary>
    public void StartGame()
    {

        if (!InstanceFinder.IsServer)
        {
            //Fishy Steamworks ��Ҫ��ѡ Peer To Peer
            Debug.LogWarning("[LobbyManager] ֻ�з������ܿ�ʼ��Ϸ��");
            return;
        }

        Debug.Log("[LobbyManager] ������ʼ��Ϸ...");

        _isTryingToStartGame = true; // 1. ���ÿ�ʼ��Ϸ����ͼ��־

        // 2. ���ü�鷽����������Ѿ����ˣ��������̿�ʼ�������û�룬��ʲôҲ�������ȴ��ͻ��������¼���������
        CheckIfAllPlayersAreReadyAndStartGame();

    }


    /// <summary>
    /// ����Ƿ����㿪ʼ��Ϸ����������������������Ϸ������
    /// </summary>
    private void CheckIfAllPlayersAreReadyAndStartGame()
    {
        // ����1: �����Ƿ���������ִ�д��߼�
        // ����2: ���������Ѿ�����˿�ʼ��ť (��־λΪtrue)
        // ����3: ��Ϸ�����Ѿ���ʼ (�����ظ�����)
        if (!InstanceFinder.IsServer || !_isTryingToStartGame || _isLoadingScene) // ʹ�������Լ��ı�־λ
        {
            return;
        }

        // ����4: ��������Ƿ��㹻��Lobby����2���ˣ����ҷ�����Ҳȷ����1��Զ������(2-1=1)
        int steamLobbyMemberCount = SteamMatchmaking.GetNumLobbyMembers(_currentLobbyId);
        int connectedFishNetClients = _networkManager.ServerManager.Clients.Count; // �������Host�Լ��������� (Զ�̿ͻ����� + 1)

        Debug.Log($"[StartCheck] ��鿪ʼ����: Steam����={steamLobbyMemberCount}, FishNet������={connectedFishNetClients}");

        // ���ǵ���Ϸ��2�˶�ս
        if (steamLobbyMemberCount == 2 && connectedFishNetClients == 2)
        {
            Debug.Log("[StartCheck] �������㣡��������Ѿ��������ڼ�����Ϸ����...");

            _isLoadingScene = true; // �ڼ���ǰ���������ñ�־λ

            // --- �ⲿ����ԭStartGame�ĺ����߼� ---
            // 1. ����Lobby״̬Ϊ����Ϸ�С�������Ϊ���ɼ���
            SteamMatchmaking.SetLobbyData(_currentLobbyId, StatusKey, StatusInGame);
            SteamMatchmaking.SetLobbyJoinable(_currentLobbyId, false);

            // 2. ͨ��FishNet�ĳ���������������Ϸ����
            var sld = new SceneLoadData("Game");
            sld.ReplaceScenes = ReplaceOption.All;
            _networkManager.SceneManager.LoadGlobalScenes(sld);

            Debug.Log("[LobbyManager] �������пͻ��˷��ͼ��� 'Game' ������ָ�");

            // 3. ���ñ�־λ����ֹ�ظ�ִ��
            _isTryingToStartGame = false;
        }
        else
        {
            Debug.Log($"[StartCheck] ����δ���㣬�ȴ������������...");
        }
    }

    #endregion

    #region Steam Callback Handlers

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[LobbyManager] Lobby����ʧ��! Steam����: {callback.m_eResult}");
            // TODO: ֪ͨUI��ʾ������Ϣ
            return;
        }

        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] Lobby�����ɹ�! Lobby ID: {_currentLobbyId}");

        Debug.Log($"[LobbyManager] ����ΪLobby {_currentLobbyId} ���� {CurrentLobbyData.Count} ��Ԫ����...");
        foreach (var dataPair in CurrentLobbyData)
        {
            Debug.Log($"[LobbyManager] -> SetData: '{dataPair.Key}' = '{dataPair.Value}'");
            SteamMatchmaking.SetLobbyData(_currentLobbyId, dataPair.Key, dataPair.Value);
        }

        // ����Lobby�ɹ���Steam���Զ��ô�����"����"���Lobby��
        // ��ᴥ�� OnLobbyEntered �ص������ǽ������������߼�ͳһ�ŵ����
        Debug.Log("[LobbyManager] Lobby�Ѵ������ȴ�OnLobbyEntered�ص�����������...");
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"[LobbyManager] �յ����ѵ���Ϸ���룬���ڼ���Lobby: {callback.m_steamIDLobby}");
        JoinLobby(callback.m_steamIDLobby);
    }

    #region Steam Callback Handlers
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] �ѽ���Lobby: {_currentLobbyId}");

        // �жϵ�ǰ����Lobby�����ǲ���Lobby�Ĵ�����(Owner)
        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);
        CSteamID mySteamId = SteamManager.Instance.PlayerSteamId;

        if (lobbyOwner == mySteamId)
        {
            // ���Ƿ���
            Debug.Log("[LobbyManager] ���ȷ�ϣ����Ƿ�������������Hostģʽ...");
            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();
            Debug.Log("[LobbyManager] FishNet Hostģʽ��������");
        }
        else
        {
            // ���ǿͻ���
            Debug.Log($"[LobbyManager] [CLIENT-LOG] ���ȷ�ϣ����ǿͻ��ˡ�Ŀ��Host SteamID: {lobbyOwner}");

            if (_networkManager.ClientManager.Started)
            {
                Debug.LogWarning("[LobbyManager] [CLIENT-LOG] �ͻ��������������У������ظ�������");
                // ���������Ҫ�����Ƿ�Ҫ�Ͽ����������Ŀ��Host���˵Ļ���
                // ��ʱ�ȼ򵥴�����������ӵ������������Lobby Owner��������Ҫ����
                return;
            }

            // ��ʽ����Ҫ���ӵ�Steam ID��ַ
            var fishy = _networkManager.TransportManager.GetTransport<FishySteamworks.FishySteamworks>();
            if (fishy != null)
            {
                Debug.Log($"[LobbyManager] [CLIENT-LOG] ��������FishySteamworks��Ŀ���ַΪ: {lobbyOwner}");
                fishy.SetClientAddress(lobbyOwner.ToString());
            }
            else
            {
                Debug.LogError("[LobbyManager] [CLIENT-LOG] δ�ҵ� FishySteamworks Transport������޷�����Ŀ���ַ��");
            }

            Debug.Log("[LobbyManager] [CLIENT-LOG] ���ڵ��� ClientManager.StartConnection()...");
            // ����StartConnection��ʹ�����Ǹո����õĵ�ַ
            bool success = _networkManager.ClientManager.StartConnection();

            Debug.Log($"[LobbyManager] [CLIENT-LOG] ClientManager.StartConnection() ���÷���: {success}");
            if (!success)
            {
                Debug.LogError("[LobbyManager] [CLIENT-LOG] ClientManager.StartConnection() ����ʧ�ܣ�");
            }
        }

        // ��������Lobby���ݲ�����UI
        CacheLobbyData();
        OnEnteredLobby?.Invoke(_currentLobbyId);

    }
    #endregion

    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        if ((CSteamID)callback.m_ulSteamIDLobby == _currentLobbyId)
        {
            Debug.Log("[LobbyManager] ��ǰLobby�����Ѹ��¡�");
            CacheLobbyData();
            // ͬ����ʹ���¼���֪ͨUI����
            OnLobbyDataUpdatedEvent?.Invoke();
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        // ������Ҽ��롢�뿪���Ͽ�����ʱ����
        if ((CSteamID)callback.m_ulSteamIDLobby == _currentLobbyId)
        {
            Debug.Log("[LobbyManager] ���������״̬�仯��");
            // TODO: ���·���������б�UI
            MainMenuController.Instance.UpdateLobbyRoomUI();
        }
    }

    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        uint lobbyCount = callback.m_nLobbiesMatching;
        Debug.Log($"[LobbyManager] OnLobbyMatchList �ص����������ҵ� {lobbyCount} ��ƥ���Lobby��");

        for (int i = 0; i < lobbyCount; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            GameObject lobbyItemGO = Instantiate(lobbyItemPrefab, lobbyListContent);
            LobbyItem lobbyItem = lobbyItemGO.GetComponent<LobbyItem>();

            if (lobbyItem != null)
            {
                lobbyItem.Setup(lobbyId);
                _currentLobbyListItems.Add(lobbyItemGO);
            }
            else
            {
                Debug.LogError("[LobbyManager] ʵ������LobbyItem Prefab��û���ҵ�LobbyItem�ű���");
                Destroy(lobbyItemGO);
            }
        }
    }

    #endregion

    #region Private Helper Methods

    private void CacheLobbyData()
    {
        CurrentLobbyData.Clear();
        int dataCount = SteamMatchmaking.GetLobbyDataCount(_currentLobbyId);
        for (int i = 0; i < dataCount; i++)
        {
            SteamMatchmaking.GetLobbyDataByIndex(_currentLobbyId, i, out string key, Constants.k_nMaxLobbyKeyLength, out string value, Constants.k_nMaxLobbyKeyLength);
            CurrentLobbyData[key] = value;
        }
    }

    private void ClearLobbyListUI()
    {
        foreach (var item in _currentLobbyListItems)
        {
            Destroy(item);
        }
        _currentLobbyListItems.Clear();
    }

    #endregion
}