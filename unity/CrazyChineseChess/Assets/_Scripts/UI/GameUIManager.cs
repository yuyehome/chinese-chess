// File: _Scripts/UI/GameUIManager.cs

using UnityEngine;
using FishNet; 

/// <summary>
/// 游戏内主UI的管理器。
/// 负责根据游戏模式和屏幕朝向，动态地创建和布局UI元素，如能量条、玩家信息块等。
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("UI Prefabs")]
    [Tooltip("能量条UI的预制件")]
    [SerializeField] private GameObject energyBarPrefab;

    [Header("UI Layout Containers")]
    [Tooltip("我方信息块的根对象，用于布局定位")]
    [SerializeField] private RectTransform myInfoBlock;
    [Tooltip("敌方信息块的根对象，用于布局定位")]
    [SerializeField] private RectTransform enemyInfoBlock;

    [Header("UI Element Parents")]
    [Tooltip("我方能量条将被实例化到的具体位置")]
    [SerializeField] private Transform myEnergyBarContainer;
    [Tooltip("敌方能量条将被实例化到的具体位置")]
    [SerializeField] private Transform enemyEnergyBarContainer;

    // --- 内部引用 ---
    private EnergySystem energySystem;
    private EnergyBarSegmentsUI myEnergyBar;
    private EnergyBarSegmentsUI enemyEnergyBar;

    void Start()
    {
        // 确保GameManager及其核心系统已准备就绪
        if (GameManager.Instance != null && GameManager.Instance.EnergySystem != null)
        {
            energySystem = GameManager.Instance.EnergySystem;

            // 仅在实时模式下才需要能量条等相关UI
            if (GameModeSelector.SelectedMode == GameModeType.RealTime)
            {
                AdaptUILayout(); // 步骤1: 先根据屏幕比例调整布局容器的位置
                SetupUI();       // 步骤2: 在调整好的容器内创建UI元素
            }
        }
        else
        {
            // 如果不是实时模式或GameManager异常，则禁用此UI管理器
            gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 如果UI未初始化，则不执行任何操作
        if (energySystem == null || myEnergyBar == null || enemyEnergyBar == null) return;

        // 每帧更新能量条的显示
        // 注意：当前硬编码我方为红方，敌方为黑方。未来网络对战中需根据服务器分配的角色动态决定。
        myEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Red), 4.0f);
        enemyEnergyBar.UpdateEnergy(energySystem.GetEnergy(PlayerColor.Black), 4.0f);
    }

    /// <summary>
    /// 在指定的容器内实例化能量条UI。
    /// </summary>
    private void SetupUI()
    {
        if (energyBarPrefab == null)
        {
            Debug.LogError("[UI] EnergyBar Prefab 未在 GameUIManager 中指定！");
            return;
        }

        // 为我方(红方)创建能量条
        GameObject myBarGO = Instantiate(energyBarPrefab, myEnergyBarContainer);
        myEnergyBar = myBarGO.GetComponent<EnergyBarSegmentsUI>();

        // 为敌方(黑方)创建能量条
        GameObject enemyBarGO = Instantiate(energyBarPrefab, enemyEnergyBarContainer);
        enemyEnergyBar = enemyBarGO.GetComponent<EnergyBarSegmentsUI>();
    }

    /// <summary>
    /// 检查屏幕朝向，并动态调整UI布局以适应竖屏或横屏。
    /// </summary>
    private void AdaptUILayout()
    {
        // 判断是否为竖屏 (高度大于宽度)
        if ((float)Screen.height / Screen.width > 1.0f)
        {
            Debug.Log("[UI] 检测到竖屏模式，调整UI布局为上下结构。");

            // --- 调整我方信息块到屏幕下中 ---
            myInfoBlock.anchorMin = new Vector2(0.5f, 0);   // 锚点(左,下)
            myInfoBlock.anchorMax = new Vector2(0.5f, 0);   // 锚点(右,上)
            myInfoBlock.pivot = new Vector2(0.5f, 0);       // 轴心点
            myInfoBlock.anchoredPosition = new Vector2(0, 20); // 离锚点的偏移，向上20像素

            // --- 调整敌方信息块到屏幕上中 ---
            enemyInfoBlock.anchorMin = new Vector2(0.5f, 1);
            enemyInfoBlock.anchorMax = new Vector2(0.5f, 1);
            enemyInfoBlock.pivot = new Vector2(0.5f, 1);
            enemyInfoBlock.anchoredPosition = new Vector2(0, -20); // 向下20像素
        }
        // 如果是横屏，则UI会保持其在编辑器中通过锚点设置的默认布局，无需代码干预。
    }

    /// <summary>
    /// 公共方法，用于响应UI按钮的点击事件来退出游戏。
    /// </summary>
    public void OnClick_ExitGame()
    {
        Debug.Log("[GameUIManager] 玩家点击了退出游戏按钮。");

        // 在网络游戏中，退出不是简单地关闭程序，而是要先断开网络连接。
        // FishNet的InstanceFinder可以方便地找到NetworkManager实例。
        if (InstanceFinder.IsHost)
        {
            // 如果是主机，需要同时关闭服务器和客户端。
            Debug.Log("主机正在关闭连接...");
            InstanceFinder.ServerManager.StopConnection(true);
            InstanceFinder.ClientManager.StopConnection();
        }
        else if (InstanceFinder.IsClient)
        {
            // 如果只是客户端，只需关闭客户端连接。
            Debug.Log("客户端正在断开连接...");
            InstanceFinder.ClientManager.StopConnection();
        }

        // 这里的逻辑可以扩展，比如返回主菜单场景
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");

        // 如果只是简单地关闭游戏程序：
        // 注意：这在Unity编辑器中不起作用，只在构建出的游戏中生效。
        #if UNITY_EDITOR
                    UnityEditor.EditorApplication.isPlaying = false;
        #else
                Application.Quit();
        #endif
    }


}