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

    private void Start()
    {
        GameNetworkManager.OnLocalPlayerDataReceived += HandleLocalPlayerDataReceived;
    }

    private void OnDestroy()
    {
        GameNetworkManager.OnLocalPlayerDataReceived -= HandleLocalPlayerDataReceived;
        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.Instance.AllPlayers.OnChange -= HandleAllPlayersDataChanged;
        }
    }

    private void HandleLocalPlayerDataReceived(PlayerNetData localPlayerData)
    {
        _localPlayerColor = localPlayerData.Color;
        InstantiateHUDs();
        GameNetworkManager.Instance.AllPlayers.OnChange += HandleAllPlayersDataChanged;
        RefreshHUDs();
    }

    private void InstantiateHUDs()
    {
        if (playerInfoDisplayPrefab == null)
        {
            Debug.LogError("[PlayerHUDManager] PlayerInfoDisplay Prefab 未指定!");
            return;
        }

        GameObject myInfoGO = Instantiate(playerInfoDisplayPrefab, myInfoAnchor);
        _myInfoDisplay = myInfoGO.GetComponent<PlayerInfoDisplay>();

        GameObject enemyInfoGO = Instantiate(playerInfoDisplayPrefab, enemyInfoAnchor);
        _enemyInfoDisplay = enemyInfoGO.GetComponent<PlayerInfoDisplay>();
    }

    /// <summary>
    /// 这是 SyncDictionary.OnChange 事件的正确委托签名。
    /// 它只包含一个 value 参数，而不是 oldItem 和 newItem。
    /// </summary>
    private void HandleAllPlayersDataChanged(SyncDictionaryOperation op, int key, PlayerNetData value, bool asServer)
    {
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