// File: _Scripts/UI/MainMenuController.cs

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;

/// <summary>
/// 主菜单场景的UI控制器。
/// 负责处理按钮点击事件，设置选择的游戏模式，并加载游戏场景。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    public static MainMenuController Instance { get; private set; }

    [Header("网络功能UI引用")]
    [Tooltip("用于显示玩家Steam昵称的TextMeshPro文本框")]
    public TextMeshProUGUI playerNameText;

    [Tooltip("要加载的游戏场景的名称，必须与Build Settings中的场景名一致")]
    public string gameSceneName = "Game";

    [Header("UI面板")]
    public GameObject mainPanel; // 主菜单按钮所在的面板
    public GameObject createLobbyPanel;
    public GameObject lobbyRoomPanel;

    [Header("创建Lobby面板的UI元素")]
    public TMP_InputField roomNameInput;
    public TMP_InputField passwordInput; // 密码功能暂时不做，但UI先留着
    public Toggle isPublicToggle;
    public TMP_Dropdown gameModeDropdown;
    public TMP_Dropdown roomLevelDropdown;
    public Button confirmCreateLobbyButton;

    [Header("Lobby房间面板的UI元素")]
    public TextMeshProUGUI myNameText;
    // public RawImage myAvatar; // 头像功能后续再做
    public TextMeshProUGUI myRankText;
    public TextMeshProUGUI myCoinText;
    public TextMeshProUGUI opponentNameText;
    public TextMeshProUGUI roomNameText_InLobby; // 房间内的房间名显示
    public TextMeshProUGUI gameModeText_InLobby;
    public TextMeshProUGUI roomLevelText_InLobby;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    private void Start()
    {
        // 在菜单开始时，更新玩家昵称
        UpdatePlayerNameDisplay();
        // 初始化时，只显示主面板
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
            playerNameText.text = "未连接到Steam";
        }
    }
        
    // --- 假设这个方法由“对战AI”按钮调用，用于打开难度选择面板 ---
    public void OnAIGameButtonClicked(GameObject difficultyPanel)
    {
        difficultyPanel.SetActive(true);
    }

    // --- 以下是三个难度按钮的处理函数 ---
    public void StartAIGameEasy()
    {
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.Easy;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartAIGameHard()
    {
        // 实际开发时，这里会加载HardAI，现在先用Easy占位
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.Hard;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartAIGameVeryHard()
    {
        // 实际开发时，这里会加载VeryHardAI，现在先用Easy占位
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.VeryHard;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// “开始回合制游戏”按钮的点击事件处理函数。
    /// </summary>
    public void StartTurnBasedGame()
    {
        GameModeSelector.SelectedMode = GameModeType.TurnBased;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// “开始实时游戏”按钮的点击事件处理函数。
    /// </summary>
    public void StartRealTimeGame()
    {
        StartAIGameEasy();
    }

    #region Lobby UI Management

    // --- 由主菜单的"创建房间"按钮调用 ---
    public void OnClick_OpenCreateLobbyPanel()
    {
        mainPanel.SetActive(false);
        createLobbyPanel.SetActive(true);
    }

    // --- 由创建Lobby面板的"取消"按钮调用 ---
    public void OnClick_CancelCreateLobby()
    {
        createLobbyPanel.SetActive(false);
        mainPanel.SetActive(true);
    }

    // --- 由创建Lobby面板的"确认创建"按钮调用 ---
    public void OnClick_ConfirmCreateLobby()
    {
        // 从UI控件获取玩家的输入
        string roomName = roomNameInput.text;
        if (string.IsNullOrWhiteSpace(roomName))
        {
            roomName = $"{SteamManager.Instance.PlayerName}的房间"; // 提供一个默认名字
        }
        bool isPublic = isPublicToggle.isOn;
        string gameMode = gameModeDropdown.options[gameModeDropdown.value].text;
        string roomLevel = roomLevelDropdown.options[roomLevelDropdown.value].text;

        // 禁用按钮防止重复点击
        confirmCreateLobbyButton.interactable = false;

        // 调用LobbyManager的核心功能
        LobbyManager.Instance.CreateLobby(isPublic, roomName, gameMode, roomLevel);
    }

    /// <summary>
    /// 由LobbyManager在进入房间后调用
    /// </summary>
    public void ShowLobbyRoomPanel()
    {
        mainPanel.SetActive(false);
        createLobbyPanel.SetActive(false);
        lobbyRoomPanel.SetActive(true);
        confirmCreateLobbyButton.interactable = true; // 恢复按钮状态以便下次使用
    }

    /// <summary>
    /// 显示主菜单面板，隐藏其他所有Lobby相关面板。
    /// 由LobbyManager在离开房间后调用。
    /// </summary>
    public void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        createLobbyPanel.SetActive(false);
        lobbyRoomPanel.SetActive(false);
    }

    /// <summary>
    /// 更新房间内等待界面的所有信息显示
    /// </summary>
    public void UpdateLobbyRoomUI()
    {
        if (!lobbyRoomPanel.activeInHierarchy) return;

        // 更新我方信息 (金币和段位暂时用假数据)
        myNameText.text = SteamManager.Instance.PlayerName;
        myRankText.text = "段位: 黄金I";
        myCoinText.text = "金币: 8888";

        // 更新房间信息
        var lobbyData = LobbyManager.Instance.CurrentLobbyData;

        string roomName, gameMode, roomLevel;

        // 使用 TryGetValue 安全地获取数据
        if (!lobbyData.TryGetValue(LobbyManager.LobbyNameKey, out roomName))
        {
            roomName = "读取中...";
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
        gameModeText_InLobby.text = $"模式: {gameMode}";
        roomLevelText_InLobby.text = $"等级: {roomLevel}";

        // 更新对手信息 (暂时为空)
        opponentNameText.text = "等待玩家加入...";
    }

    #endregion

    /// <summary>
    /// “退出游戏”按钮的点击事件处理函数。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("正在退出游戏...");
        Application.Quit();
    }
}