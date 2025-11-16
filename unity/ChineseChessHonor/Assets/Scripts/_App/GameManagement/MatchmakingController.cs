// 文件路径: Assets/Scripts/_App/GameManagement/MatchmakingController.cs

using UnityEngine;

public class MatchmakingController : MonoBehaviour
{
    private void Start()
    {
        // 确保单例已创建，并延迟一帧订阅以避免时序问题
        StartCoroutine(DelayedSubscribe());
        Debug.Log("[MatchmakingController] 开始运行，准备订阅事件。");
    }

    private System.Collections.IEnumerator DelayedSubscribe()
    {
        yield return null; // 等待一帧，确保SteamLobbyManager.Instance可用
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnMatchReady += HandleMatchReady;
            Debug.Log("[MatchmakingController] 成功订阅 OnMatchReady 事件。");
        }
        else
        {
            Debug.LogError("[MatchmakingController] 无法找到 SteamLobbyManager.Instance！");
        }
    }

    private void HandleMatchReady()
    {
        Debug.Log("[MatchmakingController] 收到 OnMatchReady 事件！准备切换UI。");

        // 使用主线程调度器确保UI操作安全
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            Debug.Log("[MatchmakingController] 主线程：正在隐藏匹配面板和主菜单...");
            UIManager.Instance.HidePanel<MatchmakingStatusPanel>();
            UIManager.Instance.HidePanel<MainMenuPanel>();
            Debug.Log("[MatchmakingController] 主线程：正在显示房间面板...");
            UIManager.Instance.ShowPanel<RoomPanel>();
        });
    }

    private void OnDestroy()
    {
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnMatchReady -= HandleMatchReady;
            Debug.Log("[MatchmakingController] 已取消订阅 OnMatchReady 事件。");
        }
    }
}