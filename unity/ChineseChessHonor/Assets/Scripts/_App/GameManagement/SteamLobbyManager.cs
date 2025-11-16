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
    #endregion

    #region Steam Callbacks & CallResults
    protected Callback<AvatarImageLoaded_t> m_AvatarImageLoaded;
    protected Callback<LobbyEnter_t> m_LobbyEnter;
    protected Callback<LobbyCreated_t> m_LobbyCreated;
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

        Debug.Log("[SteamLobbyManager] 开始寻找或创建Lobby...");

        SteamMatchmaking.AddRequestLobbyListStringFilter(GameConstants.SteamLobbyGameIdKey, GameConstants.SteamLobbyGameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListStringFilter(LOBBY_MODE_KEY, LOBBY_MODE_VALUE_1V1, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);

        SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
        m_LobbyMatchListCallResult.Set(handle);
        Debug.Log("[SteamLobbyManager] Lobby列表请求已发送，过滤器: GameId, Mode, SlotsAvailable");
    }

    public void LeaveLobby()
    {
        if (CurrentLobbyId != CSteamID.Nil)
        {
            Debug.Log($"[SteamLobbyManager] 正在离开Lobby: {CurrentLobbyId}");
            SteamMatchmaking.LeaveLobby(CurrentLobbyId);
            CurrentLobbyId = CSteamID.Nil;
        }
    }

    // 独立的CallResult用于调试，避免与主流程冲突
    private CallResult<LobbyMatchList_t> m_DebugLobbyMatchListCallResult;

    // 调试方法，只查询并打印Lobby列表
    public void Debug_RequestLobbyList()
    {
        // 在首次调用时初始化
        if (m_DebugLobbyMatchListCallResult == null)
        {
            m_DebugLobbyMatchListCallResult = CallResult<LobbyMatchList_t>.Create(OnDebugLobbyMatchListCallback);
        }

        Debug.Log("[DEBUG] 手动发起Lobby列表请求（带GameId过滤）...");
        SteamMatchmaking.AddRequestLobbyListStringFilter(GameConstants.SteamLobbyGameIdKey, GameConstants.SteamLobbyGameIdValue, ELobbyComparison.k_ELobbyComparisonEqual);

        // 我们甚至可以设置一个距离过滤器来优先显示附近的房间
        SteamMatchmaking.AddRequestLobbyListDistanceFilter(ELobbyDistanceFilter.k_ELobbyDistanceFilterWorldwide);

        SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
        m_DebugLobbyMatchListCallResult.Set(handle);
    }

    #endregion

    #region Steam Callback Handlers

    // 调试专用的回调处理器
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
            // **关键日志: 检查Owner ID是否有效**
            if (!ownerId.IsValid() || ownerId.m_SteamID == 0)
            {
                Debug.LogWarning($"  Owner: [信息不可用 - ID无效或为0]。这是正常的，因为我们尚未加入此Lobby。");
            }
            else
            {
                string ownerName = SteamFriends.GetFriendPersonaName(ownerId);
                Debug.Log($"  Owner: {ownerName} (ID: {ownerId})");
            }

            // **关键日志: 检查元数据是否能在加入前获取**
            string gameId = SteamMatchmaking.GetLobbyData(lobbyId, GameConstants.SteamLobbyGameIdKey);
            string mode = SteamMatchmaking.GetLobbyData(lobbyId, LOBBY_MODE_KEY);
            if (string.IsNullOrEmpty(gameId))
            {
                Debug.LogWarning($"  元数据 'GameId': [信息不可用]。");
            }
            else
            {
                Debug.Log($"  元数据 'GameId': '{gameId}'");
            }
            if (string.IsNullOrEmpty(mode))
            {
                Debug.LogWarning($"  元数据 'Mode': [信息不可用]。");
            }
            else
            {
                Debug.Log($"  元数据 'Mode': '{mode}'");
            }
        }
        Debug.Log($"--- [DEBUG] 查询结束 ---");
    }

    // 主匹配流程的回调处理器
    private void OnLobbyMatchListCallback(LobbyMatchList_t callback, bool ioFailure)
    {
        Debug.Log($"[SteamLobbyManager] OnLobbyMatchListCallback: 收到回调。IO Failure: {ioFailure}, 匹配到的Lobby数量: {callback.m_nLobbiesMatching}");

        if (ioFailure)
        {
            Debug.LogError("[SteamLobbyManager] 搜索Lobby时发生IO错误。");
            return;
        }

        // 简化并加固逻辑: 只要搜索有结果，就信任过滤器并尝试加入第一个
        if (callback.m_nLobbiesMatching > 0)
        {
            CSteamID lobbyToJoin = SteamMatchmaking.GetLobbyByIndex(0);
            Debug.Log($"[SteamLobbyManager] 过滤器返回了 {callback.m_nLobbiesMatching} 个结果。直接尝试加入第一个Lobby: {lobbyToJoin}");
            SteamMatchmaking.JoinLobby(lobbyToJoin);
        }
        else
        {
            Debug.Log("[SteamLobbyManager] 过滤器未返回任何Lobby，将创建一个新的。");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 2);
        }
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
        else
        {
            Debug.LogError($"[SteamLobbyManager] Lobby 创建失败! Result: {callback.m_eResult}");
        }
    }

    private void OnLobbyEnterCallback(LobbyEnter_t callback)
    {
        CurrentLobbyId = new CSteamID(callback.m_ulSteamIDLobby);
        int memberCount = SteamMatchmaking.GetNumLobbyMembers(CurrentLobbyId);
        int maxMembers = SteamMatchmaking.GetLobbyMemberLimit(CurrentLobbyId);

        // **关键日志: 进入Lobby后，我们现在应该能获取到完整的房主信息了**
        CSteamID ownerId = SteamMatchmaking.GetLobbyOwner(CurrentLobbyId);
        string ownerName = "[未知]";
        if (ownerId.IsValid() && ownerId.m_SteamID != 0)
        {
            ownerName = SteamFriends.GetFriendPersonaName(ownerId);
        }
        Debug.Log($"[SteamLobbyManager] 成功进入Lobby: {CurrentLobbyId}. 房主是: {ownerName}. 当前人数: {memberCount}/{maxMembers}");


        OnLobbyEntered?.Invoke(CurrentLobbyId);

        if (memberCount >= maxMembers)
        {
            Debug.Log("[SteamLobbyManager] Lobby已满员，比赛即将开始！");

            if (SteamMatchmaking.GetLobbyOwner(CurrentLobbyId) == SteamUser.GetSteamID())
            {
                SteamMatchmaking.SetLobbyJoinable(CurrentLobbyId, false);
                Debug.Log("[SteamLobbyManager] 我是房主，已将Lobby设为不可加入。");
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

    // 以下部分(Player & Avatar, Helper Methods)保持不变，无需修改
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