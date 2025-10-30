// File: _Scripts/UI/GameUIManager.cs

using UnityEngine;
using FishNet;  

/// <summary>
/// ��Ϸ����UI�Ĺ�������
/// ���������Ϸģʽ����Ļ���򣬶�̬�ش����Ͳ���UIԪ�أ����������������Ϣ��ȡ�
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("UI Prefabs")]
    [Tooltip("������UI��Ԥ�Ƽ�")]
    [SerializeField] private GameObject energyBarPrefab;

    [Header("UI Layout Containers")]
    [Tooltip("�ҷ���Ϣ��ĸ��������ڲ��ֶ�λ")]
    [SerializeField] private RectTransform myInfoBlock;
    [Tooltip("�з���Ϣ��ĸ��������ڲ��ֶ�λ")]
    [SerializeField] private RectTransform enemyInfoBlock;

    [Header("UI Element Parents")]
    [Tooltip("�ҷ�����������ʵ�������ľ���λ��")]
    [SerializeField] private Transform myEnergyBarContainer;
    [Tooltip("�з�����������ʵ�������ľ���λ��")]
    [SerializeField] private Transform enemyEnergyBarContainer;

    // --- �ڲ����� ---
    private EnergySystem energySystem;
    private EnergyBarSegmentsUI myEnergyBar;
    private EnergyBarSegmentsUI enemyEnergyBar;

    private PlayerColor _localPlayerColor = PlayerColor.None;
    private PlayerColor _enemyPlayerColor = PlayerColor.None;
    private bool _isNetworkInitialized = false; // ������ݳ�ʼ����־


    void Start()
    {
        // ȷ��GameManager�������ϵͳ��׼������
        if (GameManager.Instance != null && GameManager.Instance.EnergySystem != null)
        {
            energySystem = GameManager.Instance.EnergySystem;

            // ����ʵʱģʽ�²���Ҫ�����������UI
            if (GameModeSelector.SelectedMode == GameModeType.RealTime)
            {
                AdaptUILayout(); // ����1: �ȸ�����Ļ������������������λ��
                SetupUI();       // ����2: �ڵ����õ������ڴ���UIԪ��
                // ����Ƿ�������ģʽ
                bool isPVPMode = InstanceFinder.IsClient || InstanceFinder.IsServer;
                if (isPVPMode)
                {
                    // ������ģʽ�£������¼��Եȴ��������������
                    Debug.Log("[GameUIManager] ��⵽PVPģʽ�����ڶ���������ȷ���¼�...");
                    GameNetworkManager.OnLocalPlayerDataReceived += HandleLocalPlayerDataReceived;
                }
                else
                {
                    // �ڵ���ģʽ�£�����Ӳ�������
                    Debug.Log("[GameUIManager] ��⵽����ģʽ������Ĭ�������ݣ��췽����");
                    _localPlayerColor = PlayerColor.Red;
                    _enemyPlayerColor = PlayerColor.Black;
                }
            }
        }
        else
        {
            // �������ʵʱģʽ��GameManager�쳣������ô�UI������
            gameObject.SetActive(false);
        }
    }

    private void OnDestroy()
    {
        // �л��������������ʱ�����ȡ������
        GameNetworkManager.OnLocalPlayerDataReceived -= HandleLocalPlayerDataReceived;
    }

    /// <summary>
    /// ����GameNetworkManager���յ�������ҵ�����ʱ���˻ص���������
    /// </summary>
    private void HandleLocalPlayerDataReceived(PlayerNetData localPlayerData)
    {
        Debug.Log($"[GameUIManager] ���ȷ�ϣ���������� {localPlayerData.Color} ����");
        _localPlayerColor = localPlayerData.Color;
        _enemyPlayerColor = (_localPlayerColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        _isNetworkInitialized = true; // �����������ѳ�ʼ��
    }

    private void Update()
    {
        // ��飺�������ϵͳ��UIԪ��δ׼���ã���ִ���κβ���
        if (energySystem == null || myEnergyBar == null || enemyEnergyBar == null) return;

        // ��飺������δȷ�����򲻸���
        // �ڵ���ģʽ�£�_localPlayerColor �� Start() �оͱ���ֵ�ˣ���������жϲ�������
        // ������ģʽ�£�����жϻ�ȵ� HandleLocalPlayerDataReceived ִ�к��ͨ��
        if (_localPlayerColor == PlayerColor.None) return;

        // ÿ֡������ȷ�ϵ���ݣ���̬��������������ʾ
        myEnergyBar.UpdateEnergy(energySystem.GetEnergy(_localPlayerColor), 4.0f);
        enemyEnergyBar.UpdateEnergy(energySystem.GetEnergy(_enemyPlayerColor), 4.0f);
    }

    /// <summary>
    /// ��ָ����������ʵ����������UI��
    /// </summary>
    private void SetupUI()
    {
        if (energyBarPrefab == null)
        {
            Debug.LogError("[UI] EnergyBar Prefab δ�� GameUIManager ��ָ����");
            return;
        }

        // Ϊ�ҷ�(�췽)����������
        GameObject myBarGO = Instantiate(energyBarPrefab, myEnergyBarContainer);
        myEnergyBar = myBarGO.GetComponent<EnergyBarSegmentsUI>();

        // Ϊ�з�(�ڷ�)����������
        GameObject enemyBarGO = Instantiate(energyBarPrefab, enemyEnergyBarContainer);
        enemyEnergyBar = enemyBarGO.GetComponent<EnergyBarSegmentsUI>();
    }

    /// <summary>
    /// �����Ļ���򣬲���̬����UI��������Ӧ�����������
    /// </summary>
    private void AdaptUILayout()
    {
        // �ж��Ƿ�Ϊ���� (�߶ȴ��ڿ��)
        if ((float)Screen.height / Screen.width > 1.0f)
        {
            Debug.Log("[UI] ��⵽����ģʽ������UI����Ϊ���½ṹ��");

            // --- �����ҷ���Ϣ�鵽��Ļ���� ---
            myInfoBlock.anchorMin = new Vector2(0.5f, 0);   // ê��(��,��)
            myInfoBlock.anchorMax = new Vector2(0.5f, 0);   // ê��(��,��)
            myInfoBlock.pivot = new Vector2(0.5f, 0);       // ���ĵ�
            myInfoBlock.anchoredPosition = new Vector2(0, 20); // ��ê���ƫ�ƣ�����20����

            // --- �����з���Ϣ�鵽��Ļ���� ---
            enemyInfoBlock.anchorMin = new Vector2(0.5f, 1);
            enemyInfoBlock.anchorMax = new Vector2(0.5f, 1);
            enemyInfoBlock.pivot = new Vector2(0.5f, 1);
            enemyInfoBlock.anchoredPosition = new Vector2(0, -20); // ����20����
        }
        // ����Ǻ�������UI�ᱣ�����ڱ༭����ͨ��ê�����õ�Ĭ�ϲ��֣���������Ԥ��
    }

    /// <summary>
    /// ����������������ӦUI��ť�ĵ���¼����˳���Ϸ��
    /// </summary>
    public void OnClick_ExitGame()
    {
        Debug.Log("[GameUIManager] ��ҵ�����˳���Ϸ��ť��");

        // ��������Ϸ�У��˳����Ǽ򵥵عرճ��򣬶���Ҫ�ȶϿ��������ӡ�
        // FishNet��InstanceFinder���Է�����ҵ�NetworkManagerʵ����
        if (InstanceFinder.IsHost)
        {
            // �������������Ҫͬʱ�رշ������Ϳͻ��ˡ�
            Debug.Log("�������ڹر�����...");
            InstanceFinder.ServerManager.StopConnection(true);
            InstanceFinder.ClientManager.StopConnection();
        }
        else if (InstanceFinder.IsClient)
        {
            // ���ֻ�ǿͻ��ˣ�ֻ��رտͻ������ӡ�
            Debug.Log("�ͻ������ڶϿ�����...");
            InstanceFinder.ClientManager.StopConnection();
        }

        // ������߼�������չ�����緵�����˵�����
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");

        // ���ֻ�Ǽ򵥵عر���Ϸ����
        // ע�⣺����Unity�༭���в������ã�ֻ�ڹ���������Ϸ����Ч��
        #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }


}