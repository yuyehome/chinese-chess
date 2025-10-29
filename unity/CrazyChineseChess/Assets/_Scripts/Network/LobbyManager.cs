// File: _Scripts/Network/LobbyManager.cs
using UnityEngine;
using Steamworks;
using FishNet.Managing;
using System.Collections.Generic;
using System; // 引入System命名空间以使用Action
using FishNet; // 引入FishNet
using FishNet.Managing.Scened; // 引入场景管理
using FishNet.Object; // 需要这个来访问 NetworkObject

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
    [Header("Network Prefabs")]
    public GameObject gameNetworkManagerPrefab; // 把你创建的 GameNetworkManager Prefab 拖到这里
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

    private bool _isTryingToStartGame = false; // 新增一个标志位
    private bool _isLoadingScene = false; // 用这个来替代 IsLoading

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

        // 订阅服务器事件。只有当作为服务器时，这些事件才会被触发。
        _networkManager.ServerManager.OnRemoteConnectionState += ServerManager_OnRemoteConnectionState;

        // 订阅客户端的状态变化事件
        _networkManager.ClientManager.OnClientConnectionState += ClientManager_OnClientConnectionState;

        // 注册所有需要的Steam回调
        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        _lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);

        _networkManager.SceneManager.OnLoadEnd += OnSceneLoadEnd;
    }

    private void OnDestroy()
    {
        // 当LobbyManager被销毁时，取消订阅以防止内存泄漏
        if (_networkManager != null)
        {
            _networkManager.ServerManager.OnRemoteConnectionState -= ServerManager_OnRemoteConnectionState;

            _networkManager.ClientManager.OnClientConnectionState -= ClientManager_OnClientConnectionState;

            _networkManager.SceneManager.OnLoadEnd -= OnSceneLoadEnd;
        }
    }

    private void OnSceneLoadEnd(SceneLoadEndEventArgs args)
    {
        // 我们只关心服务器端，并且只在 "Game" 场景加载完成后操作
        if (!InstanceFinder.IsServer || args.LoadedScenes.Length == 0 || args.LoadedScenes[0].name != "Game")
        {
            return;
        }

        Debug.Log("[LobbyManager] 'Game' scene loaded on server. Spawning GameNetworkManager.");

        // 实例化 GameNetworkManager Prefab
        GameObject gnmInstance = Instantiate(gameNetworkManagerPrefab);

        // 通过服务器生成这个实例，这样所有客户端都会同步创建它
        InstanceFinder.ServerManager.Spawn(gnmInstance);
    }

    /// <summary>
    /// 当本地客户端的连接状态发生变化时，此方法会被调用。
    /// </summary>
    private void ClientManager_OnClientConnectionState(FishNet.Transporting.ClientConnectionStateArgs args)
    {
        Debug.Log($"[CLIENT-LOG] 本地客户端连接状态变化: {args.ConnectionState}");
        if (args.ConnectionState == FishNet.Transporting.LocalConnectionState.Stopped)
        {
            // 如果连接停止，可能是因为连接失败或被服务器踢出
            // 可以在这里添加UI提示，例如“连接主机失败”
            Debug.LogError("[CLIENT-LOG] 连接已停止。可能原因：无法连接到主机、主机关闭、网络问题。");
        }
    }

    /// <summary>
    /// 当一个远程客户端的连接状态发生变化时，此方法会被服务器调用。
    /// </summary>
    private void ServerManager_OnRemoteConnectionState(FishNet.Connection.NetworkConnection conn, FishNet.Transporting.RemoteConnectionStateArgs args)
    {
        // 在服务器端增加更详细的日志
        Debug.Log($"[SERVER-LOG] 远程客户端 {conn.ClientId} 连接状态变化: {args.ConnectionState}");

        if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Started)
        {
            Debug.Log($"[Server] 客户端 {conn.ClientId} 已完全连接。");
            // 在这里，我们可以检查是否所有人都已到齐
            CheckIfAllPlayersAreReadyAndStartGame();
        }
        else if (args.ConnectionState == FishNet.Transporting.RemoteConnectionState.Stopped)
        {
            Debug.Log($"[Server] 客户端 {conn.ClientId} 已断开连接。");
        }
    }

    #region Public UI-facing Methods

    /// <summary>
    /// UI调用：请求创建一个Lobby
    /// </summary>
    public void CreateLobby(bool isPublic, string lobbyName, string gameMode, string roomLevel)
    {
        Debug.Log($"[LobbyManager] 请求创建Lobby... 公开: {isPublic}, 名称: {lobbyName}");
        ELobbyType lobbyType = isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly;

        // 确保每次创建时都是全新的数据
        CurrentLobbyData = new Dictionary<string, string>
        {
            [GameIdKey] = GameIdValue,
            [StatusKey] = StatusWaiting,
            [LobbyNameKey] = lobbyName,
            [GameModeKey] = gameMode,
            [RoomLevelKey] = roomLevel
        };

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

        Debug.Log($"[LobbyManager] 添加搜索过滤器: Key='{GameIdKey}', Value='{GameIdValue}'");

        SteamMatchmaking.AddRequestLobbyListStringFilter(GameIdKey, GameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);

        // 强制将搜索距离过滤器设置为全球范围，排除地理位置因素
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);
        Debug.Log("[LobbyManager] 添加了 Worldwide 距离过滤器。");

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
            _isLoadingScene = false; // 重置状态
            _isTryingToStartGame = false; // 同样重置开始意图
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
            //Fishy Steamworks 需要勾选 Peer To Peer
            Debug.LogWarning("[LobbyManager] 只有房主才能开始游戏。");
            return;
        }

        Debug.Log("[LobbyManager] 房主开始游戏...");

        _isTryingToStartGame = true; // 1. 设置开始游戏的意图标志

        // 2. 调用检查方法。如果人已经齐了，它会立刻开始。如果人没齐，它什么也不做，等待客户端连接事件来触发。
        CheckIfAllPlayersAreReadyAndStartGame();

    }


    /// <summary>
    /// 检查是否满足开始游戏的条件，如果满足则加载游戏场景。
    /// </summary>
    private void CheckIfAllPlayersAreReadyAndStartGame()
    {
        // 条件1: 必须是服务器才能执行此逻辑
        // 条件2: 房主必须已经点击了开始按钮 (标志位为true)
        // 条件3: 游戏不能已经开始 (避免重复加载)
        if (!InstanceFinder.IsServer || !_isTryingToStartGame || _isLoadingScene) // 使用我们自己的标志位
        {
            return;
        }

        // 条件4: 检查人数是否足够。Lobby里有2个人，并且服务器也确认有1个远程连接(2-1=1)
        int steamLobbyMemberCount = SteamMatchmaking.GetNumLobbyMembers(_currentLobbyId);
        int connectedFishNetClients = _networkManager.ServerManager.Clients.Count; // 这包含了Host自己，所以是 (远程客户端数 + 1)

        Debug.Log($"[StartCheck] 检查开始条件: Steam人数={steamLobbyMemberCount}, FishNet连接数={connectedFishNetClients}");

        // 我们的游戏是2人对战
        if (steamLobbyMemberCount == 2 && connectedFishNetClients == 2)
        {
            Debug.Log("[StartCheck] 条件满足！所有玩家已就绪，正在加载游戏场景...");

            _isLoadingScene = true; // 在加载前，立刻设置标志位

            // --- 这部分是原StartGame的核心逻辑 ---
            // 1. 更新Lobby状态为“游戏中”，并设为不可加入
            SteamMatchmaking.SetLobbyData(_currentLobbyId, StatusKey, StatusInGame);
            SteamMatchmaking.SetLobbyJoinable(_currentLobbyId, false);

            // 2. 通过FishNet的场景管理器加载游戏场景
            var sld = new SceneLoadData("Game");
            sld.ReplaceScenes = ReplaceOption.All;
            _networkManager.SceneManager.LoadGlobalScenes(sld);

            Debug.Log("[LobbyManager] 已向所有客户端发送加载 'Game' 场景的指令。");

            // 3. 重置标志位，防止重复执行
            _isTryingToStartGame = false;
        }
        else
        {
            Debug.Log($"[StartCheck] 条件未满足，等待更多玩家连接...");
        }
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

        Debug.Log($"[LobbyManager] 正在为Lobby {_currentLobbyId} 设置 {CurrentLobbyData.Count} 条元数据...");
        foreach (var dataPair in CurrentLobbyData)
        {
            Debug.Log($"[LobbyManager] -> SetData: '{dataPair.Key}' = '{dataPair.Value}'");
            SteamMatchmaking.SetLobbyData(_currentLobbyId, dataPair.Key, dataPair.Value);
        }

        // 创建Lobby成功后，Steam会自动让创建者"进入"这个Lobby，
        // 这会触发 OnLobbyEntered 回调。我们将把网络启动逻辑统一放到那里。
        Debug.Log("[LobbyManager] Lobby已创建，等待OnLobbyEntered回调来启动网络...");
    }

    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"[LobbyManager] 收到好友的游戏邀请，正在加入Lobby: {callback.m_steamIDLobby}");
        JoinLobby(callback.m_steamIDLobby);
    }

    #region Steam Callback Handlers
    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager] 已进入Lobby: {_currentLobbyId}");

        // 判断当前进入Lobby的人是不是Lobby的创建者(Owner)
        CSteamID lobbyOwner = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);
        CSteamID mySteamId = SteamManager.Instance.PlayerSteamId;

        if (lobbyOwner == mySteamId)
        {
            // 我是房主
            Debug.Log("[LobbyManager] 身份确认：我是房主。正在启动Host模式...");
            _networkManager.ServerManager.StartConnection();
            _networkManager.ClientManager.StartConnection();
            Debug.Log("[LobbyManager] FishNet Host模式已启动。");
        }
        else
        {
            // 我是客户端
            Debug.Log($"[LobbyManager] [CLIENT-LOG] 身份确认：我是客户端。目标Host SteamID: {lobbyOwner}");

            if (_networkManager.ClientManager.Started)
            {
                Debug.LogWarning("[LobbyManager] [CLIENT-LOG] 客户端网络已在运行，不再重复启动。");
                // 这里可能需要考虑是否要断开重连，如果目标Host变了的话。
                // 暂时先简单处理：如果已连接但不是连的这个Lobby Owner，可能需要处理。
                return;
            }

            // 显式设置要连接的Steam ID地址
            var fishy = _networkManager.TransportManager.GetTransport<FishySteamworks.FishySteamworks>();
            if (fishy != null)
            {
                Debug.Log($"[LobbyManager] [CLIENT-LOG] 正在设置FishySteamworks的目标地址为: {lobbyOwner}");
                fishy.SetClientAddress(lobbyOwner.ToString());
            }
            else
            {
                Debug.LogError("[LobbyManager] [CLIENT-LOG] 未找到 FishySteamworks Transport组件！无法设置目标地址。");
            }

            Debug.Log("[LobbyManager] [CLIENT-LOG] 正在调用 ClientManager.StartConnection()...");
            // 现在StartConnection会使用我们刚刚设置的地址
            bool success = _networkManager.ClientManager.StartConnection();

            Debug.Log($"[LobbyManager] [CLIENT-LOG] ClientManager.StartConnection() 调用返回: {success}");
            if (!success)
            {
                Debug.LogError("[LobbyManager] [CLIENT-LOG] ClientManager.StartConnection() 调用失败！");
            }
        }

        // 缓存最新Lobby数据并更新UI
        CacheLobbyData();
        OnEnteredLobby?.Invoke(_currentLobbyId);

    }
    #endregion

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
        Debug.Log($"[LobbyManager] OnLobbyMatchList 回调被触发。找到 {lobbyCount} 个匹配的Lobby。");

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