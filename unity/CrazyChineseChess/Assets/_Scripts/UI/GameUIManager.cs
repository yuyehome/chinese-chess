using UnityEngine;
using FishNet;
using TMPro; // ��Ҫ����TextMeshPro

/// <summary>
/// ��Ϸ����UI�Ĺ�������
/// ���������Ϸģʽ����Ļ���򣬶�̬�ش����Ͳ���UIԪ�أ����������������Ϣ��ȡ�
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private GameObject energyBarPrefab;

    [Header("UI Layout Containers")]
    [SerializeField] private RectTransform myInfoBlock;
    [SerializeField] private RectTransform enemyInfoBlock;

    // --- ����������Ϣ���ڲ�Ԫ�صľ������� ---
    [Header("Player Info UI Elements")]
    [SerializeField] private TextMeshProUGUI myNameText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private Transform myEnergyBarContainer;
    [SerializeField] private Transform enemyEnergyBarContainer;

    // --- �ڲ����� ---
    private EnergySystem energySystem;
    private EnergyBarSegmentsUI myEnergyBar;
    private EnergyBarSegmentsUI enemyEnergyBar;

    private PlayerColor myColor = PlayerColor.None;
    private PlayerColor enemyColor = PlayerColor.None;
    private bool isInitialized = false;

    /// <summary>
    /// ��ʼ��UI����������GameSetupController����ȷʱ�����á�
    /// </summary>
    /// <param name="localPlayerColor">������ұ��������ɫ</param>
    public void Initialize(PlayerColor localPlayerColor)
    {
        if (isInitialized) return;

        this.myColor = localPlayerColor;
        this.enemyColor = (myColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        // ȷ��GameManager�Ѿ�׼����
        if (GameManager.Instance != null)
        {
            // ������Ҫ���EnergySystem����Ϊ����������״̬��
            if (GameModeSelector.SelectedMode == GameModeType.RealTime || InstanceFinder.IsClient)
            {
                AdaptUILayout();
                SetupUIElements();
                isInitialized = true;
            }
        }
        else
        {
            Debug.LogError("[UI] GameUIManager��ʼ��ʧ�ܣ�GameManager����EnergySystem��δ׼���á�");
            gameObject.SetActive(false);
        }
    }


    private void Update()
    {
        // ���δ��ʼ������Ϸ��������ִ���κβ���
        if (!isInitialized || GameManager.Instance.IsGameEnded) return;


        // ��GameManager��SyncVar�ж�ȡ����ֵ������UI
        float myEnergy = (myColor == PlayerColor.Red)
            ? GameManager.Instance.RedPlayerEnergy.Value
            : GameManager.Instance.BlackPlayerEnergy.Value;

        float enemyEnergy = (enemyColor == PlayerColor.Red)
            ? GameManager.Instance.RedPlayerEnergy.Value
            : GameManager.Instance.BlackPlayerEnergy.Value;

        // ÿ֡��������������ʾ
        myEnergyBar.UpdateEnergy(myEnergy, 4.0f);
        enemyEnergyBar.UpdateEnergy(enemyEnergy, 4.0f);

    }

    /// <summary>
    /// ��ָ����������ʵ����������UI��������������ơ�
    /// </summary>
    private void SetupUIElements()
    {
        if (energyBarPrefab == null)
        {
            Debug.LogError("[UI] EnergyBar Prefab δ�� GameUIManager ��ָ����");
            return;
        }

        // ����������
        GameObject myBarGO = Instantiate(energyBarPrefab, myEnergyBarContainer);
        myEnergyBar = myBarGO.GetComponent<EnergyBarSegmentsUI>();

        GameObject enemyBarGO = Instantiate(energyBarPrefab, enemyEnergyBarContainer);
        enemyEnergyBar = enemyBarGO.GetComponent<EnergyBarSegmentsUI>();

        // �����������
        // PVEģʽ
        if (!InstanceFinder.IsClient && !InstanceFinder.IsServer)
        {
            myNameText.text = "���";
            enemyNameText.text = "����";
        }
        else // PVPģʽ
        {
            // ��PVPģʽ�£�������Ҫ��GameNetworkManager��ȡ�����������ʾ����
            var gnm = GameNetworkManager.Instance;
            if (gnm != null)
            {
                // ������������������ҵ��Լ��Ͷ���
                foreach (var player in gnm.AllPlayers.Values)
                {
                    if (player.Color == myColor)
                    {
                        myNameText.text = player.PlayerName;
                    }
                    else if (player.Color == enemyColor)
                    {
                        enemyNameText.text = player.PlayerName;
                    }
                }
            }
        }
    }

    /// <summary>
    /// �����Ļ���򣬲���̬����UI��������Ӧ�����������
    /// </summary>
    private void AdaptUILayout()
    {
        if ((float)Screen.height / Screen.width > 1.0f) // ����
        {
            myInfoBlock.anchorMin = new Vector2(0.5f, 0);
            myInfoBlock.anchorMax = new Vector2(0.5f, 0);
            myInfoBlock.pivot = new Vector2(0.5f, 0);
            myInfoBlock.anchoredPosition = new Vector2(0, 20);

            enemyInfoBlock.anchorMin = new Vector2(0.5f, 1);
            enemyInfoBlock.anchorMax = new Vector2(0.5f, 1);
            enemyInfoBlock.pivot = new Vector2(0.5f, 1);
            enemyInfoBlock.anchoredPosition = new Vector2(0, -20);
        }
    }

    /// <summary>
    /// ��ӦUI��ť����˳���Ϸ��
    /// </summary>
    public void OnClick_ExitGame()
    {
        if (InstanceFinder.IsHost)
        {
            InstanceFinder.ServerManager.StopConnection(true);
            InstanceFinder.ClientManager.StopConnection();
        }
        else if (InstanceFinder.IsClient)
        {
            InstanceFinder.ClientManager.StopConnection();
        }

        // ������Ը�Ϊ�������˵�
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}