// File: _Scripts/Network/LobbyManager.cs

using UnityEngine;
using Steamworks;
using TMPro;
using UnityEngine.UI;
using FishNet.Managing; // 引入FishNet的NetworkManager
using System.Collections.Generic; // 用于处理回调列表

/// <summary>
/// 功能模块，负责所有与Steam Lobby相关的操作：创建、查找、加入、离开。
/// 并管理Lobby相关的UI面板切换。
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Lobby配置")]
    // 定义Lobby元数据的Key，便于统一管理，避免手误写错字符串
    public const string LobbyNameKey = "name";
    public const string GameModeKey = "game_mode";
    public const string RoomLevelKey = "room_level";

    [Header("UI引用 (Lobby列表)")]
    [Tooltip("Lobby列表项的Prefab")]
    public GameObject lobbyItemPrefab;
    [Tooltip("用于放置Lobby列表项的容器对象 (Content)")]
    public Transform lobbyListContent;

    // Steam回调句柄
    protected Callback<LobbyMatchList_t> lobbyMatchList;

    // Steam回调句柄
    protected Callback<LobbyCreated_t> lobbyCreated;
    protected Callback<LobbyEnter_t> lobbyEntered;
    protected Callback<LobbyDataUpdate_t> lobbyDataUpdate;
    // ... 未来还会添加其他回调，如玩家加入/离开房间等

    private NetworkManager _networkManager;
    private CSteamID currentLobbyId;

    // 用于存储当前房间的属性，方便UI显示
    public Dictionary<string, string> CurrentLobbyData { get; private set; } = new Dictionary<string, string>();

    private List<GameObject> currentLobbyListItems = new List<GameObject>();

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
        // 确保SteamManager已经初始化
        if (!SteamManager.Instance.IsSteamInitialized)
        {
            Debug.LogError("[LobbyManager] Steam尚未初始化！Lobby功能将不可用。");
            this.enabled = false;
            return;
        }

        _networkManager = GetComponent<NetworkManager>();
        if (_networkManager == null)
        {
            Debug.LogError("[LobbyManager] 场景中找不到NetworkManager组件！");
            this.enabled = false;
            return;
        }

        // 注册Steam回调
        lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
    }

    #region Public Methods for UI

    /// <summary>
    /// UI调用此方法来请求创建一个Lobby
    /// </summary>
    /// <param name="isPublic">Lobby是否公开</param>
    /// <param name="lobbyName">房间名称</param>
    /// <param name="gameMode">游戏模式</param>
    /// <param name="roomLevel">房间等级</param>
    public void CreateLobby(bool isPublic, string lobbyName, string gameMode, string roomLevel)
    {
        Debug.Log($"[LobbyManager] 请求创建Lobby... 公开: {isPublic}, 名称: {lobbyName}");
        ELobbyType lobbyType = isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly;

        // Steam异步创建Lobby，结果将在OnLobbyCreated回调中处理
        SteamMatchmaking.CreateLobby(lobbyType, 2); // 2表示房间最大人数

        // 临时存储我们将要设置的数据，因为Lobby创建成功后才能设置
        CurrentLobbyData.Clear();
        CurrentLobbyData[LobbyNameKey] = lobbyName;
        CurrentLobbyData[GameModeKey] = gameMode;
        CurrentLobbyData[RoomLevelKey] = roomLevel;
    }

    /// <summary>
    /// UI调用此方法来请求刷新Lobby列表
    /// </summary>
    public void RefreshLobbyList()
    {
        if (SteamManager.Instance.IsSteamInitialized)
        {
            Debug.Log("[LobbyManager] 正在请求Lobby列表...");
            // 清空旧的列表显示
            ClearLobbyList();

            // 可选：添加过滤器，只显示我们自己游戏的Lobby
            // SteamMatchmaking.AddRequestLobbyListStringFilter("game_id", "YourGameUniqueId", ELobbyComparison.k_ELobbyComparisonEqual);

            SteamMatchmaking.RequestLobbyList();
        }
    }

    #endregion

    #region Steam Callbacks

    /// <summary>
    /// 当Lobby创建成功后，由Steam自动调用
    /// </summary>
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[LobbyManager] Lobby创建失败! Steam错误: {callback.m_eResult}");
            // TODO: 通知UI显示错误信息
            return;
        }

        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] Lobby创建成功! Lobby ID: {currentLobbyId}");

        // 设置Lobby的元数据 (名称、模式等)
        foreach (var dataPair in CurrentLobbyData)
        {
            SteamMatchmaking.SetLobbyData(currentLobbyId, dataPair.Key, dataPair.Value);
        }

        // 启动网络 - 房主同时是Server和Client
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();

        Debug.Log("[LobbyManager] FishNet Server 和 Client 已启动 (Host模式)。");

        // 注意：网络启动后，会自动触发OnLobbyEntered回调，我们在那里处理UI跳转
    }

    /// <summary>
    /// 当成功进入一个Lobby后 (无论是自己创建还是加入别人的)，由Steam自动调用
    /// </summary>
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] 已进入Lobby: {currentLobbyId}");

        // 重新获取一次完整的Lobby数据并存储
        CacheLobbyData();

        // 通知UI控制器跳转到房间等待界面
        MainMenuController.Instance.ShowLobbyRoomPanel();
        MainMenuController.Instance.UpdateLobbyRoomUI(); // 更新房间内UI显示
    }

    /// <summary>
    /// 当Lobby的元数据被更新时调用
    /// </summary>
    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        // 确保是我们当前所在Lobby的更新
        if ((CSteamID)callback.m_ulSteamIDLobby == currentLobbyId)
        {
            Debug.Log("[LobbyManager] 当前Lobby数据已更新。");
            CacheLobbyData();
            MainMenuController.Instance.UpdateLobbyRoomUI();
        }
    }

    /// <summary>
    /// 当Steam返回Lobby列表后，由Steam自动调用
    /// </summary>
    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        uint lobbyCount = callback.m_nLobbiesMatching;
        Debug.Log($"[LobbyManager] 找到 {lobbyCount} 个匹配的Lobby。");

        for (int i = 0; i < lobbyCount; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);

            // 实例化Lobby列表项Prefab
            GameObject lobbyItem = Instantiate(lobbyItemPrefab, lobbyListContent);

            // 获取并设置显示信息
            string lobbyName = SteamMatchmaking.GetLobbyData(lobbyId, LobbyNameKey);
            int currentPlayers = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            int maxPlayers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

            TMP_Text roomNameText = lobbyItem.transform.Find("RoomNameText").GetComponent<TMP_Text>();
            TMP_Text playerCountText = lobbyItem.transform.Find("PlayerCountText").GetComponent<TMP_Text>();
            Button joinButton = lobbyItem.transform.Find("JoinButton").GetComponent<Button>();

            roomNameText.text = string.IsNullOrEmpty(lobbyName) ? $"Lobby {lobbyId}" : lobbyName;
            playerCountText.text = $"{currentPlayers} / {maxPlayers}";

            // 为加入按钮添加点击事件
            // 重要: 使用一个Lambda表达式来捕获当前的lobbyId
            joinButton.onClick.AddListener(() => {
                OnClick_JoinLobby(lobbyId);
            });

            currentLobbyListItems.Add(lobbyItem);
        }
    }

    // --- 这个方法我们将在下一步（加入Lobby）中实现 ---
    private void OnClick_JoinLobby(CSteamID lobbyId)
    {
        Debug.Log($"准备加入Lobby: {lobbyId}");
        // 这里将是调用 SteamMatchmaking.JoinLobby(lobbyId) 的地方
    }

    #endregion

    #region Private Helpers

    /// <summary>
    /// 从Steam获取当前Lobby的所有元数据并缓存到字典中
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

    /// <summary>
    /// 清空当前显示的Lobby列表UI
    /// </summary>
    private void ClearLobbyList()
    {
        foreach (var item in currentLobbyListItems)
        {
            Destroy(item);
        }
        currentLobbyListItems.Clear();
    }

    #endregion
}