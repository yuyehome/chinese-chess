// File: _Scripts/UI/PlayerHUDManager.cs

using UnityEngine;
using FishNet.Object.Synchronizing; 

public class PlayerHUDManager : MonoBehaviour
{
    [Header("HUD Prefabs & Anchors")]
    [SerializeField] private GameObject playerInfoDisplayPrefab;
    [SerializeField] private Transform myInfoAnchor;
    [SerializeField] private Transform enemyInfoAnchor;

    private PlayerInfoDisplay _myInfoDisplay;
    private PlayerInfoDisplay _enemyInfoDisplay;

    private PlayerColor? _localPlayerColor;

    private void Awake()
    {
        Debug.Log("[PlayerHUDManager] Awake: HUD管理器已唤醒。");
    }

    private void Start()
    {
        Debug.Log("[PlayerHUDManager] Start: 开始初始化流程...");

        // 检查 GameNetworkManager 是否已经存在
        if (GameNetworkManager.Instance != null)
        {
            // 直接尝试用现有数据初始化
            TryInitialize(GameNetworkManager.Instance);
        }
        else
        {
            Debug.LogWarning("[PlayerHUDManager] Start: GameNetworkManager.Instance 尚不存在。");
            // 这种情况理论上不应该发生，因为GNM是先于场景逻辑被创建的。
            // 但作为保险，我们什么也不做，等待后续逻辑。
        }
    }

    private void OnDestroy()
    {
        // 确保在任何情况下都尝试取消订阅
        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.OnLocalPlayerDataReceived -= HandleLocalPlayerDataReceived;
            GameNetworkManager.Instance.AllPlayers.OnChange -= HandleAllPlayersDataChanged;
        }
    }

    // 新增一个集中的初始化方法
    private void TryInitialize(GameNetworkManager gnm)
    {
        // 健壮性检查：检查GNM是否已经接收到了本地玩家数据
        if (gnm.LocalPlayerData.HasValue)
        {
            // 情况 A: 我们来晚了，事件已经发生。直接用缓存的数据初始化。
            Debug.Log("[PlayerHUDManager] TryInitialize: 检测到已缓存的玩家数据，直接进行初始化。");
            HandleLocalPlayerDataReceived(gnm.LocalPlayerData.Value);
        }
        else
        {
            // 情况 B: 我们来得早，事件还没发生。订阅它。
            Debug.Log("[PlayerHUDManager] TryInitialize: 未检测到玩家数据，开始订阅 OnLocalPlayerDataReceived 事件。");
            GameNetworkManager.OnLocalPlayerDataReceived += HandleLocalPlayerDataReceived;
        }
    }

    private void HandleLocalPlayerDataReceived(PlayerNetData localPlayerData)
    {
        Debug.Log($"[PlayerHUDManager] 事件已触发！HandleLocalPlayerDataReceived 被调用。本地玩家颜色: {localPlayerData.Color}");

        // 重要：一旦处理过，就立即取消订阅，防止重复调用
        GameNetworkManager.OnLocalPlayerDataReceived -= HandleLocalPlayerDataReceived;

        _localPlayerColor = localPlayerData.Color;
        InstantiateHUDs();
        GameNetworkManager.Instance.AllPlayers.OnChange += HandleAllPlayersDataChanged;

        // 检查一下当前字典里是否已经有数据了，如果有，就手动刷新一次。
        // 这可以处理“字典数据先于本脚本Start”的情况。
        if (GameNetworkManager.Instance.AllPlayers.Count > 0)
        {
            Debug.Log("[PlayerHUDManager] 检测到AllPlayers已有数据，立即执行一次刷新。");
            RefreshHUDs();
        }

    }

    private void InstantiateHUDs()
    {
        Debug.Log("[PlayerHUDManager] InstantiateHUDs: 正在尝试实例化UI Prefabs...");

        if (playerInfoDisplayPrefab == null)
        {
            Debug.LogError("[PlayerHUDManager] PlayerInfoDisplay Prefab 未指定! 无法实例化。");
            return;
        }

        GameObject myInfoGO = Instantiate(playerInfoDisplayPrefab, myInfoAnchor);
        _myInfoDisplay = myInfoGO.GetComponent<PlayerInfoDisplay>();

        GameObject enemyInfoGO = Instantiate(playerInfoDisplayPrefab, enemyInfoAnchor);
        _enemyInfoDisplay = enemyInfoGO.GetComponent<PlayerInfoDisplay>();

        Debug.Log($"[PlayerHUDManager] InstantiateHUDs: UI实例化完成。我方UI: {myInfoGO.name}, 敌方UI: {enemyInfoGO.name}");
    }

    /// <summary>
    /// 这是 SyncDictionary.OnChange 事件的正确委托签名。
    /// 它只包含一个 value 参数，而不是 oldItem 和 newItem。
    /// </summary>
    private void HandleAllPlayersDataChanged(SyncDictionaryOperation op, int key, PlayerNetData value, bool asServer)
    {
        Debug.Log($"[PlayerHUDManager] AllPlayers.OnChange 事件触发! 操作: {op}, Key: {key}, 玩家: {value.PlayerName}。准备刷新UI...");
        RefreshHUDs();
    }

    private void RefreshHUDs()
    {
        if (_myInfoDisplay == null || _enemyInfoDisplay == null || _localPlayerColor == null)
        {
            return;
        }

        PlayerNetData? myData = null;
        PlayerNetData? enemyData = null;

        // 这个遍历逻辑是正确的，不需要改动
        foreach (var playerData in GameNetworkManager.Instance.AllPlayers.Values)
        {
            if (playerData.Color == _localPlayerColor.Value)
            {
                myData = playerData;
            }
            else
            {
                enemyData = playerData;
            }
        }

        _myInfoDisplay.SetPlayerData(myData);
        _enemyInfoDisplay.SetPlayerData(enemyData);
    }

    private void Update()
    {
        if (_myInfoDisplay == null || _enemyInfoDisplay == null || _localPlayerColor == null || GameNetworkManager.Instance == null)
        {
            return;
        }

        float redEnergy = GameNetworkManager.Instance.RedPlayerSyncedEnergy.Value;
        float blackEnergy = GameNetworkManager.Instance.BlackPlayerSyncedEnergy.Value;

        const float maxEnergy = 4.0f;

        if (_localPlayerColor.Value == PlayerColor.Red)
        {
            _myInfoDisplay.UpdateEnergyDisplay(redEnergy, maxEnergy);
            _enemyInfoDisplay.UpdateEnergyDisplay(blackEnergy, maxEnergy);
        }
        else
        {
            _myInfoDisplay.UpdateEnergyDisplay(blackEnergy, maxEnergy);
            _enemyInfoDisplay.UpdateEnergyDisplay(redEnergy, maxEnergy);
        }
    }
}