// 文件路径: Assets/Scripts/_App/GameManagement/MatchmakingController.cs

using UnityEngine;

public class MatchmakingController : MonoBehaviour
{
    private void Start()
    {
        // 确保单例已创建
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnMatchReady += HandleMatchReady;
        }
    }

    private void HandleMatchReady()
    {
        Debug.Log("[MatchmakingController] 收到比赛就绪信号！正在切换UI...");

        // 从主线程调用UI操作，以防事件从其他线程触发
        UnityMainThreadDispatcher.Instance().Enqueue(() => {
            UIManager.Instance.HidePanel<MatchmakingStatusPanel>();
            UIManager.Instance.HidePanel<MainMenuPanel>();
            UIManager.Instance.ShowPanel<RoomPanel>();
        });
    }

    private void OnDestroy()
    {
        if (SteamLobbyManager.Instance != null)
        {
            SteamLobbyManager.Instance.OnMatchReady -= HandleMatchReady;
        }
    }
}

// 注意: 上面的代码用到了一个UnityMainThreadDispatcher。
// 这是一个非常有用的小工具，可以确保代码在主线程执行。
// 如果你项目中没有，请创建一个新脚本并使用以下代码：
// 文件路径: Assets/Scripts/_Core/Foundation/UnityMainThreadDispatcher.cs
/*
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnityMainThreadDispatcher : MonoBehaviour {
    private static readonly Queue<Action> _executionQueue = new Queue<Action>();
    private static UnityMainThreadDispatcher _instance = null;

    public static UnityMainThreadDispatcher Instance() {
        if (_instance == null) {
            _instance = FindObjectOfType<UnityMainThreadDispatcher>();
            if (_instance == null) {
                var go = new GameObject("[UnityMainThreadDispatcher]");
                _instance = go.AddComponent<UnityMainThreadDispatcher>();
                DontDestroyOnLoad(go);
            }
        }
        return _instance;
    }

    public void Enqueue(IEnumerator action) {
        lock (_executionQueue) {
            _executionQueue.Enqueue(() => {
                StartCoroutine(action);
            });
        }
    }

    public void Enqueue(Action action) {
        Enqueue(ActionWrapper(action));
    }

    private IEnumerator ActionWrapper(Action a) {
        a();
        yield return null;
    }

    private void Update() {
        lock (_executionQueue) {
            while (_executionQueue.Count > 0) {
                _executionQueue.Dequeue().Invoke();
            }
        }
    }
}
*/