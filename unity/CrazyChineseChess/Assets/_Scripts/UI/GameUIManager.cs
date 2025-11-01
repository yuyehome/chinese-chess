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
        Debug.Log($"[GameUIManager] Start调用，当前游戏模式: {GameModeSelector.SelectedMode}");
        Debug.Log($"[GameUIManager] 初始启用状态: {this.enabled}");

        // 先禁用自身，等待网络就绪
        this.enabled = false;

        // 直接开始等待协程，不要用StartCoroutine(WaitForNetworkReady())
        StartCoroutine(WaitForNetworkReady());
    }

    private System.Collections.IEnumerator WaitForNetworkReady()
    {
        Debug.Log("[GameUIManager] 开始等待网络就绪");

        // 等待GameManager就绪
        while (GameManager.Instance == null)
        {
            Debug.Log("[GameUIManager] 等待GameManager...");
            yield return null;
        }

        // 等待GameNetworkManager就绪
        while (GameNetworkManager.Instance == null)
        {
            Debug.Log("[GameUIManager] 等待GameNetworkManager...");
            yield return null;
        }

        // 等待本地玩家数据就绪
        while (GameNetworkManager.Instance.LocalPlayerData.PlayerName == null)
        {
            Debug.Log("[GameUIManager] 等待LocalPlayerData...");
            yield return null;
        }

        Debug.Log("[GameUIManager] 网络就绪，开始初始化UI");

        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            AdaptUILayout();
            SetupUI();
            this.enabled = true; // 关键：重新启用组件！
        }

        Debug.Log($"[GameUIManager] 组件启用状态: {this.enabled}");
    }

    private void Update()
    {
        if (myEnergyBar == null || enemyEnergyBar == null)
        {
            Debug.LogWarning($"[EnergyUI] 能量条未初始化: my={myEnergyBar == null}, enemy={enemyEnergyBar == null}");
            return;
        }

        // 详细的网络状态检查
        if (GameNetworkManager.Instance == null)
        {
            Debug.LogWarning("[EnergyUI] GameNetworkManager.Instance 为 null");
            return;
        }

        var localPlayerData = GameNetworkManager.Instance.LocalPlayerData;
        if (localPlayerData.PlayerName == null)
        {
            Debug.LogWarning("[EnergyUI] LocalPlayerData 未就绪");
            return;
        }

        float myEnergy = GameManager.Instance.GetEnergy(localPlayerData.Color);
        PlayerColor enemyColor = (localPlayerData.Color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        float enemyEnergy = GameManager.Instance.GetEnergy(enemyColor);

        Debug.Log($"[EnergyUI] 本地玩家: {localPlayerData.Color}, 我方能量: {myEnergy}, 敌方能量: {enemyEnergy}");

        myEnergyBar.UpdateEnergy(myEnergy, 4.0f);
        enemyEnergyBar.UpdateEnergy(enemyEnergy, 4.0f);
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

        Debug.Log($"[UI] 开始创建能量条，预制体: {energyBarPrefab.name}");
        Debug.Log($"[UI] 我方容器: {myEnergyBarContainer != null}, 敌方容器: {enemyEnergyBarContainer != null}");

        // 为我方创建能量条
        if (myEnergyBarContainer != null)
        {
            GameObject myBarGO = Instantiate(energyBarPrefab, myEnergyBarContainer);
            Debug.Log($"[UI] 我方能量条实例化: {myBarGO != null}, 位置: {myBarGO.transform.position}");

            myEnergyBar = myBarGO.GetComponent<EnergyBarSegmentsUI>();
            Debug.Log($"[UI] 我方能量条脚本: {myEnergyBar != null}");

            if (myEnergyBar != null)
            {
                // 立即设置一个测试值
                myEnergyBar.UpdateEnergy(2.5f, 4.0f);
            }
        }

        // 为敌方创建能量条
        if (enemyEnergyBarContainer != null)
        {
            GameObject enemyBarGO = Instantiate(energyBarPrefab, enemyEnergyBarContainer);
            Debug.Log($"[UI] 敌方能量条实例化: {enemyBarGO != null}, 位置: {enemyBarGO.transform.position}");

            enemyEnergyBar = enemyBarGO.GetComponent<EnergyBarSegmentsUI>();
            Debug.Log($"[UI] 敌方能量条脚本: {enemyEnergyBar != null}");

            if (enemyEnergyBar != null)
            {
                enemyEnergyBar.UpdateEnergy(3.0f, 4.0f);
            }
        }

        Debug.Log($"[UI] 能量条创建完成: 我方={myEnergyBar != null}, 敌方={enemyEnergyBar != null}");
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