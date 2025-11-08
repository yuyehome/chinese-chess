using UnityEngine;

public static class NetworkServiceProvider
{
    // !! 核心开关 !!
    // 在游戏启动时（例如在一个启动场景的初始化脚本中），
    // 根据是启动“单人游戏”还是“在线对战”来设置此值。
    public static bool IsOnlineMode = true; // 默认为false，方便开发测试

    private static INetworkService _instance;

    public static INetworkService Instance
    {
        get
        {
            if (_instance == null)
            {
                if (IsOnlineMode)
                {
                    // 在线模式：查找场景中的MirrorService实例
                    _instance = Object.FindObjectOfType<MirrorService>();
                    if (_instance == null)
                    {
                        Debug.LogError("在线模式启动失败: 场景中找不到 MirrorService 组件!");
                    }
                }
                else
                {
                    // 离线模式：直接创建一个新的OfflineService实例
                    _instance = new OfflineService();
                }
            }
            return _instance;
        }
    }

    // （可选）提供一个方法来在游戏生命周期中重置服务，例如返回主菜单时
    public static void Reset()
    {
        _instance = null;
    }
}