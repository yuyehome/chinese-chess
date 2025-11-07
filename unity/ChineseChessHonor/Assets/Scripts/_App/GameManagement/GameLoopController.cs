// 文件路径: Assets/Scripts/_App/GameManagement/GameLoopController.cs

using UnityEngine;

public class GameLoopController : PersistentSingleton<GameLoopController>
{
    [Header("场景引用")]
    [SerializeField] private BoardView boardView;
    // InputController现在直接通过NetworkServiceProvider发送指令，不再需要这里的引用

    void Start()
    {
        // 订阅网络服务事件
        if (NetworkServiceProvider.Instance != null)
        {
            NetworkServiceProvider.Instance.OnGameStateUpdated += OnNetworkGameStateUpdated;
        }
    }

    // 允许InputController等通过网络服务发送指令
    public void RequestProcessCommand(ICommand command)
    {
        NetworkServiceProvider.Instance?.SendCommand(command);
    }

    // 当接收到来自网络层（无论是Offline还是FishNet）的权威状态时
    private void OnNetworkGameStateUpdated(GameState newState)
    {
        // 直接将这个权威状态交给BoardView去渲染
        boardView.OnGameStateUpdated(newState);
    }

    protected override void Awake()
    {
        base.Awake();
        // GameLoopController 不再负责初始化游戏逻辑，
        // 这个职责移交给了 OfflineService (单机) 或 服务器 (联机)
    }

    private void OnDestroy()
    {
        if (NetworkServiceProvider.Instance != null)
        {
            NetworkServiceProvider.Instance.OnGameStateUpdated -= OnNetworkGameStateUpdated;
        }
    }
}