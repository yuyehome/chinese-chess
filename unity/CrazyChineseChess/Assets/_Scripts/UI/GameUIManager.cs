// File: _Scripts/UI/GameUIManager.cs

using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject energyBarPrefab; // ������UIԤ�Ƽ�

    [Header("UI Containers")]
    [SerializeField] private RectTransform myInfoBlock;      // �ҷ���Ϣ��ĸ�����
    [SerializeField] private RectTransform enemyInfoBlock;   // �з���Ϣ��ĸ�����

    // ���޸ġ���������������Ҫ��ʵ�����ľ���λ��
    [SerializeField] private Transform myEnergyBarContainer;
    [SerializeField] private Transform enemyEnergyBarContainer;

    private EnergySystem energySystem;
    private EnergyBarSegmentsUI myEnergyBar;
    private EnergyBarSegmentsUI enemyEnergyBar;

    void Start()
    {
        if (GameManager.Instance != null && GameManager.Instance.EnergySystem != null)
        {
            energySystem = GameManager.Instance.EnergySystem;

            if (GameModeSelector.SelectedMode == GameModeType.RealTime)
            {
                AdaptUILayout(); // �ȵ�������
                SetupUI();       // �ٴ���UI
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SetupUI()
    {
        // ��ע�⡿�������Ǽ��衰�ҷ������Ǻ췽����δ�������ս�У���Ҫ���ݷ���������Ľ�ɫ��������
        // Ϊ�ҷ�(�췽)����������
        GameObject myBarGO = Instantiate(energyBarPrefab, myEnergyBarContainer);
        myEnergyBar = myBarGO.GetComponent<EnergyBarSegmentsUI>();

        // Ϊ�з�(�ڷ�)����������
        GameObject enemyBarGO = Instantiate(energyBarPrefab, enemyEnergyBarContainer);
        enemyEnergyBar = enemyBarGO.GetComponent<EnergyBarSegmentsUI>();

        // ����Ҫ��������Ҫ��ת180�ȣ�˫������һ��
    }

    private void AdaptUILayout()
    {
        // �����Ļ���������Ǻ���
        if ((float)Screen.height / Screen.width > 1.0f) // �߶ȴ��ڿ�ȣ���Ϊ����
        {
            Debug.Log("����ģʽ������UI����Ϊ���½ṹ��");

            // --- �����ҷ���Ϣ�鵽���� ---
            myInfoBlock.anchorMin = new Vector2(0.5f, 0);   // ê�����½� X, Y
            myInfoBlock.anchorMax = new Vector2(0.5f, 0);   // ê�����Ͻ� X, Y
            myInfoBlock.pivot = new Vector2(0.5f, 0);       // ����
            myInfoBlock.anchoredPosition = new Vector2(0, 20); // λ��(��ê�����)����΢����߾�

            // --- �����з���Ϣ�鵽���� ---
            enemyInfoBlock.anchorMin = new Vector2(0.5f, 1);
            enemyInfoBlock.anchorMax = new Vector2(0.5f, 1);
            enemyInfoBlock.pivot = new Vector2(0.5f, 1);
            enemyInfoBlock.anchoredPosition = new Vector2(0, -20);
        }
        // ����Ǻ������򱣳��ڱ༭�������õ����½Ǻ����Ͻǣ���������Ԥ��
    }

    void Update()
    {
        if (energySystem == null || myEnergyBar == null || enemyEnergyBar == null) return;

        // ��ע�⡿�������Ǽ��衰�ҷ������Ǻ췽�����з������Ǻڷ�
        myEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Red), 4.0f);
        enemyEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Black), 4.0f);
    }
}