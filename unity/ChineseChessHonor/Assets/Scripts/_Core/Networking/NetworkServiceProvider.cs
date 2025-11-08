// 文件路径: Assets/Scripts/_Core/Networking/NetworkServiceProvider.cs

using UnityEngine;

public static class NetworkServiceProvider
{
    private static INetworkService _instance;

    public static INetworkService Instance
    {
        get
        {
            if (_instance == null)
            {
                // 查找场景中的MirrorService实例
                _instance = Object.FindObjectOfType<MirrorService>();
                if (_instance == null)
                {
                    Debug.LogError("场景中找不到 INetworkService 的实现 (例如 MirrorService)!");
                }
            }
            return _instance;
        }
    }
}