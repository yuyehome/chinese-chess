// File: _Scripts/Network/SteamManager.cs

using UnityEngine;
using Steamworks; // 引入Steamworks命名空间

/// <summary>
/// 底层服务模块，负责初始化Steamworks.NET并提供基础的用户信息。
/// 这是一个全局单例，确保在整个游戏生命周期内只存在一个实例。
/// </summary>
public class SteamManager : MonoBehaviour
{
    public static SteamManager Instance { get; private set; }

    public bool IsSteamInitialized { get; private set; }
    public string PlayerName { get; private set; }
    public CSteamID PlayerSteamId { get; private set; }

    private void Awake()
    {
        // 实现单例模式，并确保在场景切换时不被销毁
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
        // 尝试初始化Steam
        try
        {
            if (SteamAPI.Init())
            {
                IsSteamInitialized = true;
                PlayerName = SteamFriends.GetPersonaName();
                PlayerSteamId = SteamUser.GetSteamID();
                Debug.Log($"[SteamManager] Steam aPI 初始化成功! 玩家: {PlayerName} ({PlayerSteamId})");
            }
            else
            {
                IsSteamInitialized = false;
                Debug.LogError("[SteamManager] Steam API 初始化失败! 请确保Steam客户端正在运行。");
            }
        }
        catch (System.Exception e)
        {
            IsSteamInitialized = false;
            Debug.LogError($"[SteamManager] Steam API 初始化时发生异常: {e.Message}");
        }
    }

    private void Update()
    {
        // Steamworks.NET 要求在Update中定期调用此函数来处理回调
        if (IsSteamInitialized)
        {
            SteamAPI.RunCallbacks();
        }
    }

    private void OnApplicationQuit()
    {
        // 游戏退出时，关闭Steam API
        if (IsSteamInitialized)
        {
            SteamAPI.Shutdown();
            Debug.Log("[SteamManager] Steam API 已关闭。");
        }
    }
}