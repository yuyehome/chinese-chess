using UnityEngine;
using FishNet;
using TMPro; // 需要引入TextMeshPro

/// <summary>
/// 游戏内主UI的管理器。
/// 负责根据游戏模式和屏幕朝向，动态地创建和布局UI元素，如能量条、玩家信息块等。
/// </summary>
public class GameUIManager : MonoBehaviour
{
    [Header("UI Prefabs")]
    [SerializeField] private GameObject energyBarPrefab;

    [Header("UI Layout Containers")]
    [SerializeField] private RectTransform myInfoBlock;
    [SerializeField] private RectTransform enemyInfoBlock;

    // --- 新增：对信息块内部元素的具体引用 ---
    [Header("Player Info UI Elements")]
    [SerializeField] private TextMeshProUGUI myNameText;
    [SerializeField] private TextMeshProUGUI enemyNameText;
    [SerializeField] private Transform myEnergyBarContainer;
    [SerializeField] private Transform enemyEnergyBarContainer;

    // --- 内部引用 ---
    private EnergySystem energySystem;
    private EnergyBarSegmentsUI myEnergyBar;
    private EnergyBarSegmentsUI enemyEnergyBar;

    private PlayerColor myColor = PlayerColor.None;
    private PlayerColor enemyColor = PlayerColor.None;
    private bool isInitialized = false;

    /// <summary>
    /// 初始化UI管理器。由GameSetupController在正确时机调用。
    /// </summary>
    /// <param name="localPlayerColor">本地玩家被分配的颜色</param>
    public void Initialize(PlayerColor localPlayerColor)
    {
        if (isInitialized) return;

        this.myColor = localPlayerColor;
        this.enemyColor = (myColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        // 确保GameManager已经准备好
        if (GameManager.Instance != null)
        {
            // 不再需要检查EnergySystem，因为它现在是无状态的
            if (GameModeSelector.SelectedMode == GameModeType.RealTime || InstanceFinder.IsClient)
            {
                AdaptUILayout();
                SetupUIElements();
                isInitialized = true;
            }
        }
        else
        {
            Debug.LogError("[UI] GameUIManager初始化失败：GameManager或其EnergySystem尚未准备好。");
            gameObject.SetActive(false);
        }
    }


    private void Update()
    {
        // 如果未初始化或游戏结束，则不执行任何操作
        if (!isInitialized || GameManager.Instance.IsGameEnded) return;


        // 从GameManager的SyncVar中读取能量值来更新UI
        float myEnergy = (myColor == PlayerColor.Red)
            ? GameManager.Instance.RedPlayerEnergy.Value
            : GameManager.Instance.BlackPlayerEnergy.Value;

        float enemyEnergy = (enemyColor == PlayerColor.Red)
            ? GameManager.Instance.RedPlayerEnergy.Value
            : GameManager.Instance.BlackPlayerEnergy.Value;

        // 每帧更新能量条的显示
        myEnergyBar.UpdateEnergy(myEnergy, 4.0f);
        enemyEnergyBar.UpdateEnergy(enemyEnergy, 4.0f);

    }

    /// <summary>
    /// 在指定的容器内实例化能量条UI，并设置玩家名称。
    /// </summary>
    private void SetupUIElements()
    {
        if (energyBarPrefab == null)
        {
            Debug.LogError("[UI] EnergyBar Prefab 未在 GameUIManager 中指定！");
            return;
        }

        // 创建能量条
        GameObject myBarGO = Instantiate(energyBarPrefab, myEnergyBarContainer);
        myEnergyBar = myBarGO.GetComponent<EnergyBarSegmentsUI>();

        GameObject enemyBarGO = Instantiate(energyBarPrefab, enemyEnergyBarContainer);
        enemyEnergyBar = enemyBarGO.GetComponent<EnergyBarSegmentsUI>();

        // 设置玩家名称
        // PVE模式
        if (!InstanceFinder.IsClient && !InstanceFinder.IsServer)
        {
            myNameText.text = "玩家";
            enemyNameText.text = "电脑";
        }
        else // PVP模式
        {
            // 在PVP模式下，我们需要从GameNetworkManager获取玩家数据来显示名字
            var gnm = GameNetworkManager.Instance;
            if (gnm != null)
            {
                // 遍历所有玩家数据来找到自己和对手
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
    /// 检查屏幕朝向，并动态调整UI布局以适应竖屏或横屏。
    /// </summary>
    private void AdaptUILayout()
    {
        if ((float)Screen.height / Screen.width > 1.0f) // 竖屏
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
    /// 响应UI按钮点击退出游戏。
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

        // 这里可以改为返回主菜单
        // UnityEngine.SceneManagement.SceneManager.LoadScene("MainMenu");

#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}