// 文件路径: Assets/Scripts/_App/GameManagement/SteamLobbyManager.cs

using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

public class SteamLobbyManager : PersistentSingleton<SteamLobbyManager>
{
    private bool _isSteamInitialized = false;

    private Dictionary<CSteamID, Texture2D> _avatarCache = new Dictionary<CSteamID, Texture2D>();

    public CSteamID CurrentLobbyId { get; private set; } = CSteamID.Nil;

    // Lobby元数据的Key常量
    private const string LOBBY_MODE_KEY = "Mode";
    private const string LOBBY_MODE_VALUE_1V1 = "1v1";

    #region C# Events
    public event Action<CSteamID> OnAvatarReady;
    public event Action<CSteamID> OnLobbyCreatedSuccess;
    public event Action<CSteamID> OnLobbyEntered;
    public event Action OnMatchReady;
    private bool _isMatchReadyTriggered = false;
    #endregion

    #region Steam Callbacks & CallResults
    protected Callback<AvatarImageLoaded_t> m_AvatarImageLoaded;
    protected Callback<LobbyEnter_t> m_LobbyEnter;
    protected Callback<LobbyCreated_t> m_LobbyCreated;
    protected Callback<LobbyChatUpdate_t> m_LobbyChatUpdate;
    private CallResult<LobbyMatchList_t> m_LobbyMatchListCallResult;
    #endregion
    protected override void Awake()
    {
        base.Awake();
        if (!_isSteamInitialized)
        {
            try
            {
                if (SteamAPI.Init())
                {
                    _isSteamInitialized = true;
                    Debug.Log("[SteamLobbyManager] Steamworks API 初始化成功！");

                    m_AvatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
                    m_LobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnterCallback);
                    m_LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreatedCallback);
                    m_LobbyChatUpdate = Callback<LobbyChatUpdate_t>.Create(OnLobbyChatUpdateCallback); // 新增: 注册回调
                    m_LobbyMatchListCallResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchListCallback);
                }
                else
                {
                    Debug.LogError("[SteamLobbyManager] Steamworks API 初始化失败！");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[SteamLobbyManager] Steamworks API 初始化时发生异常: {e.Message}");
            }
        }
    }

    private void Update()
    {
        if (_isSteamInitialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        LeaveLobby();
        if (_isSteamInitialized)
        {
            SteamAPI.Shutdown();
            Debug.Log("[SteamLobbyManager] Steamworks API 已关闭。");
        }
    }
    #region Public Methods - Lobby Management

    public void FindOrCreateLobby()
    {
        if (!_isSteamInitialized)
        {
            Debug.LogError("[SteamLobbyManager] Steam未初始化，无法寻找Lobby。");
            return;
        }

        _isMatchReadyTriggered = false;

        Debug.Log("[SteamLobbyManager] 开始寻找或创建Lobby...");
        RequestLobbyListFiltered(m_LobbyMatchListCallResult, OnLobbyMatchListCallback);
    }

    public void LeaveLobby()
    {
        if (CurrentLobbyId != CSteamID.Nil)
        {
            Debug.Log($"[SteamLobbyManager] 正在离开Lobby: {CurrentLobbyId}");
            SteamMatchmaking.LeaveLobby(CurrentLobbyId);
            CurrentLobbyId = CSteamID.Nil;

            _isMatchReadyTriggered = false;
        }
    }

    private CallResult<LobbyMatchList_t> m_DebugLobbyMatchListCallResult;

    // 统一的、最终的Lobby搜索逻辑
    private void RequestLobbyListFiltered(CallResult<LobbyMatchList_t> callResult, CallResult<LobbyMatchList_t>.APIDispatchDelegate callback)
    {
        // 在首次调用调试时初始化
        if (callResult == null)
        {
            callResult = CallResult<LobbyMatchList_t>.Create(callback);
            m_DebugLobbyMatchListCallResult = callResult; // 缓存起来
        }

        // --- 统一的过滤条件 ---
        // 1. 必须是我们的游戏
        SteamMatchmaking.AddRequestLobbyListStringFilter(GameConstants.SteamLobbyGameIdKey, GameConstants.SteamLobbyGameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);
        // 2. 必须是1v1模式
        SteamMatchmaking.AddRequestLobbyListStringFilter(LOBBY_MODE_KEY, LOBBY_MODE_VALUE_1V1, ELobbyComparison.k_ELobbyComparisonEqual);
        // 3. 扩大搜索范围
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

        // **注意: 我们暂时移除了 AddRequestLobbyListFilterSlotsAvailable(1) 过滤器，以提高匹配成功率 **

        SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
        callResult.Set(handle);
        Debug.Log("[SteamLobbyManager] 已发送统一的Lobby列表请求。");
    }

    // 调试方法现在也调用统一的搜索逻辑
    public void Debug_RequestLobbyList()
    {
        Debug.Log("[DEBUG] 手动发起统一的Lobby列表请求...");
        RequestLobbyListFiltered(m_DebugLobbyMatchListCallResult, OnDebugLobbyMatchListCallback);
    }

    #endregion

    #region Steam Callback Handlers

    private void OnDebugLobbyMatchListCallback(LobbyMatchList_t callback, bool ioFailure)
    {
        Debug.Log($"--- [DEBUG] Lobby列表查询结果 ---");
        if (ioFailure)
        {
            Debug.LogError($"[DEBUG] 查询IO失败!");
            return;
        }

        Debug.Log($"[DEBUG] 查询成功，找到 {callback.m_nLobbiesMatching} 个与本游戏相关的Lobby。");

        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            Debug.Log($"--- Lobby [{i + 1}/{callback.m_nLobbiesMatching}] ---");
            Debug.Log($"  Lobby ID: {lobbyId}");

            CSteamID ownerId = SteamMatchmaking.GetLobbyOwner(lobbyId);
            if (!ownerId.IsValid() || ownerId.m_SteamID == 0)
            {
                Debug.LogWarning($"  Owner: [信息不可用 - ID无效或为0]。");
            }
            else
            {
                string ownerName = SteamFriends.GetFriendPersonaName(ownerId);
                Debug.Log($"  Owner: {ownerName} (ID: {ownerId})");
            }

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            int memberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);
            Debug.Log($"  成员: {memberCount} / {memberLimit}");

            int dataCount = SteamMatchmaking.GetLobbyDataCount(lobbyId);
            Debug.Log($"  元数据 ({dataCount} 条):");
            for (int j = 0; j < dataCount; j++)
            {
                SteamMatchmaking.GetLobbyDataByIndex(lobbyId, j, out string key, 256, out string value, 4096);
                Debug.Log($"    - '{key}': '{value}'");
            }
        }
        Debug.Log($"--- [DEBUG] 查询结束 ---");
    }

    private void OnLobbyMatchListCallback(LobbyMatchList_t callback, bool ioFailure)
    {
        Debug.Log($"[SteamLobbyManager] OnLobbyMatchListCallback: 收到回调。IO Failure: {ioFailure}, 匹配到的Lobby数量: {callback.m_nLobbiesMatching}");

        if (ioFailure)
        {
            Debug.LogError("[SteamLobbyManager] 搜索Lobby时发生IO错误。");
            return;
        }

        // **关键逻辑: 手动遍历并检查空位**
        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            int maxMembers = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);

            // 检查是否有空位
            if (memberCount < maxMembers)
            {
                Debug.Log($"[SteamLobbyManager] 在结果中找到了一个有空位的Lobby (ID: {lobbyId}, {memberCount}/{maxMembers})，尝试加入...");
                SteamMatchmaking.JoinLobby(lobbyId);
                return; // 找到就加入并退出
            }
            else
            {
                Debug.Log($"[SteamLobbyManager] 找到Lobby (ID: {lobbyId})，但已满 ({memberCount}/{maxMembers})，跳过。");
            }
        }

        Debug.Log("[SteamLobbyManager] 未找到任何有空位的Lobby，将创建一个新的。");
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 2);
    }
    private void OnLobbyCreatedCallback(LobbyCreated_t callback)
    {
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            Debug.Log($"[SteamLobbyManager] Lobby 创建成功! Lobby ID: {lobbyId}");
            SteamMatchmaking.SetLobbyData(lobbyId, GameConstants.SteamLobbyGameIdKey, GameConstants.SteamLobbyGameIdValue);
            SteamMatchmaking.SetLobbyData(lobbyId, LOBBY_MODE_KEY, LOBBY_MODE_VALUE_1V1);
            SteamMatchmaking.SetLobbyJoinable(lobbyId, true);
            Debug.Log($"[SteamLobbyManager] Lobby属性已设置: GameId='{GameConstants.SteamLobbyGameIdValue}', Mode='{LOBBY_MODE_VALUE_1V1}', Joinable=true");
            OnLobbyCreatedSuccess?.Invoke(lobbyId);
        }
        else { Debug.LogError($"[SteamLobbyManager] Lobby 创建失败! Result: {callback.m_eResult}"); }
    }

    private void OnLobbyEnterCallback(LobbyEnter_t callback)
    {
        CurrentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        Debug.Log($"[SteamLobbyManager] (OnLobbyEnter) 我成功进入了Lobby: {CurrentLobbyId}.");
        OnLobbyEntered?.Invoke(CurrentLobbyId);

        CheckIfLobbyIsFull();
    }

    private void OnLobbyChatUpdateCallback(LobbyChatUpdate_t callback)
    {
        if ((CSteamID)callback.m_ulSteamIDLobby != CurrentLobbyId) return;

        Debug.Log($"[SteamLobbyManager] (OnLobbyChatUpdate) Lobby状态更新。");

        CheckIfLobbyIsFull();
    }

    private void CheckIfLobbyIsFull()
    {
        // 如果事件已触发，则直接返回，防止重复执行
        if (_isMatchReadyTriggered) return;

        int memberCount = SteamMatchmaking.GetNumLobbyMembers(CurrentLobbyId);
        int maxMembers = SteamMatchmaking.GetLobbyMemberLimit(CurrentLobbyId);

        Debug.Log($"[CheckIfLobbyIsFull] 检查当前人数: {memberCount}/{maxMembers}");

        if (memberCount >= maxMembers)
        {
            // 设置状态锁，确保只触发一次
            _isMatchReadyTriggered = true;

            Debug.Log("[CheckIfLobbyIsFull] 检测到Lobby已满员，广播OnMatchReady事件！");

            if (SteamMatchmaking.GetLobbyOwner(CurrentLobbyId) == SteamUser.GetSteamID())
            {
                SteamMatchmaking.SetLobbyJoinable(CurrentLobbyId, false);
                Debug.Log("[CheckIfLobbyIsFull] 我是房主，已将Lobby设为不可加入。");
            }

            OnMatchReady?.Invoke();
        }
    }

    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        CSteamID steamId = callback.m_steamID;
        int imageId = callback.m_iImage;
        if (_avatarCache.ContainsKey(steamId)) return;
        var avatarTexture = GetSteamImageAsTexture2D(imageId);
        if (avatarTexture != null)
        {
            _avatarCache[steamId] = avatarTexture;
            OnAvatarReady?.Invoke(steamId);
        }
    }

    #endregion

    #region Public Methods - Player & Avatar
    public PlayerProfile GetLocalPlayerProfile()
    {
        if (!_isSteamInitialized) return new PlayerProfile { nickname = "Offline Player" };

        return new PlayerProfile
        {
            steamId = SteamUser.GetSteamID().m_SteamID,
            nickname = SteamFriends.GetPersonaName(),
            goldCoins = 9999,
            eloRating = 1500
        };
    }

    public Texture2D GetAvatar(CSteamID steamId)
    {
        if (_avatarCache.TryGetValue(steamId, out Texture2D texture))
        {
            return texture;
        }

        int imageId = SteamFriends.GetLargeFriendAvatar(steamId);

        if (imageId > 0)
        {
            return GetSteamImageAsTexture2D(imageId);
        }

        return null;
    }
    #endregion

    #region Helper Methods
    private Texture2D GetSteamImageAsTexture2D(int iImage)
    {
        if (!SteamUtils.GetImageSize(iImage, out uint width, out uint height)) return null;

        byte[] imageData = new byte[width * height * 4];
        if (!SteamUtils.GetImageRGBA(iImage, imageData, (int)(width * height * 4))) return null;

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
        texture.LoadRawTextureData(imageData);
        texture.Apply();

        Color[] pixels = texture.GetPixels();
        Color[] flippedPixels = new Color[pixels.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flippedPixels[y * width + x] = pixels[((height - 1 - y) * width) + x];
            }
        }

        Texture2D flippedTexture = new Texture2D((int)width, (int)height);
        flippedTexture.SetPixels(flippedPixels);
        flippedTexture.Apply();

        Destroy(texture);

        return flippedTexture;
    }
    #endregion
}