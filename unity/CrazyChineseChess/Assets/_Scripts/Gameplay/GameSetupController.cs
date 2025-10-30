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
            // 在PVP模式下，我们不在这里做任何事。
            // 而是等待 GameNetworkManager 通过事件通知我们本地玩家的数据。
            Debug.Log("[GameSetup] 检测到PVP模式，等待网络玩家数据...");
            GameNetworkManager.OnLocalPlayerDataReceived += InitializeLocalPlayerForPVP;
        }
        else
        {
            // 在PVE（单机）模式下，我们立即进行设置。
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
        Debug.Log($"[GameSetup] 收到网络玩家数据，开始为 {localPlayerData.Color} 方设置本地输入...");

        // 获取或创建 PlayerInputController
        PlayerInputController playerController = GetComponent<PlayerInputController>();
        if (playerController == null)
        {
            playerController = gameObject.AddComponent<PlayerInputController>();
        }

        // 初始化输入控制器，这会激活它
        playerController.Initialize(localPlayerData.Color, GameManager.Instance);

        // 如果分配到的是黑方，直接设置预设的相机位置和旋转
        if (localPlayerData.Color == PlayerColor.Black)
        {
            Debug.Log("[GameSetup] 本地玩家为黑方，设置预设的相机Transform。");
            Camera mainCamera = Camera.main;
            if (mainCamera != null)
            {
                mainCamera.transform.position = new Vector3(0f, 0.6f, 0.15f);
                mainCamera.transform.rotation = Quaternion.Euler(80f, 180f, 0f);
            }
        }

        // 设置完成，可以禁用自身，因为它的使命已经完成了
        this.enabled = false;
    }

    /// <summary>
    /// [PVE-Setup] 初始化单机游戏模式。
    /// </summary>
    private void InitializeForPVE()
    {
        // PVE模式下，玩家固定为红方
        PlayerInputController playerController = GetComponent<PlayerInputController>();
        if (playerController == null) playerController = gameObject.AddComponent<PlayerInputController>();
        playerController.Initialize(PlayerColor.Red, GameManager.Instance);

        // 根据难度选择创建AI
        IAIStrategy aiStrategy;
        switch (GameModeSelector.SelectedAIDifficulty)
        {
            case AIDifficulty.VeryHard:
                aiStrategy = new VeryHardAIStrategy();
                break;
            case AIDifficulty.Hard:
                aiStrategy = new HardAIStrategy();
                break;
            case AIDifficulty.Easy:
            default:
                aiStrategy = new EasyAIStrategy();
                break;
        }
        AIController aiController = gameObject.AddComponent<AIController>();
        aiController.Initialize(PlayerColor.Black, GameManager.Instance);
        aiController.SetupAI(aiStrategy);

        // PVE设置完成，禁用自身
        this.enabled = false;
    }
}