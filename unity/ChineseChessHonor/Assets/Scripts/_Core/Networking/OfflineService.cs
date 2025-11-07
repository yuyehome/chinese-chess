// 文件路径: Assets/Scripts/_Core/Networking/OfflineService.cs

using System;
using UnityEngine; // For Debug.Log

public class OfflineService : INetworkService
{
    public event Action OnConnected;
    public event Action OnDisconnected;
    public event Action<GameState> OnGameStateUpdated;

    public bool IsHost => true;

    // 单机模式下，它内部持有一个完整的游戏逻辑循环
    private CommandProcessor _localCommandProcessor;
    private GameState _localGameState;
    private IGameModeLogic _localGameModeLogic;
    private float _tickTimer = 0f;

    public void StartHost()
    {
        Debug.Log("OfflineService: Starting host...");

        // 1. 创建本地游戏状态和处理器
        _localGameState = new GameState();
        _localCommandProcessor = new CommandProcessor(_localGameState);
        _localGameModeLogic = GameModeManager.CreateLogic(GameModeType.RealTime_Fair); // 暂时硬编码模式

        _localCommandProcessor.SetGameMode(_localGameModeLogic);
        _localGameModeLogic.Initialize(_localGameState);

        // 2. 订阅本地处理器的状态更新事件
        _localCommandProcessor.OnGameStateUpdated += (state) => {
            // 当本地状态更新时，触发自己的网络事件，完美模拟从“服务器”接收到状态
            OnGameStateUpdated?.Invoke(state);
        };

        // 3. 立即触发连接成功事件
        OnConnected?.Invoke();

        // 4. 启动一个模拟的游戏循环 (这里我们用一个MonoBehaviour来驱动)
        var driver = new GameObject("OfflineService_Driver").AddComponent<OfflineServiceDriver>();
        driver.TickAction = Tick;
    }

    public void StartClient(string address)
    {
        // 单机模式不支持客户端连接
        Debug.LogError("OfflineService does not support StartClient.");
    }

    public void Disconnect()
    {
        Debug.Log("OfflineService: Disconnecting.");
        OnDisconnected?.Invoke();
        // 清理资源...
    }

    public void SendCommand(ICommand command)
    {
        // 直接将指令发送给本地的处理器
        _localCommandProcessor?.ProcessCommand(command);
    }

    // 这个方法由驱动器在FixedUpdate中调用
    private void Tick()
    {
        if (_localCommandProcessor != null)
        {
            _localCommandProcessor.Tick();
        }
    }

    // 辅助类，用于提供MonoBehaviour的Update能力
    private class OfflineServiceDriver : MonoBehaviour
    {
        public Action TickAction;
        void FixedUpdate() => TickAction?.Invoke();
    }
}