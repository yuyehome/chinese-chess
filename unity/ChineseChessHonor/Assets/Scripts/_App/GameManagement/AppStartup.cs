// 文件路径: Assets/Scripts/_App/GameManagement/AppStartup.cs

using UnityEngine;
using UnityEngine.SceneManagement;

public class AppStartup : MonoBehaviour
{
    // 这个组件应放在一个不会销毁的GameObject上，或者在MainMenu场景的某个对象上
    void Start()
    {
        // 默认初始化为离线服务，以防直接从BattleScene启动进行测试
        if (NetworkServiceProvider.Instance == null)
        {
            NetworkServiceProvider.Initialize(new OfflineService());
        }
    }

    // --- UI按钮将调用这些方法 ---
    public void StartOfflineGame()
    {
        Debug.Log("Starting Offline Game...");
        NetworkServiceProvider.Initialize(new OfflineService());
        NetworkServiceProvider.Instance.OnConnected += LoadBattleScene;
        NetworkServiceProvider.Instance.StartHost();
    }

    public void StartHostGame()
    {
        Debug.Log("Starting Host Game...");
        // FishNetService需要是场景中的一个组件
        var fishNetService = FindObjectOfType<FishNetService>();
        if (fishNetService != null)
        {
            NetworkServiceProvider.Initialize(fishNetService);
            NetworkServiceProvider.Instance.OnConnected += LoadBattleScene;
            NetworkServiceProvider.Instance.StartHost();
        }
        else
        {
            Debug.LogError("场景中找不到FishNetService组件!");
        }
    }

    private void LoadBattleScene()
    {
        // 确保只订阅一次
        NetworkServiceProvider.Instance.OnConnected -= LoadBattleScene;
        SceneManager.LoadScene("BattleScene"); // 你的战斗场景名
    }
}