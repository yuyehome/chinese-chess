// 文件路径: Assets/Scripts/_App/GameManagement/SteamLobbyManager.cs

using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

// 这个类将是我们与Steamworks API交互的唯一入口
public class SteamLobbyManager : PersistentSingleton<SteamLobbyManager>
{
    private bool _isSteamInitialized = false;

    // 用于缓存已加载的Steam头像
    private Dictionary<CSteamID, Texture2D> _avatarCache = new Dictionary<CSteamID, Texture2D>();

    // C# 事件，供UI或其他系统订阅
    public event Action<CSteamID> OnAvatarReady;

    // Steam API 回调处理器
    protected Callback<AvatarImageLoaded_t> m_AvatarImageLoaded;

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
        // 必须每帧调用以处理Steam的回调
        if (_isSteamInitialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    private void OnDestroy()
    {
        if (_isSteamInitialized)
        {
            SteamAPI.Shutdown();
            Debug.Log("[SteamLobbyManager] Steamworks API 已关闭。");
        }
    }

    /// <summary>
    /// 获取当前本地玩家的个人资料
    /// </summary>
    public PlayerProfile GetLocalPlayerProfile()
    {
        if (!_isSteamInitialized) return new PlayerProfile { nickname = "Offline Player" };

        return new PlayerProfile
        {
            steamId = SteamUser.GetSteamID().m_SteamID,
            nickname = SteamFriends.GetPersonaName(),
            // 其他游戏内数据（金币、ELO等）需要从另一个系统（如PlayerDataManager）加载
            goldCoins = 9999,
            eloRating = 1500
        };
    }

    /// <summary>
    /// 异步获取指定用户的Steam头像
    /// </summary>
    /// <param name="steamId">目标用户的SteamID</param>
    /// <returns>如果头像已缓存则直接返回Texture2D，否则返回null并开始加载</returns>
    public Texture2D GetAvatar(CSteamID steamId)
    {
        if (_avatarCache.TryGetValue(steamId, out Texture2D texture))
        {
            return texture;
        }

        // Steamworks.NET 返回的 imageID 可能是-1(未加载), 0(加载中), >0(加载完成)
        int imageId = SteamFriends.GetLargeFriendAvatar(steamId);

        if (imageId > 0)
        {
            return GetSteamImageAsTexture2D(imageId);
        }

        // 如果返回-1，说明需要请求加载，Steam会通过AvatarImageLoaded_t回调通知我们
        // 如果是0，说明正在加载中，我们只需等待回调即可
        // 这个函数本身不处理等待，调用者需要订阅OnAvatarReady事件

        return null;
    }

    // 当Steam加载完一个头像时，此回调被触发
    private void OnAvatarImageLoaded(AvatarImageLoaded_t callback)
    {
        CSteamID steamId = callback.m_steamID;
        int imageId = callback.m_iImage;

        if (_avatarCache.ContainsKey(steamId)) return; // 已经处理过了

        var avatarTexture = GetSteamImageAsTexture2D(imageId);
        if (avatarTexture != null)
        {
            _avatarCache[steamId] = avatarTexture;
            OnAvatarReady?.Invoke(steamId); // 广播事件
            Debug.Log($"[SteamLobbyManager] 用户 {steamId} 的头像已加载并缓存。");
        }
    }

    // 一个辅助函数，将Steam的图像数据转换为Unity的Texture2D
    private Texture2D GetSteamImageAsTexture2D(int iImage)
    {
        if (!SteamUtils.GetImageSize(iImage, out uint width, out uint height))
        {
            return null;
        }

        byte[] imageData = new byte[width * height * 4];

        if (!SteamUtils.GetImageRGBA(iImage, imageData, (int)(width * height * 4)))
        {
            return null;
        }

        Texture2D texture = new Texture2D((int)width, (int)height, TextureFormat.RGBA32, false, true);
        texture.LoadRawTextureData(imageData);
        texture.Apply();

        // Steam返回的图像是上下颠倒的，我们需要垂直翻转它
        Color32[] pixels = texture.GetPixels32();
        Array.Reverse(pixels);
        Texture2D flippedTexture = new Texture2D((int)width, (int)height);
        flippedTexture.SetPixels32(pixels);

        // 手动翻转Y坐标
        Color[] originalPixels = flippedTexture.GetPixels();
        Color[] flippedPixels = new Color[originalPixels.Length];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                flippedPixels[y * width + x] = originalPixels[((height - 1 - y) * width) + x];
            }
        }
        flippedTexture.SetPixels(flippedPixels);
        flippedTexture.Apply();

        //Destroy(texture); // 销毁中间纹理

        return flippedTexture;
    }
}