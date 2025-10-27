// File: _Scripts/UI/MainMenuController.cs

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

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
    }

    private void Start()
    {
        // �ڲ˵���ʼʱ����������ǳ�
        UpdatePlayerNameDisplay();
        // ��ʼ��ʱ��ֻ��ʾ�����
        mainPanel.SetActive(true);
        createLobbyPanel.SetActive(false);
        lobbyRoomPanel.SetActive(false);
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
        if (!lobbyRoomPanel.activeInHierarchy) return;

        // �����ҷ���Ϣ (��ҺͶ�λ��ʱ�ü�����)
        myNameText.text = SteamManager.Instance.PlayerName;
        myRankText.text = "��λ: �ƽ�I";
        myCoinText.text = "���: 8888";

        // ���·�����Ϣ
        var lobbyData = LobbyManager.Instance.CurrentLobbyData;

        string roomName, gameMode, roomLevel;

        // ʹ�� TryGetValue ��ȫ�ػ�ȡ����
        if (!lobbyData.TryGetValue(LobbyManager.LobbyNameKey, out roomName))
        {
            roomName = "��ȡ��...";
        }
        if (!lobbyData.TryGetValue(LobbyManager.GameModeKey, out gameMode))
        {
            gameMode = "N/A";
        }
        if (!lobbyData.TryGetValue(LobbyManager.RoomLevelKey, out roomLevel))
        {
            roomLevel = "N/A";
        }

        roomNameText_InLobby.text = roomName;
        gameModeText_InLobby.text = $"ģʽ: {gameMode}";
        roomLevelText_InLobby.text = $"�ȼ�: {roomLevel}";

        // ���¶�����Ϣ (��ʱΪ��)
        opponentNameText.text = "�ȴ���Ҽ���...";
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