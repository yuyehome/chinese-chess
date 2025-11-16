// 文件路径: Assets/Scripts/_App/GameManagement/SteamLobbyManager.cs

using Steamworks;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

// 使用别名简化代码
using Lobby = Steamworks.Data.Lobby;
using Friend = Steamworks.Data.Friend;

public class SteamLobbyManager : PersistentSingleton<SteamLobbyManager>
{
    private bool _isSteamInitialized = false;

    // 用于缓存已加载的Steam头像
    private Dictionary<CSteamID, Texture2D> _avatarCache = new Dictionary<CSteamID, Texture2D>();

    // 当前所在的Lobby
    public Lobby? CurrentLobby { get; private set; }

    #region C# Events
    // 供UI或其他系统订阅
    public event Action<CSteamID> OnAvatarReady;
    public event Action<Lobby> OnLobbyCreated;
    public event Action<Lobby> OnLobbyEntered;
    public event Action OnMatchReady; // 当Lobby满员时触发
    #endregion

    #region Steam Callbacks & CallResults
    // Steam API 回调处理器
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

                    // 注册回调
                    m_AvatarImageLoaded = Callback<AvatarImageLoaded_t>.Create(OnAvatarImageLoaded);
                    m_LobbyEnter = Callback<LobbyEnter_t>.Create(OnLobbyEnter);
                    m_LobbyCreated = Callback<LobbyCreated_t>.Create(OnLobbyCreated);
                    m_LobbyMatchListCallResult = CallResult<LobbyMatchList_t>.Create(OnLobbyMatchList);
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
        if (CurrentLobby.HasValue)
        {
            CurrentLobby.Value.Leave();
            CurrentLobby = null;
        }

        if (_isSteamInitialized)
        {
            SteamAPI.Shutdown();
            Debug.Log("[SteamLobbyManager] Steamworks API 已关闭。");
        }
    }

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

    #region Public Methods - Lobby Management

    // 这是外部调用的主入口
    public async void FindOrCreateLobby()
    {
        if (!_isSteamInitialized)
        {
            Debug.LogError("[SteamLobbyManager] Steam未初始化，无法寻找Lobby。");
            return;
        }

        Debug.Log("[SteamLobbyManager] 开始寻找或创建Lobby...");

        // 1. 设置搜索条件
        var request = SteamMatchmaking.RequestLobbyList();
        request.WithMaxResults(10); // 最多返回10个结果
        request.WithQuery("Mode", "1v1"); // 匹配我们在创建时设置的元数据
        request.FilterSlotsAvailable(1); // 至少有1个空位

        // 2. 发起异步请求
        m_LobbyMatchListCallResult.Set(await request.Request());
    }

    #endregion

    #region Steam Callback Handlers

    // 当Lobby搜索结果返回时调用
    private async void OnLobbyMatchList(LobbyMatchList_t callback, bool ioFailure)
    {
        if (ioFailure || callback.m_nLobbiesMatching == 0)
        {
            Debug.Log("[SteamLobbyManager] 未找到符合条件的Lobby，将创建一个新的。");
            // 没有找到房间，创建新的
            await SteamMatchmaking.CreateLobbyAsync(2); // 1v1，所以最大成员为2
        }
        else
        {
            Debug.Log($"[SteamLobbyManager] 找到了 {callback.m_nLobbiesMatching} 个Lobby，尝试加入第一个。");
            // 找到了房间，加入第一个
            var lobbyId = SteamMatchmaking.GetLobbyByIndex(0);
            await lobbyId.Join();
        }
    }

    // 当自己成功创建一个Lobby后调用
    private void OnLobbyCreated(LobbyCreated_t callback)
    {
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            Debug.Log($"[SteamLobbyManager] Lobby 创建成功! Lobby ID: {callback.m_ulSteamIDLobby}");
            var lobby = new Lobby(callback.m_ulSteamIDLobby);

            // 为Lobby设置元数据，以便其他玩家可以搜索到
            lobby.SetData("Mode", "1v1");
            lobby.SetPublic(); // 设置为公开
            lobby.SetJoinable(true);

            // OnLobbyEntered 会在之后被自动调用，所以我们在这里不需要额外处理
        }
        else
        {
            Debug.LogError($"[SteamLobbyManager] Lobby 创建失败! Result: {callback.m_eResult}");
        }
    }

    // 当自己成功进入一个Lobby后调用 (无论是创建还是加入)
    private void OnLobbyEnter(LobbyEnter_t callback)
    {
        CurrentLobby = new Lobby(callback.m_ulSteamIDLobby);
        Debug.Log($"[SteamLobbyManager] 成功进入Lobby: {CurrentLobby.Value.Id}. 当前人数: {CurrentLobby.Value.MemberCount}/{CurrentLobby.Value.MaxMembers}");

        OnLobbyEntered?.Invoke(CurrentLobby.Value);

        // 检查Lobby是否已满
        if (CurrentLobby.Value.IsFull)
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

    // 一个辅助函数，将Steam的图像数据转换为Unity的Texture2D
    private Texture2D GetSteamImageAsTexture2D(int iImage)
    {
        if (!SteamUtils.GetImageSize(iImage, out uint width, out uint height))
        {
            Debug.LogError($"[SteamLobbyManager] 无法获取图像尺寸, ImageID: {iImage}");
            return null;
        }

        byte[] imageData = new byte[width * height * 4];

        if (!SteamUtils.GetImageRGBA(iImage, imageData, (int)(width * height * 4)))
        {
            Debug.LogError($"[SteamLobbyManager] 无法获取图像RGBA数据, ImageID: {iImage}");
            return null;
        }

        // 创建最终要使用的Texture
        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);

        // **核心修复逻辑**
        // 直接将图像数据加载到Texture中。此时它是上下颠倒的。
        texture.LoadRawTextureData(imageData);

        // 获取颠倒的像素数组
        Color32[] flippedPixels = texture.GetPixels32();

        // 创建一个正确顺序的像素数组
        Color32[] correctPixels = new Color32[flippedPixels.Length];

        // 手动进行垂直翻转
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                // 将 (x, y) 的像素从原始数组的 (x, height - 1 - y) 位置复制过来
                correctPixels[y * width + x] = flippedPixels[(height - 1 - y) * width + x];
            }
        }

        // 将翻转后的正确像素数组设置回Texture
        texture.SetPixels32(correctPixels);
        texture.Apply(); // 应用更改

        return texture;
    }
    #endregion

}