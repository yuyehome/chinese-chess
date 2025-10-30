using UnityEngine;
using FishNet;

/// <summary>
/// 游戏场景的引导者和设置控制器 (Orchestrator)。
/// 它的唯一职责是根据当前是PVP还是PVE模式，正确地初始化游戏环境，
/// 包括创建玩家控制器、设置AI、调整相机等。
/// 完成初始化后，它便不再活动。
/// </summary>
public class GameSetupController : MonoBehaviour
{

    private void Start()
    {
        bool isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;

        if (isPVPMode)
        {
            Debug.Log("[GameSetup] 检测到PVP模式，等待网络玩家数据...");
            // 订阅事件
            GameNetworkManager.OnLocalPlayerDataReceived += InitializeLocalPlayerForPVP;

            // 除了订阅，我们还要检查一下数据是不是已经到了
            // 如果 GameNetworkManager 的实例存在，并且已经缓存了本地玩家数据
            if (GameNetworkManager.Instance != null && GameNetworkManager.Instance.LocalPlayerData.Color != PlayerColor.None)
            {
                Debug.Log("[GameSetup] 检测到玩家数据已提前到达，立即执行初始化！");
                // 直接用已经存在的数据进行初始化
                InitializeLocalPlayerForPVP(GameNetworkManager.Instance.LocalPlayerData);
            }
        }
        else
        {
            Debug.Log("[GameSetup] 检测到PVE模式，立即初始化单机对战...");
            InitializeForPVE();
        }
    }


    private void OnDestroy()
    {
        // 确保在对象销毁时取消订阅，防止内存泄漏
        GameNetworkManager.OnLocalPlayerDataReceived -= InitializeLocalPlayerForPVP;
    }

    /// <summary>
    /// [PVP-Callback] 当从服务器接收到本地玩家的数据后，此方法被调用。
    /// </summary>
    private void InitializeLocalPlayerForPVP(PlayerNetData localPlayerData)
    {
        // 安全锁：确保这个初始化逻辑只执行一次
        if (this.enabled == false) return;

        Debug.Log($"[GameSetup] 收到网络玩家数据，开始为 {localPlayerData.Color} 方设置本地输入和相机...");

        // 移除可能存在的旧组件，确保我们是从一个干净的状态开始
        if (TryGetComponent<PlayerInputController>(out var oldController))
        {
            Destroy(oldController);
        }
        // 总是添加一个新的、干净的控制器组件
        PlayerInputController playerController = gameObject.AddComponent<PlayerInputController>();

        // 初始化输入控制器，这会激活它
        playerController.Initialize(localPlayerData.Color, GameManager.Instance);
        Debug.Log($"[GameSetup] PlayerInputController 已为 {localPlayerData.Color} 方初始化。");

        // 如果分配到的是黑方，旋转相机
        if (localPlayerData.Color == PlayerColor.Black)
        {
            Debug.Log("[GameSetup] 本地玩家为黑方，正在调整相机视角。");
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 0.6f, 0.15f);
                mainCamera.transform.rotation = Quaternion.Euler(80f, 180f, 0f);
            }
        }

        // 设置完成，禁用自身，因为它的使命已经完成了
        this.enabled = false;
        Debug.Log("[GameSetup] 初始化完成，GameSetupController 已禁用。");
    }

    private void InitializeForPVE()
    {
        // 移除可能存在的旧组件
        if (TryGetComponent<PlayerInputController>(out var oldPlayerController)) Destroy(oldPlayerController);
        if (TryGetComponent<TurnBasedInputController>(out var oldTurnBasedController)) Destroy(oldTurnBasedController);
        if (TryGetComponent<AIController>(out var oldAIController)) Destroy(oldAIController);

        // 根据游戏模式创建合适的控制器
        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            // PVE实时模式下，玩家固定为红方
            PlayerInputController playerController = gameObject.AddComponent<PlayerInputController>();
            playerController.Initialize(PlayerColor.Red, GameManager.Instance);

            // 根据难度选择创建AI
            IAIStrategy aiStrategy;
            switch (GameModeSelector.SelectedAIDifficulty)
            {
                case AIDifficulty.VeryHard: aiStrategy = new VeryHardAIStrategy(); break;
                case AIDifficulty.Hard: aiStrategy = new HardAIStrategy(); break;
                case AIDifficulty.Easy: default: aiStrategy = new EasyAIStrategy(); break;
            }
            AIController aiController = gameObject.AddComponent<AIController>();
            aiController.Initialize(PlayerColor.Black, GameManager.Instance);
            aiController.SetupAI(aiStrategy);
        }
        else if (GameModeSelector.SelectedMode == GameModeType.TurnBased)
        {
            // PVE回合制模式
            TurnBasedInputController turnBasedInput = gameObject.AddComponent<TurnBasedInputController>();
            turnBasedInput.Initialize(PlayerColor.Red, GameManager.Instance);
            // 这里可以根据需要添加回合制AI
        }
        // PVE设置完成，禁用自身
        this.enabled = false;
    }

}