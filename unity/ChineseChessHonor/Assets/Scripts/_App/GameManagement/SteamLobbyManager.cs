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

    // 专门用于调试的回调处理器
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
            string ownerName = SteamFriends.GetFriendPersonaName(ownerId);
            Debug.Log($"  Owner: {ownerName} (ID: {ownerId})");

            int memberCount = SteamMatchmaking.GetNumLobbyMembers(lobbyId);
            int memberLimit = SteamMatchmaking.GetLobbyMemberLimit(lobbyId);
            Debug.Log($"  成员: {memberCount} / {memberLimit}");

            // 打印所有元数据
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

        // 遍历所有返回的Lobby，找到第一个真正可用的
        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            // 额外检查，确保这个Lobby的数据是我们期望的 (虽然过滤器已经做了，但双重检查更安全)
            if (SteamMatchmaking.GetLobbyData(lobbyId, GameConstants.SteamLobbyGameIdKey) == GameConstants.SteamLobbyGameIdValue)
            {
                Debug.Log($"[SteamLobbyManager] 在结果中找到了一个有效的Lobby (ID: {lobbyId})，尝试加入...");
                SteamMatchmaking.JoinLobby(lobbyId);
                return; // 找到就加入并退出，不再继续
            }
        }

        // 如果循环结束都没有找到或没有返回任何Lobby
        Debug.Log("[SteamLobbyManager] 未在返回结果中找到合适的Lobby，将创建一个新的。");
        SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 2); // 1v1，最大成员2
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

        Debug.Log($"[SteamLobbyManager] 成功进入Lobby: {CurrentLobbyId}. 当前人数: {memberCount}/{maxMembers}");

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