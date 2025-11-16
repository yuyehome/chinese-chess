// 文件路径: Assets/Scripts/_Core/Networking/NetworkServiceProvider.cs

using UnityEngine;

/// <summary>
/// 服务定位器，为上层逻辑提供INetworkService的全局唯一实例。
/// 通过一个静态开关决定是在线模式还是离线模式。
/// </summary>
public class NetworkServiceProvider : MonoBehaviour
{
    /// <summary>
    /// 在游戏启动的最开始阶段设置此值，以决定使用哪个网络服务。
    /// true = MirrorService, false = OfflineService.
    /// </summary>
    public static bool IsOnlineMode { get; set; } = false;

    [Header("服务 Prefabs")]
    [Tooltip("将包含 MirrorService 组件的 Prefab 拖拽至此")]
    [SerializeField] private GameObject mirrorServicePrefab;

    // OfflineService 是纯C#类，不需要Prefab

    private static INetworkService _instance;
    public static INetworkService Instance
    {
        get
        {
            if (_instance == null)
            {
                // 注意：在某些极端的编辑器启动情况下，Awake可能尚未执行。
                // 如果持续出现NullRef，可能需要一个更复杂的懒汉式初始化。
                // 但通常设置好Script Execution Order后，这里不应该为空。
                Debug.LogError("[NetworkServiceProvider] 实例尚未被创建或获取失败！请检查脚本执行顺序和Prefab配置。");
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null)
        {
            Debug.LogWarning("[NetworkServiceProvider] 场景中存在多个实例，将销毁此实例。");
            Destroy(gameObject);
            return;
        }

        if (IsOnlineMode)
        {
            if (mirrorServicePrefab != null)
            {
                GameObject serviceObject = Instantiate(mirrorServicePrefab);
                _instance = serviceObject.GetComponent<INetworkService>();
                if (_instance == null)
                {
                    Debug.LogError("[NetworkServiceProvider] 'mirrorServicePrefab' 上没有找到实现了 INetworkService 接口的组件！");
                }
                else
                {
                    Debug.Log("[NetworkServiceProvider] 在线模式已激活，已实例化 [MirrorService]。");
                }
            }
            else
            {
                Debug.LogError("[NetworkServiceProvider] 'mirrorServicePrefab' 字段为空！请在Inspector中进行配置。");
            }
        }
        else // Offline Mode
        {
            // 对于纯C#类，我们直接创建实例
            _instance = new OfflineService();
            Debug.Log("[NetworkServiceProvider] 离线模式已激活，已创建 [OfflineService] 实例。");
            // 因为它不是GameObject，所以不需要DontDestroyOnLoad
        }
    }
}