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

    private const string LOBBY_MODE_KEY = "Mode";
    private const string LOBBY_MODE_VALUE = "1v1";

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

        // 添加搜索过滤器
        SteamMatchmaking.AddRequestLobbyListStringFilter(LOBBY_MODE_KEY, LOBBY_MODE_VALUE, ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);

        // 发起请求
        SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
        m_LobbyMatchListCallResult.Set(handle);
        Debug.Log("[SteamLobbyManager] Lobby列表请求已发送。");
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

    // 新增: 供调试使用的公共方法
    public void Debug_RequestLobbyList()
    {
        Debug.Log("[DEBUG] 手动发起Lobby列表请求（无过滤条件）...");
        // 不加任何过滤条件，搜索所有公开的Lobby
        SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
        m_LobbyMatchListCallResult.Set(handle);
    }

    #endregion

    #region Steam Callback Handlers

    private void OnLobbyMatchListCallback(LobbyMatchList_t callback, bool ioFailure)
    {
        Debug.Log($"[SteamLobbyManager] OnLobbyMatchListCallback: 收到回调。IO Failure: {ioFailure}, 匹配到的Lobby数量: {callback.m_nLobbiesMatching}");

        // --- 增加详细的调试日志 ---
        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            string mode = SteamMatchmaking.GetLobbyData(lobbyId, LOBBY_MODE_KEY);
            string ownerName = SteamFriends.GetFriendPersonaName(SteamMatchmaking.GetLobbyOwner(lobbyId));
            Debug.Log($"  - 找到的Lobby[{i}]: ID={lobbyId}, Owner={ownerName}, Mode='{mode}'");
        }
        // --- 调试日志结束 ---

        if (ioFailure)
        {
            Debug.LogError("[SteamLobbyManager] 搜索Lobby时发生IO错误。");
            return;
        }

        // 尝试找到一个完全匹配的Lobby
        CSteamID suitableLobbyId = CSteamID.Nil;
        for (int i = 0; i < callback.m_nLobbiesMatching; i++)
        {
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(i);
            // 再次确认元数据匹配，防止Steam的过滤器有延迟
            if (SteamMatchmaking.GetLobbyData(lobbyId, LOBBY_MODE_KEY) == LOBBY_MODE_VALUE)
            {
                suitableLobbyId = lobbyId;
                break;
            }
        }

        if (suitableLobbyId != CSteamID.Nil)
        {
            Debug.Log($"[SteamLobbyManager] 在 {callback.m_nLobbiesMatching} 个结果中找到了合适的Lobby，尝试加入: {suitableLobbyId}");
            SteamMatchmaking.JoinLobby(suitableLobbyId);
        }
        else
        {
            Debug.Log("[SteamLobbyManager] 未找到完全符合条件的Lobby，将创建一个新的。");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 2);
        }
    }

    private void OnLobbyCreatedCallback(LobbyCreated_t callback)
    {
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            Debug.Log($"[SteamLobbyManager] Lobby 创建成功! Lobby ID: {lobbyId}");

            // **关键步骤: 为Lobby设置元数据和属性**
            SteamMatchmaking.SetLobbyData(lobbyId, LOBBY_MODE_KEY, LOBBY_MODE_VALUE);
            SteamMatchmaking.SetLobbyType(lobbyId, ELobbyType.k_ELobbyTypePublic); // 确保是公开的
            SteamMatchmaking.SetLobbyJoinable(lobbyId, true); // **非常重要: 允许其他玩家加入**

            Debug.Log($"[SteamLobbyManager] Lobby属性已设置: Mode='{LOBBY_MODE_VALUE}', Type=Public, Joinable=true");

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

            // **关键: 房主应该将Lobby设为不可加入，防止第三人进入**
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