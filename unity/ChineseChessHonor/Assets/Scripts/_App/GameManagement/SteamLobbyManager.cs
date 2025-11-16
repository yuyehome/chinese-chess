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

    #region C# Events
    public event Action<CSteamID> OnAvatarReady;
    public event Action<CSteamID> OnLobbyCreatedSuccess; // 参数改为 CSteamID
    public event Action<CSteamID> OnLobbyEntered;      // 参数改为 CSteamID
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
                    Debug.LogError("[SteamLobbyManager] Steamworks API 初始化失败！请确保Steam客户端正在运行，并且项目中存在 steam_appid.txt 文件。");
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
        if (CurrentLobbyId != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyId);
            CurrentLobbyId = CSteamID.Nil;
        }

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

        SteamMatchmaking.AddRequestLobbyListStringFilter("Mode", "1v1", ELobbyComparison.k_ELobbyComparisonEqual);
        SteamMatchmaking.AddRequestLobbyListFilterSlotsAvailable(1);
        SteamAPICall_t handle = SteamMatchmaking.RequestLobbyList();
        m_LobbyMatchListCallResult.Set(handle);
    }

    public void LeaveLobby()
    {
        if (CurrentLobbyId != CSteamID.Nil)
        {
            SteamMatchmaking.LeaveLobby(CurrentLobbyId);
            CurrentLobbyId = CSteamID.Nil;
            Debug.Log("[SteamLobbyManager] 已离开Lobby。");
        }
    }

    #endregion

    #region Steam Callback Handlers

    private void OnLobbyMatchListCallback(LobbyMatchList_t callback, bool ioFailure)
    {
        if (ioFailure || callback.m_nLobbiesMatching == 0)
        {
            Debug.Log("[SteamLobbyManager] 未找到符合条件的Lobby，将创建一个新的。");
            SteamMatchmaking.CreateLobby(ELobbyType.k_ELobbyTypePublic, 2); // 1v1，最大成员2
        }
        else
        {
            Debug.Log($"[SteamLobbyManager] 找到了 {callback.m_nLobbiesMatching} 个Lobby，尝试加入第一个。");
            CSteamID lobbyId = SteamMatchmaking.GetLobbyByIndex(0);
            SteamMatchmaking.JoinLobby(lobbyId);
        }
    }

    private void OnLobbyCreatedCallback(LobbyCreated_t callback)
    {
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            CSteamID lobbyId = new CSteamID(callback.m_ulSteamIDLobby);
            Debug.Log($"[SteamLobbyManager] Lobby 创建成功! Lobby ID: {lobbyId}");

            // 为Lobby设置元数据
            SteamMatchmaking.SetLobbyData(lobbyId, "Mode", "1v1");

            OnLobbyCreatedSuccess?.Invoke(lobbyId);
            // OnLobbyEnter 会在之后被自动调用
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