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
        Debug.Log("[PlayerHUDManager] Awake: HUD�������ѻ��ѡ�");
    }

    private void Start()
    {
        Debug.Log("[PlayerHUDManager] Start: ��ʼ��ʼ������...");

        // ��� GameNetworkManager �Ƿ��Ѿ�����
        if (GameNetworkManager.Instance != null)
        {
            // ֱ�ӳ������������ݳ�ʼ��
            TryInitialize(GameNetworkManager.Instance);
        }
        else
        {
            Debug.LogWarning("[PlayerHUDManager] Start: GameNetworkManager.Instance �в����ڡ�");
            // ������������ϲ�Ӧ�÷�������ΪGNM�����ڳ����߼��������ġ�
            // ����Ϊ���գ�����ʲôҲ�������ȴ������߼���
        }
    }

    private void OnDestroy()
    {
        // ȷ�����κ�����¶�����ȡ������
        if (GameNetworkManager.Instance != null)
        {
            GameNetworkManager.OnLocalPlayerDataReceived -= HandleLocalPlayerDataReceived;
            GameNetworkManager.Instance.AllPlayers.OnChange -= HandleAllPlayersDataChanged;
        }
    }

    // ����һ�����еĳ�ʼ������
    private void TryInitialize(GameNetworkManager gnm)
    {
        // ��׳�Լ�飺���GNM�Ƿ��Ѿ����յ��˱����������
        if (gnm.LocalPlayerData.HasValue)
        {
            // ��� A: ���������ˣ��¼��Ѿ�������ֱ���û�������ݳ�ʼ����
            Debug.Log("[PlayerHUDManager] TryInitialize: ��⵽�ѻ����������ݣ�ֱ�ӽ��г�ʼ����");
            HandleLocalPlayerDataReceived(gnm.LocalPlayerData.Value);
        }
        else
        {
            // ��� B: ���������磬�¼���û��������������
            Debug.Log("[PlayerHUDManager] TryInitialize: δ��⵽������ݣ���ʼ���� OnLocalPlayerDataReceived �¼���");
            GameNetworkManager.OnLocalPlayerDataReceived += HandleLocalPlayerDataReceived;
        }
    }

    private void HandleLocalPlayerDataReceived(PlayerNetData localPlayerData)
    {
        Debug.Log($"[PlayerHUDManager] �¼��Ѵ�����HandleLocalPlayerDataReceived �����á����������ɫ: {localPlayerData.Color}");

        // ��Ҫ��һ���������������ȡ�����ģ���ֹ�ظ�����
        GameNetworkManager.OnLocalPlayerDataReceived -= HandleLocalPlayerDataReceived;

        _localPlayerColor = localPlayerData.Color;
        InstantiateHUDs();
        GameNetworkManager.Instance.AllPlayers.OnChange += HandleAllPlayersDataChanged;

        // ���һ�µ�ǰ�ֵ����Ƿ��Ѿ��������ˣ�����У����ֶ�ˢ��һ�Ρ�
        // ����Դ����ֵ��������ڱ��ű�Start���������
        if (GameNetworkManager.Instance.AllPlayers.Count > 0)
        {
            Debug.Log("[PlayerHUDManager] ��⵽AllPlayers�������ݣ�����ִ��һ��ˢ�¡�");
            RefreshHUDs();
        }

    }

    private void InstantiateHUDs()
    {
        Debug.Log("[PlayerHUDManager] InstantiateHUDs: ���ڳ���ʵ����UI Prefabs...");

        if (playerInfoDisplayPrefab == null)
        {
            Debug.LogError("[PlayerHUDManager] PlayerInfoDisplay Prefab δָ��! �޷�ʵ������");
            return;
        }

        GameObject myInfoGO = Instantiate(playerInfoDisplayPrefab, myInfoAnchor);
        _myInfoDisplay = myInfoGO.GetComponent<PlayerInfoDisplay>();

        GameObject enemyInfoGO = Instantiate(playerInfoDisplayPrefab, enemyInfoAnchor);
        _enemyInfoDisplay = enemyInfoGO.GetComponent<PlayerInfoDisplay>();

        Debug.Log($"[PlayerHUDManager] InstantiateHUDs: UIʵ������ɡ��ҷ�UI: {myInfoGO.name}, �з�UI: {enemyInfoGO.name}");
    }

    /// <summary>
    /// ���� SyncDictionary.OnChange �¼�����ȷί��ǩ����
    /// ��ֻ����һ�� value ������������ oldItem �� newItem��
    /// </summary>
    private void HandleAllPlayersDataChanged(SyncDictionaryOperation op, int key, PlayerNetData value, bool asServer)
    {
        Debug.Log($"[PlayerHUDManager] AllPlayers.OnChange �¼�����! ����: {op}, Key: {key}, ���: {value.PlayerName}��׼��ˢ��UI...");
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

        // ��������߼�����ȷ�ģ�����Ҫ�Ķ�
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