// File: _Scripts/Network/LobbyManager.cs

using UnityEngine;
using Steamworks;
using FishNet.Managing;
using System.Collections.Generic;
using FishNet; // 引入FishNet
using FishNet.Managing.Scened; // 引入场景管理
using System;
using System.Collections; // 引入协程命名空间

/// <summary>
/// 功能模块，负责所有与Steam Lobby相关的操作：创建、查找、加入、离开、状态管理。
/// 并管理Lobby相关的UI面板切换。
/// </summary>
public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    #region Lobby Configuration
    [Header("Lobby配置")]
    public const string HostAddressKey = "HostAddress"; // FishySteamworks 需要这个Key
    public const string GameIdKey = "game_id";
    public const string StatusKey = "status";
    public const string LobbyNameKey = "name";
    public const string GameModeKey = "game_mode";
    public const string RoomLevelKey = "room_level";
    public const string GameIdValue = "ChineseChessHonor";
    public const string StatusWaiting = "waiting";
    public const string StatusInGame = "ingame";
    #endregion

    #region UI References
    [Header("UI引用 (Lobby列表)")]
    public GameObject lobbyItemPrefab;
    public Transform lobbyListContent;
    #endregion

    #region Private State
    public CSteamID _currentLobbyId; // 改为public，方便在Inspector中观察
    private List<GameObject> _currentLobbyListItems = new List<GameObject>();
    public Dictionary<string, string> CurrentLobbyData { get; private set; } = new Dictionary<string, string>();
    #endregion

    #region Steam Callbacks
    protected Callback<LobbyCreated_t> _lobbyCreated;
    protected Callback<LobbyEnter_t> _lobbyEntered;
    protected Callback<LobbyDataUpdate_t> _lobbyDataUpdate;
    protected Callback<LobbyMatchList_t> _lobbyMatchList;
    protected Callback<GameLobbyJoinRequested_t> _gameLobbyJoinRequested;
    protected Callback<LobbyChatUpdate_t> _lobbyChatUpdate;
    #endregion

    #region C# Events
    public static event Action<CSteamID> OnEnteredLobby;
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
        DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        Debug.Log("[LobbyManager.Start] LobbyManager 开始初始化...");
        if (!SteamManager.Instance.IsSteamInitialized)
        {
            Debug.LogError("[LobbyManager.Start] Steam尚未初始化！Lobby功能将不可用。");
            this.enabled = false;
            return;
        }

        Debug.Log("[LobbyManager.Start] 正在注册Steam回调...");
        _lobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
        _lobbyEntered = Callback<LobbyEnter_t>.Create(OnLobbyEntered);
        _lobbyDataUpdate = Callback<LobbyDataUpdate_t>.Create(OnLobbyDataUpdated);
        _lobbyMatchList = Callback<LobbyMatchList_t>.Create(OnLobbyMatchList);
        _gameLobbyJoinRequested = Callback<GameLobbyJoinRequested_t>.Create(OnGameLobbyJoinRequested);
        _lobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdate);
        Debug.Log("[LobbyManager.Start] Steam回调注册完成。");
    }

    #region Public UI-facing Methods

    public void CreateLobby(bool isPublic, string lobbyName, string gameMode, string roomLevel)
    {
        Debug.Log($"[LobbyManager.CreateLobby] UI请求创建Lobby... 公开: {isPublic}, 名称: {lobbyName}");
        ELobbyType lobbyType = isPublic ? ELobbyType.k_ELobbyTypePublic : ELobbyType.k_ELobbyTypeFriendsOnly;

        CurrentLobbyData.Clear();
        CurrentLobbyData[GameIdKey] = GameIdValue;
        CurrentLobbyData[StatusKey] = StatusWaiting;
        CurrentLobbyData[LobbyNameKey] = lobbyName;
        CurrentLobbyData[GameModeKey] = gameMode;
        CurrentLobbyData[RoomLevelKey] = roomLevel;

        Debug.Log("[LobbyManager.CreateLobby] 即将调用 SteamMatchmaking.CreateLobby API...");
        SteamMatchmaking.CreateLobby(lobbyType, 2);
    }

    public void RefreshLobbyList()
    {
        if (!SteamManager.Instance.IsSteamInitialized) return;

        Debug.Log("[LobbyManager.RefreshLobbyList] 正在请求Lobby列表...");
        ClearLobbyListUI();
        SteamMatchmaking.AddRequestLobbyListStringFilter(GameIdKey, GameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.RequestLobbyList();
    }

    public void JoinLobby(CSteamID lobbyId)
    {
        Debug.Log($"[LobbyManager.JoinLobby] 正在尝试加入Lobby: {lobbyId}");
        SteamMatchmaking.JoinLobby(lobbyId);
    }

    public void LeaveLobby()
    {
        if (_currentLobbyId.IsValid())
        {
            Debug.Log($"[LobbyManager.LeaveLobby] 正在离开Lobby: {_currentLobbyId}");
            SteamMatchmaking.LeaveLobby(_currentLobbyId);
            _currentLobbyId = CSteamID.Nil;

            var networkManager = InstanceFinder.NetworkManager;
            if (networkManager != null)
            {
                Debug.Log($"[LobbyManager.LeaveLobby] NetworkManager 状态: IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient}");
                if (networkManager.IsServer)
                {
                    Debug.Log("[LobbyManager.LeaveLobby] 停止服务器连接...");
                    networkManager.ServerManager.StopConnection(true);
                }
                if (networkManager.IsClient)
                {
                    Debug.Log("[LobbyManager.LeaveLobby] 停止客户端连接...");
                    networkManager.ClientManager.StopConnection();
                }
            }
            else
            {
                Debug.LogWarning("[LobbyManager.LeaveLobby] 未找到 NetworkManager 实例。");
            }
        }

        MainMenuController.Instance.ShowMainPanel();
    }

    public void StartGame()
    {
        Debug.Log("[LobbyManager.StartGame] '开始游戏' 按钮被点击。");

        // --- 核心调试日志 ---
        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager == null)
        {
            Debug.LogError("[LobbyManager.StartGame] 严重错误: InstanceFinder.NetworkManager 返回 null！");
            return;
        }

        // 打印出 NetworkManager 的所有相关状态
        Debug.Log($"[LobbyManager.StartGame] --- NetworkManager 状态诊断 ---");
        Debug.Log($"[LobbyManager.StartGame] networkManager.IsServer: {networkManager.IsServer}");
        Debug.Log($"[LobbyManager.StartGame] networkManager.IsClient: {networkManager.IsClient}");
        Debug.Log($"[LobbyManager.StartGame] networkManager.ServerManager.Started: {networkManager.ServerManager.Started}");
        Debug.Log($"[LobbyManager.StartGame] networkManager.ClientManager.Started: {networkManager.ClientManager.Started}");
        Debug.Log($"[LobbyManager.StartGame] --- 诊断结束 ---");

        // 使用 networkManager 实例来判断，而不是 InstanceFinder 的静态属性，以确保我们检查的是同一个对象
        if (!networkManager.IsServer)
        {
            Debug.LogWarning("[LobbyManager.StartGame] 判断失败: 'networkManager.IsServer' 为 false。流程中断。");
            // 为了找出原因，我们再检查一下静态属性作为对比
            Debug.LogWarning($"[LobbyManager.StartGame] 对比: InstanceFinder.IsServer = {InstanceFinder.IsServer}");
            return;
        }

        Debug.Log("[LobbyManager.StartGame] 房主身份验证通过。继续执行...");

        // 1. 更新Lobby状态
        SteamMatchmaking.SetLobbyData(_currentLobbyId, StatusKey, StatusInGame);
        SteamMatchmaking.SetLobbyJoinable(_currentLobbyId, false);
        Debug.Log("[LobbyManager.StartGame] Steam Lobby 状态已更新为 '游戏中' 且不可加入。");

        // 2. 加载场景
        var sld = new SceneLoadData("Game");
        networkManager.SceneManager.LoadGlobalScenes(sld);
        Debug.Log("[LobbyManager.StartGame] 已向所有客户端发送加载 'Game' 场景的指令。");
    }

    #endregion

    #region Steam Callback Handlers

    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        Debug.Log($"[LobbyManager.OnLobbyCreated] 收到 Steam 的 LobbyCreated 回调。结果: {callback.m_eResult}");

        if (callback.m_eResult != EResult.k_EResultOK)
        {
            // ... (失败逻辑不变)
            return;
        }

        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager.OnLobbyCreated] Lobby创建成功! Lobby ID: {_currentLobbyId}");

        // 1. 将房主的SteamID作为HostAddress写入Lobby，这是FishySteamworks的关键
        CSteamID mySteamId = SteamUser.GetSteamID();
        SteamMatchmaking.SetLobbyData(_currentLobbyId, HostAddressKey, mySteamId.ToString());
        Debug.Log($"[LobbyManager.OnLobbyCreated] 已将 HostAddress ({mySteamId}) 写入Lobby元数据。");
        // 2. 写入其他元数据
        foreach (var dataPair in CurrentLobbyData)
        {
            SteamMatchmaking.SetLobbyData(_currentLobbyId, dataPair.Key, dataPair.Value);
        }
        Debug.Log("[LobbyManager.OnLobbyCreated] Lobby 元数据设置完成。");

        StartCoroutine(StartHostSequence());

    }

    private IEnumerator StartHostSequence()
    {
        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager == null)
        {
            Debug.LogError("[LobbyManager.StartHostSequence] 找不到 NetworkManager，无法启动Host。");
            yield break;
        }

        // 1. 先启动服务器
        Debug.Log("[LobbyManager.StartHostSequence] 步骤 1: 启动服务器 (ServerManager)...");
        networkManager.ServerManager.StartConnection();

        // 等待一小段时间，确保服务器完全启动并且Lobby数据有时间同步
        yield return new WaitForSeconds(1.3f);

        // 2. 再启动客户端
        Debug.Log("[LobbyManager.StartHostSequence] 步骤 2: 启动客户端 (ClientManager)...");
        networkManager.ClientManager.StartConnection();

        // 3. 再次等待后检查最终状态
        yield return new WaitForSeconds(1.3f);
        Debug.Log($"[LobbyManager.StartHostSequence] 步骤 3: 最终状态检查: IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient}");
    }


    private System.Collections.IEnumerator CheckServerStatusAfterDelay()
    {
        // 等待0.5秒，给NetworkManager足够的时间去完成异步启动
        yield return new WaitForSeconds(0.5f);

        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager != null)
        {
            Debug.Log($"[LobbyManager.Coroutine] 延迟0.5秒后检查状态: IsServer={networkManager.IsServer}, IsClient={networkManager.IsClient}");
        }
    }


    private void OnGameLobbyJoinRequested(GameLobbyJoinRequested_t callback)
    {
        Debug.Log($"[LobbyManager.OnGameLobbyJoinRequested] 收到好友的游戏邀请，正在加入Lobby: {callback.m_steamIDLobby}");
        JoinLobby(callback.m_steamIDLobby);
    }


    private void OnLobbyEntered(LobbyEnter_t callback)
    {
        _currentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[LobbyManager.OnLobbyEntered] 已进入Lobby: {_currentLobbyId}");

        var networkManager = InstanceFinder.NetworkManager;
        if (networkManager != null)
        {
            // ----------------- CHANGE START -----------------
            // 关键逻辑修正：判断自己是不是这个Lobby的房主
            CSteamID mySteamId = SteamUser.GetSteamID();
            CSteamID lobbyOwnerId = SteamMatchmaking.GetLobbyOwner(_currentLobbyId);

            if (mySteamId == lobbyOwnerId)
            {
                // 我就是房主。
                // 在 OnLobbyCreated 中已经启动了Host模式，这里不需要做任何事。
                // 增加一个日志来确认这一点。
                Debug.Log("[LobbyManager.OnLobbyEntered] 检测到当前玩家是Lobby房主，无需执行网络操作。");
            }
            else
            {
                // 我是后加入的客户端。
                Debug.Log("[LobbyManager.OnLobbyEntered] 检测到当前为 Client (非房主)，准备启动 Client 模式...");
                networkManager.ClientManager.StartConnection();
                Debug.Log($"[LobbyManager.OnLobbyEntered] FishNet Client已启动，正在连接到Host: {lobbyOwnerId}");
            }
            // ----------------- CHANGE END -----------------
        }
        else
        {
            Debug.LogError("[LobbyManager.OnLobbyEntered] 严重错误: 未能找到 NetworkManager 实例！");
        }

        CacheLobbyData();
        OnEnteredLobby?.Invoke(_currentLobbyId);
    }

    private void OnLobbyDataUpdated(LobbyDataUpdate_t callback)
    {
        if ((CSteamID)callback.m_ulSteamIDLobby == _currentLobbyId)
        {
            Debug.Log("[LobbyManager] 当前Lobby数据已更新。");
            CacheLobbyData();
            OnLobbyDataUpdatedEvent?.Invoke();
        }
    }

    private void OnLobbyChatUpdate(LobbyChatUpdate_t callback)
    {
        if ((CSteamID)callback.m_ulSteamIDLobby == _currentLobbyId)
        {
            Debug.Log("[LobbyManager] 房间内玩家状态变化。");
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