// File: _Scripts/UI/GameUIManager.cs

using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    // --- 在Unity编辑器中拖拽赋值 ---
    [Header("Prefabs & Parents")]
    [SerializeField] private GameObject energyBarPrefab; // 能量条UI预制件
    [SerializeField] private RectTransform leftPanel;    // 左侧/下方UI容器
    [SerializeField] private RectTransform rightPanel;   // 右侧/上方UI容器

    // --- 引用 ---
    private EnergySystem energySystem;
    private EnergyBarSegmentsUI redEnergyBar; 
    private EnergyBarSegmentsUI blackEnergyBar; 

    void Start()
    {
        // 【新增调试日志】
        Debug.Log($"GameUIManager starting... Selected Mode is: {GameModeSelector.SelectedMode}");

        if (GameModeSelector.SelectedMode != GameModeType.RealTime)
        {
            // 【新增调试日志】
            Debug.Log("Not in RealTime mode. Disabling UI Manager.");

            if (leftPanel != null) leftPanel.gameObject.SetActive(false);
            if (rightPanel != null) rightPanel.gameObject.SetActive(false);
            this.enabled = false;
            return;
        }

        if (GameManager.Instance != null && GameManager.Instance.EnergySystem != null)
        {
            // 【新增调试日志】
            Debug.Log("GameManager and EnergySystem are ready. Setting up UI...");

            energySystem = GameManager.Instance.EnergySystem;
            SetupUI();
            AdaptUILayout();
        }
        else
        {
            // 【新增调试日志】
            Debug.LogError($"GameUIManager Error: RealTime mode selected, but something is missing. GameManager.Instance is null? {(GameManager.Instance == null)}. EnergySystem is null? {(GameManager.Instance?.EnergySystem == null)}");

            if (leftPanel != null) leftPanel.gameObject.SetActive(false);
            if (rightPanel != null) rightPanel.gameObject.SetActive(false);
            this.enabled = false;
        }
    }

    private void SetupUI()
    {
        // 为红方（通常是本地玩家，放在左/下）创建能量条
        GameObject redBarGO = Instantiate(energyBarPrefab, leftPanel);
        redEnergyBar = redBarGO.GetComponent<EnergyBarSegmentsUI>();

        // 为黑方（通常是对手，放在右/上）创建能量条
        GameObject blackBarGO = Instantiate(energyBarPrefab, rightPanel);
        blackEnergyBar = blackBarGO.GetComponent<EnergyBarSegmentsUI>();
        // 可以在这里对黑方的能量条做一些视觉区分，比如旋转180度
        blackBarGO.transform.localRotation = Quaternion.Euler(0, 0, 180);
    }

    private void AdaptUILayout()
    {
        // 检查屏幕是竖屏还是横屏
        if ((float)Screen.height / Screen.width > 1.0f) // 高度大于宽度，视为竖屏
        {
            Debug.Log("竖屏模式，调整UI布局为上下结构。");
            // 将LeftPanel锚点设置到下中
            leftPanel.anchorMin = new Vector2(0.5f, 0);
            leftPanel.anchorMax = new Vector2(0.5f, 0);
            leftPanel.pivot = new Vector2(0.5f, 0);
            leftPanel.anchoredPosition = new Vector2(0, 50); // 稍微向上偏移一点

            // 将RightPanel锚点设置到上中
            rightPanel.anchorMin = new Vector2(0.5f, 1);
            rightPanel.anchorMax = new Vector2(0.5f, 1);
            rightPanel.pivot = new Vector2(0.5f, 1);
            rightPanel.anchoredPosition = new Vector2(0, -50); // 稍微向下偏移一点
        }
        // 横屏布局我们已经在编辑器里设置好了，所以不需要额外代码
    }


    void Update()
    {
        if (energySystem == null || redEnergyBar == null || blackEnergyBar == null) return;

        // 持续更新能量条的显示
        redEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Red), 4.0f);
        blackEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Black), 4.0f);
    }

}