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

    // 在类的最上方（字段声明区域）添加这个内部类
    [System.Serializable] // 让它能在Inspector中显示
    public class PlayerPanelUI
    {
        public GameObject panelRoot; // 面板的根对象，用于控制显隐
        public TextMeshProUGUI nameText;
        public TextMeshProUGUI rankText;
        public TextMeshProUGUI coinText;
        public RawImage avatarImage; // 添加头像的引用
    }

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
    public PlayerPanelUI hostPlayerPanel;   // 用于显示房主信息的UI面板
    public PlayerPanelUI guestPlayerPanel;  // 用于显示客人信息的UI面板

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
        CSteamID ownerId = SteamMatchmaking.GetLobbyOwner(lobbyId);

        // 获取房间内的所有玩家
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
        CSteamID guestId = CSteamID.Nil; // 初始化客人ID为空

        // 遍历所有成员，找到不是房主的那个人，他就是客人
        for (int i = 0; i < memberCount; i++)
        {
            CSteamID memberId = SteamMatchmaking.GetLobbyMemberByIndex(lobbyId, i);
            if (memberId != ownerId)
            {
                guestId = memberId;
                break; // 找到一个就够了
            }
        }

        // 更新房主面板信息
        DisplayPlayerData(hostPlayerPanel, ownerId);

        // 更新客人面板信息
        // 如果 guestId 是 CSteamID.Nil (即无效ID)，DisplayPlayerData 方法会自动处理“等待玩家”的逻辑
        DisplayPlayerData(guestPlayerPanel, guestId);


        // 更新房间信息 (这部分逻辑不变)
        var lobbyData = LobbyManager.Instance.CurrentLobbyData;
        roomNameText_InLobby.text = lobbyData.GetValueOrDefault(LobbyManager.LobbyNameKey, "读取中...");
        gameModeText_InLobby.text = $"模式: {lobbyData.GetValueOrDefault(LobbyManager.GameModeKey, "N/A")}";
        roomLevelText_InLobby.text = $"等级: {lobbyData.GetValueOrDefault(LobbyManager.RoomLevelKey, "N/A")}";
    }

    /// <summary>
    /// 一个辅助方法，用于将指定玩家的数据填充到指定的UI面板上
    /// </summary>
    /// <param name="panel">要填充的UI面板数据结构</param>
    /// <param name="steamId">玩家的SteamID</param>
    private void DisplayPlayerData(PlayerPanelUI panel, CSteamID steamId)
    {
        // 如果steamId无效，则显示为等待状态
        if (!steamId.IsValid() || steamId == CSteamID.Nil)
        {
            panel.panelRoot.SetActive(true); // 确保面板是可见的
            panel.nameText.text = "等待玩家加入...";
            panel.rankText.text = ""; // 清空不必要的信息
            panel.coinText.text = "";
            panel.avatarImage.texture = null; // 清空头像
            panel.avatarImage.color = new Color(0, 0, 0, 0.5f); // 给个半透明的底色
            return;
        }

        // 如果steamId有效，则获取并显示信息
        panel.panelRoot.SetActive(true);
        panel.nameText.text = SteamFriends.GetFriendPersonaName(steamId);

        // 只有本地玩家才显示段位和金币等详细信息
        if (steamId == SteamManager.Instance.PlayerSteamId)
        {
            panel.rankText.gameObject.SetActive(true);
            panel.coinText.gameObject.SetActive(true);
            panel.rankText.text = "段位: 黄金I"; // 假数据
            panel.coinText.text = "金币: 8888"; // 假数据
        }
        else // 其他玩家不显示
        {
            panel.rankText.gameObject.SetActive(false);
            panel.coinText.gameObject.SetActive(false);
        }

        // --- 获取并显示Steam头像 ---
        StartCoroutine(FetchAndDisplayAvatar(panel.avatarImage, steamId));
    }

    #endregion

    private System.Collections.IEnumerator FetchAndDisplayAvatar(RawImage targetImage, CSteamID steamId)
    {
        // 1. 从Steam请求头像ID，这是一个快速的同步调用
        int avatarId = SteamFriends.GetLargeFriendAvatar(steamId);

        // 如果avatarId为-1，表示该用户的头像数据还没被Steam下载到本地
        if (avatarId == -1)
        {
            // 在这种情况下，我们需要等待Steam下载完成。
            // Steamworks.NET 提供了一个方便的回调来处理这个过程。
            Callback<AvatarImageLoaded_t> avatarLoadedCallback = null;
            bool isAvatarReady = false;

            avatarLoadedCallback = Callback<AvatarImageLoaded_t>.Create(result =>
            {
                // 当头像下载完成时，这个回调会被触发
                if (result.m_steamID == steamId)
                {
                    isAvatarReady = true;
                    // 注销回调，避免内存泄漏
                    if (avatarLoadedCallback != null)
                    {
                        avatarLoadedCallback.Dispose();
                        avatarLoadedCallback = null;
                    }
                }
            });

            // 等待 isAvatarReady 变为 true，最多等待5秒
            float timeout = 5f;
            while (!isAvatarReady && timeout > 0)
            {
                timeout -= Time.deltaTime;
                yield return null; // 等待下一帧
            }

            if (!isAvatarReady)
            {
                Debug.LogWarning($"[Avatar] 获取 {steamId} 的头像超时。");
                yield break; // 超时则直接退出
            }

            // 下载完成后，再次获取ID
            avatarId = SteamFriends.GetLargeFriendAvatar(steamId);
        }

        // 2. 如果avatarId有效 (不为0或-1)，则获取头像数据
        if (avatarId > 0)
        {
            uint imageWidth, imageHeight;
            if (SteamUtils.GetImageSize(avatarId, out imageWidth, out imageHeight))
            {
                byte[] imageData = new byte[imageWidth * imageHeight * 4];
                if (SteamUtils.GetImageRGBA(avatarId, imageData, imageData.Length))
                {
                    // 创建Texture2D并加载像素数据
                    Texture2D avatarTexture = new Texture2D((int)imageWidth, (int)imageHeight, TextureFormat.RGBA32, false);
                    avatarTexture.LoadRawTextureData(imageData);
                    avatarTexture.Apply();

                    // 将Texture应用到RawImage上
                    targetImage.texture = avatarTexture;
                    targetImage.color = Color.white; // 恢复不透明
                }
            }
        }
    }

    /// <summary>
    /// “退出游戏”按钮的点击事件处理函数。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("正在退出游戏...");
        Application.Quit();
    }
}