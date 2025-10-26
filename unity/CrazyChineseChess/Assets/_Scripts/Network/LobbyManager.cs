// File: _Scripts/Network/LobbyManager.cs

using UnityEngine;
using Steamworks;
using FishNet.Managing; // ����FishNet��NetworkManager
using System.Collections.Generic; // ���ڴ���ص��б�

/// <summary>
/// ����ģ�飬����������Steam Lobby��صĲ��������������ҡ����롢�뿪��
/// ������Lobby��ص�UI����л���
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Lobby����")]
    // ����LobbyԪ���ݵ�Key������ͳһ������������д���ַ���
    public const string LobbyNameKey = "name";
    public const string GameModeKey = "game_mode";
    public const string RoomLevelKey = "room_level";

    // Steam�ص����
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyDataUpdate_t> lobbyDataUpdate;
    // ... δ��������������ص�������Ҽ���/�뿪�����

    private NetworkManager _networkManager;
    private CSteamID currentLobbyId;

    // ���ڴ洢��ǰ��������ԣ�����UI��ʾ
    public Dictionary<string, string> CurrentLobbyData { get; private set; } = new Dictionary<string, string>();


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        // ȷ��SteamManager�Ѿ���ʼ��
        if (!SteamManager.Instance.IsSteamInitialized)
        {
            Debug.LogError("[LobbyManager] Steam��δ��ʼ����Lobby���ܽ������á�");
            this.enabled = false;
            return;
        }

        _networkManager = GetComponent<NetworkManager>();
        if (_networkManager == null)
        {
            Debug.LogError("[LobbyManager] �������Ҳ���NetworkManager�����");
            this.enabled = false;
            return;
        }

        // ע��Steam�ص�
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
    }

    #region Public Methods for UI

    /// <summary>
    /// UI���ô˷��������󴴽�һ��Lobby
    /// </summary>
    /// <param name="isPublic">Lobby�Ƿ񹫿�</param>
    /// <param name="lobbyName">��������</param>
    /// <param name="gameMode">��Ϸģʽ</param>
    /// <param name="roomLevel">����ȼ�</param>
    public void CreateLobby(bool isPublic, string lobbyName, string gameMode, string roomLevel)
    {
        Debug.Log($"[LobbyManager] ���󴴽�Lobby... ����: {isPublic}, ����: {lobbyName}");
        ELobbyType lobbyType = isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly;

        // Steam�첽����Lobby���������OnLobbyCreated�ص��д���
        SteamMatchmaking.CreateLobby(lobbyType, 2); // 2��ʾ�����������

        // ��ʱ�洢���ǽ�Ҫ���õ����ݣ���ΪLobby�����ɹ����������
        CurrentLobbyData.Clear();
        CurrentLobbyData[LobbyNameKey] = lobbyName;
        CurrentLobbyData[GameModeKey] = gameMode;
        CurrentLobbyData[RoomLevelKey] = roomLevel;
    }

    #endregion

    #region Steam Callbacks

    /// <summary>
    /// ��Lobby�����ɹ�����Steam�Զ�����
    /// </summary>
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[LobbyManager] Lobby����ʧ��! Steam����: {callback.m_eResult}");
            // TODO: ֪ͨUI��ʾ������Ϣ
            return;
        }

        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] Lobby�����ɹ�! Lobby ID: {currentLobbyId}");

        // ����Lobby��Ԫ���� (���ơ�ģʽ��)
        foreach (var dataPair in CurrentLobbyData)
        {
            SteamMatchmaking.SetLobbyData(currentLobbyId, dataPair.Key, dataPair.Value);
        }

        // �������� - ����ͬʱ��Server��Client
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();

        Debug.Log("[LobbyManager] FishNet Server �� Client ������ (Hostģʽ)��");

        // ע�⣺���������󣬻��Զ�����OnLobbyEntered�ص������������ﴦ��UI��ת
    }

    /// <summary>
    /// ���ɹ�����һ��Lobby�� (�������Լ��������Ǽ�����˵�)����Steam�Զ�����
    /// </summary>
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] �ѽ���Lobby: {currentLobbyId}");

        // ���»�ȡһ��������Lobby���ݲ��洢
        CacheLobbyData();

        // ֪ͨUI��������ת������ȴ�����
        MainMenuController.Instance.ShowLobbyRoomPanel();
        MainMenuController.Instance.UpdateLobbyRoomUI(); // ���·�����UI��ʾ
    }

    /// <summary>
    /// ��Lobby��Ԫ���ݱ�����ʱ����
    /// </summary>
    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        // ȷ�������ǵ�ǰ����Lobby�ĸ���
        if ((CSteamID)callback.m_ulSteamIDLobby == currentLobbyId)
        {
            Debug.Log("[LobbyManager] ��ǰLobby�����Ѹ��¡�");
            CacheLobbyData();
            MainMenuController.Instance.UpdateLobbyRoomUI();
        }
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// ��Steam��ȡ��ǰLobby������Ԫ���ݲ����浽�ֵ���
    /// </summary>
    private void CacheLobbyData()
    {
        CurrentLobbyData.Clear();
        int dataCount = SteamMatchmaking.GetLobbyDataCount(currentLobbyId);
        for (int i = 0; i < dataCount; i++)
        {
            SteamMatchmaking.GetLobbyDataByIndex(currentLobbyId, i, out string key, Constants.k_nMaxLobbyKeyLength, out string value, Constants.k_nMaxLobbyKeyLength);
            CurrentLobbyData[key] = value;
        }
    }

    #endregion
}