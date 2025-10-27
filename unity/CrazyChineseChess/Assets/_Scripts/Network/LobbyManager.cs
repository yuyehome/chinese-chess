// File: _Scripts/Network/LobbyManager.cs
using UnityEngine;
using Steamworks;
using FishNet.Managing;
using System.Collections.Generic;
using System; // 引入System命名空间以使用Action
using FishNet; // 引入FishNet
using FishNet.Managing.Scened; // 引入场景管理

/// <summary>
/// 功能模块，负责所有与Steam Lobby相关的操作：创建、查找、加入、离开、状态管理。
/// 并管理Lobby相关的UI面板切换。
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    #region Lobby Configuration
    [Header("Lobby配置")]
    // 定义Lobby元数据的Key，便于统一管理，避免手误写错字符串
    public const string GameIdKey = "game_id";
    public const string StatusKey = "status";
    public const string LobbyNameKey = "name";
    public const string GameModeKey = "game_mode";
    public const string RoomLevelKey = "room_level";

    // 定义Lobby元数据的Value
    public const string GameIdValue = "ChineseChessHonor"; // 你的游戏唯一标识
    public const string StatusWaiting = "waiting";
    public const string StatusInGame = "ingame";
    #endregion

    #region UI References
    [Header("UI引用 (Lobby列表)")]
    [Tooltip("Lobby列表项的Prefab")]
    public GameObject lobbyItemPrefab;
    [Tooltip("用于放置Lobby列表项的容器对象 (Content)")]
    public Transform lobbyListContent;
    #endregion

    #region Private State
    private NetworkManager _networkManager;
    public CSteamID _currentLobbyId;
    private List<GameObject> _currentLobbyListItems = new List<GameObject>();
    public Dictionary<string, string> CurrentLobbyData { get; private set; } = new Dictionary<string, string>();
    #endregion

    #region Steam Callbacks
    // Steam回调句柄
    protected Callback<LobbyCreated_t> _lobbyCreated;
    protected Callback<LobbyEnter_t> _lobbyEntered;
    protected Callback<LobbyDataUpdate_t> _lobbyDataUpdate;
    protected Callback<LobbyMatchList_t> _lobbyMatchList;
    protected Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested; // 通过好友邀请加入
    protected Callback<LobbyChatUpdate_t> _lobbyChatUpdate; // 玩家加入/离开/断开连接
    #endregion

    #region C# Events
    /// <summary>
    /// 当成功进入一个Lobby时触发（无论是创建还是加入）
    /// </summary>
    public static event Action<CSteamID> OnEnteredLobby;
    /// <summary>
    /// 当Lobby内数据更新时触发
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
        DontDestroyOnLoad(gameObject); // 添加这一行，确保它在场景切换时不会被销毁
    }

    private void Start()
    {
        if (!SteamManager.Instance.IsSteamInitialized)
        {
            Debug.LogError("[LobbyManager] Steam尚未初始化！Lobby功能将不可用。");
            this.enabled = false;
            return;
        }

        _networkManager = FindObjectOfType<NetworkManager>();
        if (_networkManager == null)
        {
            Debug.LogError("[LobbyManager] 场景中找不到NetworkManager组件！");
            this.enabled = false;
            return;
        }

        // 注册所有需要的Steam回调
        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        _lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
    }

    #region Public UI-facing Methods

    /// <summary>
    /// UI调用：请求创建一个Lobby
    /// </summary>
    public void CreateLobby(bool isPublic, string lobbyName, string gameMode, string roomLevel)
    {
        Debug.Log($"[LobbyManager] 请求创建Lobby... 公开: {isPublic}, 名称: {lobbyName}");
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
    /// UI调用：请求刷新Lobby列表
    /// </summary>
    public void RefreshLobbyList()
    {
        if (!SteamManager.Instance.IsSteamInitialized) return;

        Debug.Log("[LobbyManager] 正在请求Lobby列表...");
        ClearLobbyListUI();
        SteamMatchmaking.AddRequestLobbyListStringFilter(GameIdKey, GameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }

    /// <summary>
    /// 由LobbyItem或外部调用：加入一个指定的Lobby
    /// </summary>
    public void JoinLobby(CSteamID lobbyId)
    {
        Debug.Log($"[LobbyManager] 正在尝试加入Lobby: {lobbyId}");
        SteamMatchmaking.JoinLobby(lobbyId);
        // 后续逻辑在OnLobbyEntered回调中处理
    }

    /// <summary>
    /// UI调用：离开当前Lobby
    /// </summary>
    public void LeaveLobby()
    {
        if (_currentLobbyId.IsValid())
        {
            Debug.Log($"[LobbyManager] 正在离开Lobby: {_currentLobbyId}");
            SteamMatchmaking.LeaveLobby(_currentLobbyId);
            _currentLobbyId = CSteamID.Nil;

            // 根据是Host还是Client，关闭网络连接
            if (_networkManager.IsServer) _networkManager.ServerManager.StopConnection(true);
            if (_networkManager.IsClient) _networkManager.ClientManager.StopConnection();
        }

        // TODO: 引导UI返回主菜单
        MainMenuController.Instance.ShowMainPanel();
    }


    /// <summary>
    /// UI调用：房主点击开始游戏
    /// </summary>
    public void StartGame()
    {

        if (!InstanceFinder.IsServer)
        {
            Debug.LogWarning("[LobbyManager] 只有房主才能开始游戏。");
            return;
        }

        Debug.Log("[LobbyManager] 房主开始游戏...");

        // 1. 更新Lobby状态为“游戏中”，并设为不可加入
        SteamMatchmaking.SetLobbyData(_currentLobbyId, StatusKey, StatusInGame);
        SteamMatchmaking.SetLobbyJoinable(_currentLobbyId, false);

        // 2. 通过FishNet的场景管理器加载游戏场景
        // 这个方法会通知所有已连接的客户端同步加载"Game"场景
        var sld = new SceneLoadData("Game");
        _networkManager.SceneManager.LoadGlobalScenes(sld);

        Debug.Log("[LobbyManager] 已向所有客户端发送加载 'Game' 场景的指令。");
    }

    #endregion

    #region Steam Callback Handlers

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            Debug.LogError($"[LobbyManager] Lobby创建失败! Steam错误: {callback.m_eResult}");
            // TODO: 通知UI显示错误信息
            return;
        }

        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] Lobby创建成功! Lobby ID: {_currentLobbyId}");

        // 将之前缓存的数据设置到Lobby元数据中
        foreach (var dataPair in CurrentLobbyData)
        {
            SteamMatchmaking.SetLobbyData(_currentLobbyId, dataPair.Key, dataPair.Value);
        }

        // 房主启动网络
        _networkManager.ServerManager.StartConnection();
        _networkManager.ClientManager.StartConnection();
        Debug.Log("[LobbyManager] FishNet Host模式已启动。");
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"[LobbyManager] 收到好友的游戏邀请，正在加入Lobby: {callback.m_steamIDLobby}");
        JoinLobby(callback.m_steamIDLobby);
    }

    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] 已进入Lobby: {_currentLobbyId}");

        // 如果我们是客户端（不是房主），现在启动网络连接
        if (!_networkManager.IsServer)
        {
            CSteamID hostId = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);
            // FishySteamworks Transport会自动从Lobby所有者获取连接信息
            _networkManager.ClientManager.StartConnection();
            Debug.Log($"[LobbyManager] FishNet Client已启动，正在连接到Host: {hostId}");
        }

        // 缓存最新Lobby数据并更新UI
        CacheLobbyData();

        // 不再直接调用MainMenuController，而是触发一个全局事件
        // 其他脚本（如MainMenuController）可以监听这个事件
        OnEnteredLobby?.Invoke(_currentLobbyId);
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        if ((CSteamID)callback.m_ulSteamIDLobby == _currentLobbyId)
        {
            Debug.Log("[LobbyManager] 当前Lobby数据已更新。");
            CacheLobbyData();
            // 同样，使用事件来通知UI更新
            OnLobbyDataUpdatedEvent?.Invoke();
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        // 当有玩家加入、离开、断开连接时触发
        if ((CSteamID)callback.m_ulSteamIDLobby == _currentLobbyId)
        {
            Debug.Log("[LobbyManager] 房间内玩家状态变化。");
            // TODO: 更新房间内玩家列表UI
            MainMenuController.Instance.UpdateLobbyRoomUI();
        }
    }

    private void OnLobbyMatchList(LobbyMatchList_t callback)
    {
        uint lobbyCount = callback.m_nLobbiesMatching;
        Debug.Log($"[LobbyManager] 找到 {lobbyCount} 个匹配的Lobby。");

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
                Debug.LogError("[LobbyManager] 实例化的LobbyItem Prefab上没有找到LobbyItem脚本！");
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