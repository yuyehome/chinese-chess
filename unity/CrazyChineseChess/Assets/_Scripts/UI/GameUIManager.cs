// File: _Scripts/UI/GameUIManager.cs

using UnityEngine;

public class GameUIManager : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject energyBarPrefab; // 能量条UI预制件

    [Header("UI Containers")]
    [SerializeField] private RectTransform myInfoBlock;      // 我方信息块的根对象
    [SerializeField] private RectTransform enemyInfoBlock;   // 敌方信息块的根对象

    // 【修改】这两个是能量条要被实例化的具体位置
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
                AdaptUILayout(); // 先调整布局
                SetupUI();       // 再创建UI
            }
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void SetupUI()
    {
        // 【注意】这里我们假设“我方”总是红方。在未来网络对战中，需要根据服务器分配的角色来决定。
        // 为我方(红方)创建能量条
        GameObject myBarGO = Instantiate(energyBarPrefab, myEnergyBarContainer);
        myEnergyBar = myBarGO.GetComponent<EnergyBarSegmentsUI>();

        // 为敌方(黑方)创建能量条
        GameObject enemyBarGO = Instantiate(energyBarPrefab, enemyEnergyBarContainer);
        enemyEnergyBar = enemyBarGO.GetComponent<EnergyBarSegmentsUI>();

        // 【重要】不再需要旋转180度，双方布局一致
    }

    private void AdaptUILayout()
    {
        // 检查屏幕是竖屏还是横屏
        if ((float)Screen.height / Screen.width > 1.0f) // 高度大于宽度，视为竖屏
        {
            Debug.Log("竖屏模式，调整UI布局为上下结构。");

            // --- 调整我方信息块到下中 ---
            myInfoBlock.anchorMin = new Vector2(0.5f, 0);   // 锚点左下角 X, Y
            myInfoBlock.anchorMax = new Vector2(0.5f, 0);   // 锚点右上角 X, Y
            myInfoBlock.pivot = new Vector2(0.5f, 0);       // 轴心
            myInfoBlock.anchoredPosition = new Vector2(0, 20); // 位置(从锚点计算)，稍微给点边距

            // --- 调整敌方信息块到上中 ---
            enemyInfoBlock.anchorMin = new Vector2(0.5f, 1);
            enemyInfoBlock.anchorMax = new Vector2(0.5f, 1);
            enemyInfoBlock.pivot = new Vector2(0.5f, 1);
            enemyInfoBlock.anchoredPosition = new Vector2(0, -20);
        }
        // 如果是横屏，则保持在编辑器里设置的右下角和左上角，无需代码干预。
    }

    void Update()
    {
        if (energySystem == null || myEnergyBar == null || enemyEnergyBar == null) return;

        // 【注意】这里我们假设“我方”总是红方，“敌方”总是黑方
        myEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Red), 4.0f);
        enemyEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Black), 4.0f);
    }
}