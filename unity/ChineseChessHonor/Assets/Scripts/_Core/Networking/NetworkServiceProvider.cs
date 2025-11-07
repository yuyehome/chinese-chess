// 文件路径: Assets/Scripts/_Core/Networking/NetworkServiceProvider.cs

using UnityEngine;

public static class NetworkServiceProvider
{
    public static INetworkService Instance { get; private set; }

    // 在游戏启动最开始时被调用
    public static void Initialize(INetworkService serviceInstance)
    {
        Instance = serviceInstance;
    }

    // (可选) 提供一个清理方法
    public static void Shutdown()
    {
        Instance = null;
    }
}