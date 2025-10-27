// File: _Scripts/UI/MainMenuController.cs

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Steamworks; // ����Steamworks
using System.Collections.Generic; // ����List
using FishNet; // ����FishNet�����ڷ��� InstanceFinder

/// <summary>
/// ���˵�������UI��������
/// ������ť����¼�������ѡ�����Ϸģʽ����������Ϸ������
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("���繦��UI����")]
    [Tooltip("������ʾ���Steam�ǳƵ�TextMeshPro�ı���")]
    public TextMeshProUGUI playerNameText;

    [Tooltip("Ҫ���ص���Ϸ���������ƣ�������Build Settings�еĳ�����һ��")]
    public string gameSceneName = "Game";

    [Header("UI���")]
    public GameObject mainPanel; // ���˵���ť���ڵ����
    public GameObject createLobbyPanel;
    public GameObject lobbyRoomPanel;

    [Header("����Lobby����UIԪ��")]
    public TMP_InputField roomNameInput;
    public TMP_InputField passwordInput; // ���빦����ʱ��������UI������
    public Toggle isPublicToggle;
    public TMP_Dropdown gameModeDropdown;
    public TMP_Dropdown roomLevelDropdown;
    public Button confirmCreateLobbyButton;

    [Header("Lobby��������UIԪ��")]
    public TextMeshProUGUI myNameText;
    // public RawImage myAvatar; // ͷ���ܺ�������
    public TextMeshProUGUI myRankText;
    public TextMeshProUGUI myCoinText;
    public TextMeshProUGUI opponentNameText;
    public TextMeshProUGUI roomNameText_InLobby; // �����ڵķ�������ʾ
    public TextMeshProUGUI gameModeText_InLobby;
    public TextMeshProUGUI roomLevelText_InLobby;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        // ��Awake��ע���LobbyManager�¼��ļ���
        LobbyManager.OnEnteredLobby += HandleEnterLobby;
        LobbyManager.OnLobbyDataUpdatedEvent += UpdateLobbyRoomUI;
    }

    private void OnDestroy()
    {
        // ���˶�������ʱ�������л�������������ȡ�������Է��ڴ�й©
        LobbyManager.OnEnteredLobby -= HandleEnterLobby;
        LobbyManager.OnLobbyDataUpdatedEvent -= UpdateLobbyRoomUI;
    }

    private void Start()
    {
        // �ڲ˵���ʼʱ����������ǳ�
        UpdatePlayerNameDisplay();

        // ��ʼ��ʱ��ֻ��ʾ�����
        ShowMainPanel(); // ʹ������֮ǰ�����ķ�����ȷ��״̬ͳһ

    }

    /// <summary>
    /// ����һ���¼�����������LobbyManager��OnEnteredLobby�¼�����ʱ������
    /// </summary>
    private void HandleEnterLobby(CSteamID lobbyId)
    {
        ShowLobbyRoomPanel();
        UpdateLobbyRoomUI();
    }

    private void UpdatePlayerNameDisplay()
    {
        if (SteamManager.Instance != null && SteamManager.Instance.IsSteamInitialized)
        {
            playerNameText.text = $"{SteamManager.Instance.PlayerName}";
        }
        else
        {
            playerNameText.text = "δ���ӵ�Steam";
        }
    }
        
    // --- ������������ɡ���սAI����ť���ã����ڴ��Ѷ�ѡ����� ---
    public void OnAIGameButtonClicked(GameObject difficultyPanel)
    {
        difficultyPanel.SetActive(true);
    }

    // --- �����������ѶȰ�ť�Ĵ����� ---
    public void StartAIGameEasy()
    {
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.Easy;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartAIGameHard()
    {
        // ʵ�ʿ���ʱ����������HardAI����������Easyռλ
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.Hard;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartAIGameVeryHard()
    {
        // ʵ�ʿ���ʱ����������VeryHardAI����������Easyռλ
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.VeryHard;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// ����ʼ�غ�����Ϸ����ť�ĵ���¼���������
    /// </summary>
    public void StartTurnBasedGame()
    {
        GameModeSelector.SelectedMode = GameModeType.TurnBased;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// ����ʼʵʱ��Ϸ����ť�ĵ���¼���������
    /// </summary>
    public void StartRealTimeGame()
    {
        StartAIGameEasy();
    }

    #region Lobby UI Management

    // --- �����˵���"��������"��ť���� ---
    public void OnClick_OpenCreateLobbyPanel()
    {
        mainPanel.SetActive(false);
        createLobbyPanel.SetActive(true);
    }

    // --- �ɴ���Lobby����"ȡ��"��ť���� ---
    public void OnClick_CancelCreateLobby()
    {
        createLobbyPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    // --- �ɴ���Lobby����"ȷ�ϴ���"��ť���� ---
    public void OnClick_ConfirmCreateLobby()
    {
        // ��UI�ؼ���ȡ��ҵ�����
        string roomName = roomNameInput.text;
        if (string.IsNullOrWhiteSpace(roomName))
        {
            roomName = $"{SteamManager.Instance.PlayerName}�ķ���"; // �ṩһ��Ĭ������
        }
        bool isPublic = isPublicToggle.isOn;
        string gameMode = gameModeDropdown.options[gameModeDropdown.value].text;
        string roomLevel = roomLevelDropdown.options[roomLevelDropdown.value].text;

        // ���ð�ť��ֹ�ظ����
        confirmCreateLobbyButton.interactable = false;

        // ����LobbyManager�ĺ��Ĺ���
        LobbyManager.Instance.CreateLobby(isPublic, roomName, gameMode, roomLevel);
    }

    /// <summary>
    /// ��LobbyManager�ڽ��뷿������
    /// </summary>
    public void ShowLobbyRoomPanel()
    {
        mainPanel.SetActive(false);
        createLobbyPanel.SetActive(false);
        lobbyRoomPanel.SetActive(true);
        confirmCreateLobbyButton.interactable = true; // �ָ���ť״̬�Ա��´�ʹ��
    }

    /// <summary>
    /// ��ʾ���˵���壬������������Lobby�����塣
    /// ��LobbyManager���뿪�������á�
    /// </summary>
    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        createLobbyPanel.SetActive(false);
        lobbyRoomPanel.SetActive(false);
    }

    /// <summary>
    /// ���·����ڵȴ������������Ϣ��ʾ
    /// </summary>
    public void UpdateLobbyRoomUI()
    {
        if (!lobbyRoomPanel.activeInHierarchy || !LobbyManager.Instance._currentLobbyId.IsValid()) return;


        CSteamID lobbyId = LobbyManager.Instance._currentLobbyId;

        // ��ȡ�����ڵ��������
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        List<CSteamID> members = new List<CSteamID>();
        for (int i = 0; i < memberCount; i++)
        {
            members.Add(SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i));
        }

        // ��ȡ����ID�����Լ���ID
        CSteamID ownerId = SteamMatchmaking.GetLobbyOwner(lobbyId);
        CSteamID myId = SteamManager.Instance.PlayerSteamId;

        // �������ǲ��Ƿ�����������˭����ߣ�˭���ұ�
        if (InstanceFinder.IsServer) // ���ɿ����жϷ�ʽ���жϵ�ǰ�ͻ����ǲ���Host
        {
            // ���Ƿ���������ʾ�����
            DisplayPlayerData(myNameText, myRankText, myCoinText, myId);

            if (members.Count > 1)
            {
                // �ҵ��Ƿ������Ǹ���ң���ʾ���ұ�
                CSteamID opponentId = CSteamID.Nil;
                foreach (var member in members)
                {
                    if (member != ownerId)
                    {
                        opponentId = member;
                        break;
                    }
                }
                DisplayPlayerData(opponentNameText, null, null, opponentId); // ����ֻ��ʾ����
            }
            else
            {
                // ֻ����һ����
                opponentNameText.text = "�ȴ���Ҽ���...";
            }
        }
        else // ���Ǻ����Ŀͻ���
        {
            // ���ǿͻ��ˣ�������ʾ�����
            DisplayPlayerData(myNameText, null, null, ownerId);

            // ���Լ���ʾ���ұ�
            DisplayPlayerData(opponentNameText, myRankText, myCoinText, myId);
        }

        // ���·�����Ϣ (�ⲿ���߼�����)
        var lobbyData = LobbyManager.Instance.CurrentLobbyData;
        roomNameText_InLobby.text = lobbyData.GetValueOrDefault(LobbyManager.LobbyNameKey, "��ȡ��...");
        gameModeText_InLobby.text = $"ģʽ: {lobbyData.GetValueOrDefault(LobbyManager.GameModeKey, "N/A")}";
        roomLevelText_InLobby.text = $"�ȼ�: {lobbyData.GetValueOrDefault(LobbyManager.RoomLevelKey, "N/A")}";

    }

    /// <summary>
    /// һ���������������ڽ�ָ����ҵ�������䵽ָ����UIԪ����
    /// </summary>
    /// <param name="nameText">��ʾ�ǳƵ��ı���</param>
    /// <param name="rankText">��ʾ��λ���ı��� (��Ϊnull)</param>
    /// <param name="coinText">��ʾ��ҵ��ı��� (��Ϊnull)</param>
    /// <param name="steamId">��ҵ�SteamID</param>
    private void DisplayPlayerData(TextMeshProUGUI nameText, TextMeshProUGUI rankText, TextMeshProUGUI coinText, CSteamID steamId)
    {
        if (steamId == CSteamID.Nil || !steamId.IsValid())
        {
            nameText.text = "�ȴ����...";
            return;
        }

        // ��ȡ����ʾ�ǳ�
        nameText.text = SteamFriends.GetFriendPersonaName(steamId);

        // ����ṩ�˶�λ�ͽ���ı������������� (��Ϊֻ�б�����Ҳ���ʾ��Щ)
        if (rankText != null)
        {

            rankText.text = "��λ: �ƽ�I";
        }
        if (coinText != null)
        {
            coinText.text = "���: 8888";
        }
    }

    #endregion

    /// <summary>
    /// ���˳���Ϸ����ť�ĵ���¼���������
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("�����˳���Ϸ...");
        Application.Quit();
    }
}