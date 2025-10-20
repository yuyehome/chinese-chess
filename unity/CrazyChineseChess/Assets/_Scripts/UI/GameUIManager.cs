// File: _Scripts/UI/GameUIManager.cs

using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    // --- ��Unity�༭������ק��ֵ ---
    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject energyBarPrefab; // ������UIԤ�Ƽ�
    [SerializeField] private RectTransform leftPanel;    // ���/�·�UI����
    [SerializeField] private RectTransform rightPanel;   // �Ҳ�/�Ϸ�UI����

    // --- ���� ---
    private EnergySystem energySystem;
    private EnergyBarSegmentsUI redEnergyBar; 
    private EnergyBarSegmentsUI blackEnergyBar; 

    void Start()
    {
        // ������������־��
        Debug.Log($"GameUIManager starting... Selected Mode is: {GameModeSelector.SelectedMode}");

        if (GameModeSelector.SelectedMode != GameModeType.RealTime)
        {
            // ������������־��
            Debug.Log("Not in RealTime mode. Disabling UI Manager.");

            if (leftPanel != null) leftPanel.gameObject.SetActive(false);
            if (rightPanel != null) rightPanel.gameObject.SetActive(false);
            this.enabled = false;
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.EnergySystem != null)
        {
            // ������������־��
            Debug.Log("GameManager and EnergySystem are ready. Setting up UI...");

            energySystem = GameManager.Instance.EnergySystem;
            SetupUI();
            AdaptUILayout();
        }
        else
        {
            // ������������־��
            Debug.LogError($"GameUIManager Error: RealTime mode selected, but something is missing. GameManager.Instance is null? {(GameManager.Instance == null)}. EnergySystem is null? {(GameManager.Instance?.EnergySystem == null)}");

            if (leftPanel != null) leftPanel.gameObject.SetActive(false);
            if (rightPanel != null) rightPanel.gameObject.SetActive(false);
            this.enabled = false;
        }
    }

    private void SetupUI()
    {
        // Ϊ�췽��ͨ���Ǳ�����ң�������/�£�����������
        GameObject redBarGO = Instantiate(energyBarPrefab, leftPanel);
        redEnergyBar = redBarGO.GetComponent<EnergyBarSegmentsUI>();

        // Ϊ�ڷ���ͨ���Ƕ��֣�������/�ϣ�����������
        GameObject blackBarGO = Instantiate(energyBarPrefab, rightPanel);
        blackEnergyBar = blackBarGO.GetComponent<EnergyBarSegmentsUI>();
        // ����������Ժڷ�����������һЩ�Ӿ����֣�������ת180��
        blackBarGO.transform.localRotation = Quaternion.Euler(0, 0, 180);
    }

    private void AdaptUILayout()
    {
        // �����Ļ���������Ǻ���
        if ((float)Screen.height / Screen.width > 1.0f) // �߶ȴ��ڿ�ȣ���Ϊ����
        {
            Debug.Log("����ģʽ������UI����Ϊ���½ṹ��");
            // ��LeftPanelê�����õ�����
            leftPanel.anchorMin = new Vector2(0.5f, 0);
            leftPanel.anchorMax = new Vector2(0.5f, 0);
            leftPanel.pivot = new Vector2(0.5f, 0);
            leftPanel.anchoredPosition = new Vector2(0, 50); // ��΢����ƫ��һ��

            // ��RightPanelê�����õ�����
            rightPanel.anchorMin = new Vector2(0.5f, 1);
            rightPanel.anchorMax = new Vector2(0.5f, 1);
            rightPanel.pivot = new Vector2(0.5f, 1);
            rightPanel.anchoredPosition = new Vector2(0, -50); // ��΢����ƫ��һ��
        }
        // �������������Ѿ��ڱ༭�������ú��ˣ����Բ���Ҫ�������
    }


    void Update()
    {
        if (energySystem == null || redEnergyBar == null || blackEnergyBar == null) return;

        // ������������������ʾ
        redEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Red), 4.0f);
        blackEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Black), 4.0f);
    }

}