// File: _Scripts/Network/LobbyManager.cs
using UnityEngine;
using Steamworks;
using FishNet.Managing;
using System.Collections.Generic;
using System; // ����System�����ռ���ʹ��Action
using FishNet; // ����FishNet
using FishNet.Managing.Scened; // ���볡������

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

        // ע��������Ҫ��Steam�ص�
        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        _lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }

    #region Public UI-facing Methods

    /// <summary>
    /// UI���ã����󴴽�һ��Lobby
    /// </summary>
    public void CreateLobby(bool isPublic, string lobbyName, string gameMode, string roomLevel)
    {
        Debug.Log($"[LobbyManager] ���󴴽�Lobby... ����: {isPublic}, ����: {lobbyName}");
        ELobbyType lobbyType = isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly;

        CurrentLobbyData.Clear();
        CurrentLobbyData[GameIdKey] = GameIdValue;
        CurrentLobbyData[StatusKey] = StatusWaiting;
        CurrentLobbyData[LobbyNameKey] = lobbyName;
        CurrentLobbyData[GameModeKey] = gameMode;
        CurrentLobbyData[RoomLevelKey] = roomLevel;

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
        SteamMatchmaking.AddRequestLobbyListStringFilter(GameIdKey, GameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);
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
            Debug.LogWarning("[LobbyManager] ֻ�з������ܿ�ʼ��Ϸ��");
            return;
        }

        Debug.Log("[LobbyManager] ������ʼ��Ϸ...");

        // 1. ����Lobby״̬Ϊ����Ϸ�С�������Ϊ���ɼ���
        SteamMatchmaking.SetLobbyData(_currentLobbyId, StatusKey, StatusInGame);
        SteamMatchmaking.SetLobbyJoinable(_currentLobbyId, false);

        // 2. ͨ��FishNet�ĳ���������������Ϸ����
        // ���������֪ͨ���������ӵĿͻ���ͬ������"Game"����
        var sld = new SceneLoadData("Game");
        _networkManager.SceneManager.LoadGlobalScenes(sld);

        Debug.Log("[LobbyManager] �������пͻ��˷��ͼ��� 'Game' ������ָ�");
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

        // ��֮ǰ������������õ�LobbyԪ������
        foreach (var dataPair in CurrentLobbyData)
        {
            SteamMatchmaking.SetLobbyData(_currentLobbyId, dataPair.Key, dataPair.Value);
        }

        // ������������
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();
        Debug.Log("[LobbyManager] FishNet Hostģʽ��������");
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"[LobbyManager] �յ����ѵ���Ϸ���룬���ڼ���Lobby: {callback.m_steamIDLobby}");
        JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] �ѽ���Lobby: {_currentLobbyId}");

        // ��������ǿͻ��ˣ����Ƿ�����������������������
        if (!_networkManager.IsServer)
        {
            CSteamID hostId = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);
            // FishySteamworks Transport���Զ���Lobby�����߻�ȡ������Ϣ
            _networkManager.ClientManager.StartConnection();
            Debug.Log($"[LobbyManager] FishNet Client���������������ӵ�Host: {hostId}");
        }

        // ��������Lobby���ݲ�����UI
        CacheLobbyData();

        // ����ֱ�ӵ���MainMenuController�����Ǵ���һ��ȫ���¼�
        // �����ű�����MainMenuController�����Լ�������¼�
        OnEnteredLobby?.Invoke(_currentLobbyId);
    }

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
        Debug.Log($"[LobbyManager] �ҵ� {lobbyCount} ��ƥ���Lobby��");

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