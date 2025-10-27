// File: _Scripts/UI/MainMenuController.cs

using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using Steamworks; // 引入Steamworks
using System.Collections.Generic; // 引入List
using FishNet; // 引入FishNet，用于访问 InstanceFinder

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
        // 在Awake中注册对LobbyManager事件的监听
        LobbyManager.OnEnteredLobby += HandleEnterLobby;
        LobbyManager.OnLobbyDataUpdatedEvent += UpdateLobbyRoomUI;
    }

    private void OnDestroy()
    {
        // 当此对象被销毁时（例如切换场景），必须取消监听以防内存泄漏
        LobbyManager.OnEnteredLobby -= HandleEnterLobby;
        LobbyManager.OnLobbyDataUpdatedEvent -= UpdateLobbyRoomUI;
    }

    private void Start()
    {
        // 在菜单开始时，更新玩家昵称
        UpdatePlayerNameDisplay();

        // 初始化时，只显示主面板
        ShowMainPanel(); // 使用我们之前创建的方法，确保状态统一

    }

    /// <summary>
    /// 这是一个事件处理函数，当LobbyManager的OnEnteredLobby事件触发时被调用
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
        if (!lobbyRoomPanel.activeInHierarchy || !LobbyManager.Instance._currentLobbyId.IsValid()) return;


        CSteamID lobbyId = LobbyManager.Instance._currentLobbyId;

        // 获取房间内的所有玩家
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        List<CSteamID> members = new List<CSteamID>();
        for (int i = 0; i < memberCount; i++)
        {
            members.Add(SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i));
        }

        // 获取房主ID和我自己的ID
        CSteamID ownerId = SteamMatchmaking.GetLobbyOwner(lobbyId);
        CSteamID myId = SteamManager.Instance.PlayerSteamId;

        // 根据我是不是房主，来决定谁在左边，谁在右边
        if (InstanceFinder.IsServer) // 更可靠的判断方式，判断当前客户端是不是Host
        {
            // 我是房主，我显示在左边
            DisplayPlayerData(myNameText, myRankText, myCoinText, myId);

            if (members.Count > 1)
            {
                // 找到非房主的那个玩家，显示在右边
                CSteamID opponentId = CSteamID.Nil;
                foreach (var member in members)
                {
                    if (member != ownerId)
                    {
                        opponentId = member;
                        break;
                    }
                }
                DisplayPlayerData(opponentNameText, null, null, opponentId); // 对手只显示名字
            }
            else
            {
                // 只有我一个人
                opponentNameText.text = "等待玩家加入...";
            }
        }
        else // 我是后加入的客户端
        {
            // 我是客户端，房主显示在左边
            DisplayPlayerData(myNameText, null, null, ownerId);

            // 我自己显示在右边
            DisplayPlayerData(opponentNameText, myRankText, myCoinText, myId);
        }

        // 更新房间信息 (这部分逻辑不变)
        var lobbyData = LobbyManager.Instance.CurrentLobbyData;
        roomNameText_InLobby.text = lobbyData.GetValueOrDefault(LobbyManager.LobbyNameKey, "读取中...");
        gameModeText_InLobby.text = $"模式: {lobbyData.GetValueOrDefault(LobbyManager.GameModeKey, "N/A")}";
        roomLevelText_InLobby.text = $"等级: {lobbyData.GetValueOrDefault(LobbyManager.RoomLevelKey, "N/A")}";

    }

    /// <summary>
    /// 一个辅助方法，用于将指定玩家的数据填充到指定的UI元素上
    /// </summary>
    /// <param name="nameText">显示昵称的文本框</param>
    /// <param name="rankText">显示段位的文本框 (可为null)</param>
    /// <param name="coinText">显示金币的文本框 (可为null)</param>
    /// <param name="steamId">玩家的SteamID</param>
    private void DisplayPlayerData(TextMeshProUGUI nameText, TextMeshProUGUI rankText, TextMeshProUGUI coinText, CSteamID steamId)
    {
        if (steamId == CSteamID.Nil || !steamId.IsValid())
        {
            nameText.text = "等待玩家...";
            return;
        }

        // 获取并显示昵称
        nameText.text = SteamFriends.GetFriendPersonaName(steamId);

        // 如果提供了段位和金币文本框，则填充假数据 (因为只有本地玩家才显示这些)
        if (rankText != null)
        {

            rankText.text = "段位: 黄金I";
        }
        if (coinText != null)
        {
            coinText.text = "金币: 8888";
        }
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