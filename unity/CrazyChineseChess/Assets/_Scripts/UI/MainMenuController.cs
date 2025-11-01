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

    // ��������Ϸ����ֶ����������������ڲ���
    [System.Serializable] // ��������Inspector����ʾ
    public class PlayerPanelUI
    {
        public GameObject panelRoot; // ���ĸ��������ڿ�������
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI coinText;
        public RawImage avatarImage; // ���ͷ�������
    }

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
    public PlayerPanelUI hostPlayerPanel;   // ������ʾ������Ϣ��UI���
    public PlayerPanelUI guestPlayerPanel;  // ������ʾ������Ϣ��UI���

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
        CSteamID ownerId = SteamMatchmaking.GetLobbyOwner(lobbyId);

        // ��ȡ�����ڵ��������
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        CSteamID guestId = CSteamID.Nil; // ��ʼ������IDΪ��

        // �������г�Ա���ҵ����Ƿ������Ǹ��ˣ������ǿ���
        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
            if (memberId != ownerId)
            {
                guestId = memberId;
                break; // �ҵ�һ���͹���
            }
        }

        // ���·��������Ϣ
        DisplayPlayerData(hostPlayerPanel, ownerId);

        // ���¿��������Ϣ
        // ��� guestId �� CSteamID.Nil (����ЧID)��DisplayPlayerData �������Զ������ȴ���ҡ����߼�
        DisplayPlayerData(guestPlayerPanel, guestId);


        // ���·�����Ϣ (�ⲿ���߼�����)
        var lobbyData = LobbyManager.Instance.CurrentLobbyData;
        roomNameText_InLobby.text = lobbyData.GetValueOrDefault(LobbyManager.LobbyNameKey, "��ȡ��...");
        gameModeText_InLobby.text = $"ģʽ: {lobbyData.GetValueOrDefault(LobbyManager.GameModeKey, "N/A")}";
        roomLevelText_InLobby.text = $"�ȼ�: {lobbyData.GetValueOrDefault(LobbyManager.RoomLevelKey, "N/A")}";
    }

    /// <summary>
    /// һ���������������ڽ�ָ����ҵ�������䵽ָ����UI�����
    /// </summary>
    /// <param name="panel">Ҫ����UI������ݽṹ</param>
    /// <param name="steamId">��ҵ�SteamID</param>
    private void DisplayPlayerData(PlayerPanelUI panel, CSteamID steamId)
    {
        // ���steamId��Ч������ʾΪ�ȴ�״̬
        if (!steamId.IsValid() || steamId == CSteamID.Nil)
        {
            panel.panelRoot.SetActive(true); // ȷ������ǿɼ���
            panel.nameText.text = "�ȴ���Ҽ���...";
            panel.rankText.text = ""; // ��ղ���Ҫ����Ϣ
            panel.coinText.text = "";
            panel.avatarImage.texture = null; // ���ͷ��
            panel.avatarImage.color = new Color(0, 0, 0, 0.5f); // ������͸���ĵ�ɫ
            return;
        }

        // ���steamId��Ч�����ȡ����ʾ��Ϣ
        panel.panelRoot.SetActive(true);
        panel.nameText.text = SteamFriends.GetFriendPersonaName(steamId);

        // ֻ�б�����Ҳ���ʾ��λ�ͽ�ҵ���ϸ��Ϣ
        if (steamId == SteamManager.Instance.PlayerSteamId)
        {
            panel.rankText.gameObject.SetActive(true);
            panel.coinText.gameObject.SetActive(true);
            panel.rankText.text = "��λ: �ƽ�I"; // ������
            panel.coinText.text = "���: 8888"; // ������
        }
        else // ������Ҳ���ʾ
        {
            panel.rankText.gameObject.SetActive(false);
            panel.coinText.gameObject.SetActive(false);
        }

        // --- ��ȡ����ʾSteamͷ�� ---
        StartCoroutine(FetchAndDisplayAvatar(panel.avatarImage, steamId));
    }

    #endregion

    private System.Collections.IEnumerator FetchAndDisplayAvatar(RawImage targetImage, CSteamID steamId)
    {
        // 1. ��Steam����ͷ��ID������һ�����ٵ�ͬ������
        int avatarId = SteamFriends.GetLargeFriendAvatar(steamId);

        // ���avatarIdΪ-1����ʾ���û���ͷ�����ݻ�û��Steam���ص�����
        if (avatarId == -1)
        {
            // ����������£�������Ҫ�ȴ�Steam������ɡ�
            // Steamworks.NET �ṩ��һ������Ļص�������������̡�
            Callback<AvatarImageLoaded_t> avatarLoadedCallback = null;
            bool isAvatarReady = false;

            avatarLoadedCallback = Callback<AvatarImageLoaded_t>.Create(result =>
            {
                // ��ͷ���������ʱ������ص��ᱻ����
                if (result.m_steamID == steamId)
                {
                    isAvatarReady = true;
                    // ע���ص��������ڴ�й©
                    if (avatarLoadedCallback != null)
                    {
                        avatarLoadedCallback.Dispose();
                        avatarLoadedCallback = null;
                    }
                }
            });

            // �ȴ� isAvatarReady ��Ϊ true�����ȴ�5��
            float timeout = 5f;
            while (!isAvatarReady && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null; // �ȴ���һ֡
            }

            if (!isAvatarReady)
            {
                Debug.LogWarning($"[Avatar] ��ȡ {steamId} ��ͷ��ʱ��");
                yield break; // ��ʱ��ֱ���˳�
            }

            // ������ɺ��ٴλ�ȡID
            avatarId = SteamFriends.GetLargeFriendAvatar(steamId);
        }

        // 2. ���avatarId��Ч (��Ϊ0��-1)�����ȡͷ������
        if (avatarId > 0)
        {
            uint imageWidth, imageHeight;
            if (SteamUtils.GetImageSize(avatarId, out imageWidth, out imageHeight))
            {
                byte[] imageData = new byte[imageWidth * imageHeight * 4];
                if (SteamUtils.GetImageRGBA(avatarId, imageData, imageData.Length))
                {
                    // ����Texture2D��������������
                    Texture2D avatarTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false);
                    avatarTexture.LoadRawTextureData(imageData);
                    avatarTexture.Apply();

                    // ��TextureӦ�õ�RawImage��
                    targetImage.texture = avatarTexture;
                    targetImage.color = Color.white; // �ָ���͸��
                }
            }
        }
    }

    /// <summary>
    /// ���˳���Ϸ����ť�ĵ���¼���������
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("�����˳���Ϸ...");
        Application.Quit();
    }
}