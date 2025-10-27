// File: _Scripts/Network/LobbyManager.cs

using UnityEngine;
using Steamworks;
using FishNet.Managing;
using System.Collections.Generic;
using FishNet; // ����FishNet
using FishNet.Managing.Scened; // ���볡������
using System;
using System.Collections; // ����Э�������ռ�

/// <summary>
/// ����ģ�飬����������Steam Lobby��صĲ��������������ҡ����롢�뿪��״̬����
/// ������Lobby��ص�UI����л���
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    #region Lobby Configuration
    [Header("Lobby����")]
    public const string HostAddressKey = "HostAddress"; // FishySteamworks ��Ҫ���Key
    public const string GameIdKey = "game_id";
    public const string StatusKey = "status";
    public const string LobbyNameKey = "name";
    public const string GameModeKey = "game_mode";
    public const string RoomLevelKey = "room_level";
    public const string GameIdValue = "ChineseChessHonor";
    public const string StatusWaiting = "waiting";
    public const string StatusInGame = "ingame";
    #endregion

    #region UI References
    [Header("UI���� (Lobby�б�)")]
    public GameObject lobbyItemPrefab;
    public Transform lobbyListContent;
    #endregion

    #region Private State
    public CSteamID _currentLobbyId; // ��Ϊpublic��������Inspector�й۲�
    private List<GameObject> _currentLobbyListItems = new List<GameObject>();
    public Dictionary<string, string> CurrentLobbyData { get; private set; } = new Dictionary<string, string>();
    #endregion

    #region Steam Callbacks
    protected Callback<LobbyCreated_t> _lobbyCreated;
    protected Callback<LobbyEnter_t> _lobbyEntered;
    protected Callback<LobbyDataUpdate_t> _lobbyDataUpdate;
    protected Callback<LobbyMatchList_t> _lobbyMatchList;
    protected Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;
    protected Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    #endregion

    #region C# Events
    public static event Action<CSteamID> OnEnteredLobby;
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
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("[LobbyManager.Start] LobbyManager ��ʼ��ʼ��...");
        if (!SteamManager.Instance.IsSteamInitialized)
        {
            Debug.LogError("[LobbyManager.Start] Steam��δ��ʼ����Lobby���ܽ������á�");
            this.enabled = false;
            return;
        }

        Debug.Log("[LobbyManager.Start] ����ע��Steam�ص�...");
        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        _lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        Debug.Log("[LobbyManager.Start] Steam�ص�ע����ɡ�");
    }

    #region Public UI-facing Methods

    public void CreateLobby(bool isPublic, string lobbyName, string gameMode, string roomLevel)
    {
        Debug.Log($"[LobbyManager.CreateLobby] UI���󴴽�Lobby... ����: {isPublic}, ����: {lobbyName}");
        ELobbyType lobbyType = isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly;

        CurrentLobbyData.Clear();
        CurrentLobbyData[GameIdKey] = GameIdValue;
        CurrentLobbyData[StatusKey] = StatusWaiting;
        CurrentLobbyData[LobbyNameKey] = lobbyName;
        CurrentLobbyData[GameModeKey] = gameMode;
        CurrentLobbyData[RoomLevelKey] = roomLevel;

        Debug.Log("[LobbyManager.CreateLobby] �������� SteamMatchmaking.CreateLobby API...");
        SteamMatchmaking.CreateLobby(lobbyType, 2);
    }

    public void RefreshLobbyList()
    {
        if (!SteamManager.Instance.IsSteamInitialized) return;

        Debug.Log("[LobbyManager.RefreshLobbyList] ��������Lobby�б�...");
        ClearLobbyListUI();
        SteamMatchmaking.AddRequestLobbyListStringFilter(GameIdKey, GameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }

    public void JoinLobby(CSteamID lobbyId)
    {
        Debug.Log($"[LobbyManager.JoinLobby] ���ڳ��Լ���Lobby: {lobbyId}");
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    public void LeaveLobby()
    {
        if (_currentLobbyId.IsValid())
        {
            Debug.Log($"[LobbyManager.LeaveLobby] �����뿪Lobby: {_currentLobbyId}");
            SteamMatchmaking.LeaveLobby(_currentLobbyId);
            _currentLobbyId = CSteamID.Nil;

            var networkManager = InstanceFinder.NetworkManager;
            if (networkManager != null)
            {
                Debug.Log($"[LobbyManager.LeaveLobby] NetworkManager ״̬: IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient}");
                if (networkManager.IsServer)
                {
                    Debug.Log("[LobbyManager.LeaveLobby] ֹͣ����������...");
                    networkManager.ServerManager.StopConnection(true);
                }
                if (networkManager.IsClient)
                {
                    Debug.Log("[LobbyManager.LeaveLobby] ֹͣ�ͻ�������...");
                    networkManager.ClientManager.StopConnection();
                }
            }
            else
            {
                Debug.LogWarning("[LobbyManager.LeaveLobby] δ�ҵ� NetworkManager ʵ����");
            }
        }

        MainMenuController.Instance.ShowMainPanel();
    }

    public void StartGame()
    {
        Debug.Log("[LobbyManager.StartGame] '��ʼ��Ϸ' ��ť�������");

        // --- ���ĵ�����־ ---
        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager == null)
        {
            Debug.LogError("[LobbyManager.StartGame] ���ش���: InstanceFinder.NetworkManager ���� null��");
            return;
        }

        // ��ӡ�� NetworkManager ���������״̬
        Debug.Log($"[LobbyManager.StartGame] --- NetworkManager ״̬��� ---");
        Debug.Log($"[LobbyManager.StartGame] networkManager.IsServer: {networkManager.IsServer}");
        Debug.Log($"[LobbyManager.StartGame] networkManager.IsClient: {networkManager.IsClient}");
        Debug.Log($"[LobbyManager.StartGame] networkManager.ServerManager.Started: {networkManager.ServerManager.Started}");
        Debug.Log($"[LobbyManager.StartGame] networkManager.ClientManager.Started: {networkManager.ClientManager.Started}");
        Debug.Log($"[LobbyManager.StartGame] --- ��Ͻ��� ---");

        // ʹ�� networkManager ʵ�����жϣ������� InstanceFinder �ľ�̬���ԣ���ȷ�����Ǽ�����ͬһ������
        if (!networkManager.IsServer)
        {
            Debug.LogWarning("[LobbyManager.StartGame] �ж�ʧ��: 'networkManager.IsServer' Ϊ false�������жϡ�");
            // Ϊ���ҳ�ԭ�������ټ��һ�¾�̬������Ϊ�Ա�
            Debug.LogWarning($"[LobbyManager.StartGame] �Ա�: InstanceFinder.IsServer = {InstanceFinder.IsServer}");
            return;
        }

        Debug.Log("[LobbyManager.StartGame] ���������֤ͨ��������ִ��...");

        // 1. ����Lobby״̬
        SteamMatchmaking.SetLobbyData(_currentLobbyId, StatusKey, StatusInGame);
        SteamMatchmaking.SetLobbyJoinable(_currentLobbyId, false);
        Debug.Log("[LobbyManager.StartGame] Steam Lobby ״̬�Ѹ���Ϊ '��Ϸ��' �Ҳ��ɼ��롣");

        // 2. ���س���
        var sld = new SceneLoadData("Game");
        networkManager.SceneManager.LoadGlobalScenes(sld);
        Debug.Log("[LobbyManager.StartGame] �������пͻ��˷��ͼ��� 'Game' ������ָ�");
    }

    #endregion

    #region Steam Callback Handlers

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        Debug.Log($"[LobbyManager.OnLobbyCreated] �յ� Steam �� LobbyCreated �ص������: {callback.m_eResult}");

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            // ... (ʧ���߼�����)
            return;
        }

        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager.OnLobbyCreated] Lobby�����ɹ�! Lobby ID: {_currentLobbyId}");

        // 1. ��������SteamID��ΪHostAddressд��Lobby������FishySteamworks�Ĺؼ�
        CSteamID mySteamId = SteamUser.GetSteamID();
        SteamMatchmaking.SetLobbyData(_currentLobbyId, HostAddressKey, mySteamId.ToString());
        Debug.Log($"[LobbyManager.OnLobbyCreated] �ѽ� HostAddress ({mySteamId}) д��LobbyԪ���ݡ�");
        // 2. д������Ԫ����
        foreach (var dataPair in CurrentLobbyData)
        {
            SteamMatchmaking.SetLobbyData(_currentLobbyId, dataPair.Key, dataPair.Value);
        }
        Debug.Log("[LobbyManager.OnLobbyCreated] Lobby Ԫ����������ɡ�");

        StartCoroutine(StartHostSequence());

    }

    private IEnumerator StartHostSequence()
    {
        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager == null)
        {
            Debug.LogError("[LobbyManager.StartHostSequence] �Ҳ��� NetworkManager���޷�����Host��");
            yield break;
        }

        // 1. ������������
        Debug.Log("[LobbyManager.StartHostSequence] ���� 1: ���������� (ServerManager)...");
        networkManager.ServerManager.StartConnection();

        // �ȴ�һС��ʱ�䣬ȷ����������ȫ��������Lobby������ʱ��ͬ��
        yield return new WaitForSeconds(1.3f);

        // 2. �������ͻ���
        Debug.Log("[LobbyManager.StartHostSequence] ���� 2: �����ͻ��� (ClientManager)...");
        networkManager.ClientManager.StartConnection();

        // 3. �ٴεȴ���������״̬
        yield return new WaitForSeconds(1.3f);
        Debug.Log($"[LobbyManager.StartHostSequence] ���� 3: ����״̬���: IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient}");
    }


    private System.Collections.IEnumerator CheckServerStatusAfterDelay()
    {
        // �ȴ�0.5�룬��NetworkManager�㹻��ʱ��ȥ����첽����
        yield return new WaitForSeconds(0.5f);

        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager != null)
        {
            Debug.Log($"[LobbyManager.Coroutine] �ӳ�0.5�����״̬: IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient}");
        }
    }


    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"[LobbyManager.OnGameLobbyJoinRequested] �յ����ѵ���Ϸ���룬���ڼ���Lobby: {callback.m_steamIDLobby}");
        JoinLobby(callback.m_steamIDLobby);
    }


    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager.OnLobbyEntered] �ѽ���Lobby: {_currentLobbyId}");

        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager != null)
        {
            // ----------------- CHANGE START -----------------
            // �ؼ��߼��������ж��Լ��ǲ������Lobby�ķ���
            CSteamID mySteamId = SteamUser.GetSteamID();
            CSteamID lobbyOwnerId = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);

            if (mySteamId == lobbyOwnerId)
            {
                // �Ҿ��Ƿ�����
                // �� OnLobbyCreated ���Ѿ�������Hostģʽ�����ﲻ��Ҫ���κ��¡�
                // ����һ����־��ȷ����һ�㡣
                Debug.Log("[LobbyManager.OnLobbyEntered] ��⵽��ǰ�����Lobby����������ִ�����������");
            }
            else
            {
                // ���Ǻ����Ŀͻ��ˡ�
                Debug.Log("[LobbyManager.OnLobbyEntered] ��⵽��ǰΪ Client (�Ƿ���)��׼������ Client ģʽ...");
                networkManager.ClientManager.StartConnection();
                Debug.Log($"[LobbyManager.OnLobbyEntered] FishNet Client���������������ӵ�Host: {lobbyOwnerId}");
            }
            // ----------------- CHANGE END -----------------
        }
        else
        {
            Debug.LogError("[LobbyManager.OnLobbyEntered] ���ش���: δ���ҵ� NetworkManager ʵ����");
        }

        CacheLobbyData();
        OnEnteredLobby?.Invoke(_currentLobbyId);
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        if ((CSteamID)callback.m_ulSteamIDLobby == _currentLobbyId)
        {
            Debug.Log("[LobbyManager] ��ǰLobby�����Ѹ��¡�");
            CacheLobbyData();
            OnLobbyDataUpdatedEvent?.Invoke();
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if ((CSteamID)callback.m_ulSteamIDLobby == _currentLobbyId)
        {
            Debug.Log("[LobbyManager] ���������״̬�仯��");
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