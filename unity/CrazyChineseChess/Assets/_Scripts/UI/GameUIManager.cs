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

    void Start()
    {
        Debug.Log($"[GameUIManager] Start���ã���ǰ��Ϸģʽ: {GameModeSelector.SelectedMode}");
        Debug.Log($"[GameUIManager] ��ʼ����״̬: {this.enabled}");

        // �Ƚ��������ȴ��������
        this.enabled = false;

        // ֱ�ӿ�ʼ�ȴ�Э�̣���Ҫ��StartCoroutine(WaitForNetworkReady())
        StartCoroutine(WaitForNetworkReady());
    }

    private System.Collections.IEnumerator WaitForNetworkReady()
    {
        Debug.Log("[GameUIManager] ��ʼ�ȴ��������");

        // �ȴ�GameManager����
        while (GameManager.Instance == null)
        {
            Debug.Log("[GameUIManager] �ȴ�GameManager...");
            yield return null;
        }

        // �ȴ�GameNetworkManager����
        while (GameNetworkManager.Instance == null)
        {
            Debug.Log("[GameUIManager] �ȴ�GameNetworkManager...");
            yield return null;
        }

        // �ȴ�����������ݾ���
        while (GameNetworkManager.Instance.LocalPlayerData.PlayerName == null)
        {
            Debug.Log("[GameUIManager] �ȴ�LocalPlayerData...");
            yield return null;
        }

        Debug.Log("[GameUIManager] �����������ʼ��ʼ��UI");

        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            AdaptUILayout();
            SetupUI();
            this.enabled = true; // �ؼ����������������
        }

        Debug.Log($"[GameUIManager] �������״̬: {this.enabled}");
    }

    private void Update()
    {
        if (myEnergyBar == null || enemyEnergyBar == null)
        {
            Debug.LogWarning($"[EnergyUI] ������δ��ʼ��: my={myEnergyBar == null}, enemy={enemyEnergyBar == null}");
            return;
        }

        // ��ϸ������״̬���
        if (GameNetworkManager.Instance == null)
        {
            Debug.LogWarning("[EnergyUI] GameNetworkManager.Instance Ϊ null");
            return;
        }

        var localPlayerData = GameNetworkManager.Instance.LocalPlayerData;
        if (localPlayerData.PlayerName == null)
        {
            Debug.LogWarning("[EnergyUI] LocalPlayerData δ����");
            return;
        }

        float myEnergy = GameManager.Instance.GetEnergy(localPlayerData.Color);
        PlayerColor enemyColor = (localPlayerData.Color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        float enemyEnergy = GameManager.Instance.GetEnergy(enemyColor);

        Debug.Log($"[EnergyUI] �������: {localPlayerData.Color}, �ҷ�����: {myEnergy}, �з�����: {enemyEnergy}");

        myEnergyBar.UpdateEnergy(myEnergy, 4.0f);
        enemyEnergyBar.UpdateEnergy(enemyEnergy, 4.0f);
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

        Debug.Log($"[UI] ��ʼ������������Ԥ����: {energyBarPrefab.name}");
        Debug.Log($"[UI] �ҷ�����: {myEnergyBarContainer != null}, �з�����: {enemyEnergyBarContainer != null}");

        // Ϊ�ҷ�����������
        if (myEnergyBarContainer != null)
        {
            GameObject myBarGO = Instantiate(energyBarPrefab, myEnergyBarContainer);
            Debug.Log($"[UI] �ҷ�������ʵ����: {myBarGO != null}, λ��: {myBarGO.transform.position}");

            myEnergyBar = myBarGO.GetComponent<EnergyBarSegmentsUI>();
            Debug.Log($"[UI] �ҷ��������ű�: {myEnergyBar != null}");

            if (myEnergyBar != null)
            {
                // ��������һ������ֵ
                myEnergyBar.UpdateEnergy(2.5f, 4.0f);
            }
        }

        // Ϊ�з�����������
        if (enemyEnergyBarContainer != null)
        {
            GameObject enemyBarGO = Instantiate(energyBarPrefab, enemyEnergyBarContainer);
            Debug.Log($"[UI] �з�������ʵ����: {enemyBarGO != null}, λ��: {enemyBarGO.transform.position}");

            enemyEnergyBar = enemyBarGO.GetComponent<EnergyBarSegmentsUI>();
            Debug.Log($"[UI] �з��������ű�: {enemyEnergyBar != null}");

            if (enemyEnergyBar != null)
            {
                enemyEnergyBar.UpdateEnergy(3.0f, 4.0f);
            }
        }

        Debug.Log($"[UI] �������������: �ҷ�={myEnergyBar != null}, �з�={enemyEnergyBar != null}");
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