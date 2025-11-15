// 文件路径: Assets/Scripts/_App/Scenes/MainMenuController.cs

using UnityEngine;

/// <summary>
/// MainMenuScene的场景控制器。
/// 负责初始化该场景所需的服务和UI。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("测试按键")]
    [SerializeField] private KeyCode testShowRoomPanelKey = KeyCode.Alpha1;
    [SerializeField] private KeyCode testShowInvitePanelKey = KeyCode.Alpha2;

    void Start()
    {
        InitializeScene();
    }

    private void InitializeScene()
    {
        // 确保核心服务已初始化（在实际项目中可能有更复杂的依赖管理）
        // LocalizationManager.Instance. ...
        // AudioManager.Instance. ...

        // 播放主菜单背景音乐
        // AudioManager.Instance.PlayBGM("bgm_mainmenu");

        // 场景启动时，默认显示主菜单面板
        UIManager.Instance.ShowPanel<MainMenuPanel>();

        Debug.Log($"[MainMenuController] 场景初始化完成。主菜单已显示。");
        Debug.Log($"按【{testShowRoomPanelKey}】键可测试显示 RoomPanel (待创建)。");
        Debug.Log($"按【{testShowInvitePanelKey}】键可测试显示 InvitePopupPanel (待创建)。");
    }

    // --- 用于测试的 Update 循环 ---
    private void Update()
    {
        if (Input.GetKeyDown(testShowRoomPanelKey))
        {
            UIManager.Instance.ShowPanel<RoomPanel>();
        }

        // 在后续步骤中，我们会在这里添加测试其他面板的代码
        // if (Input.GetKeyDown(testShowRoomPanelKey))
        // {
        //     UIManager.Instance.ShowPanel<RoomPanel>();
        // }

        // if (Input.GetKeyDown(testShowInvitePanelKey))
        // {
        //     UIManager.Instance.ShowPanel<InvitePopupPanel>();
        // }
    }
}