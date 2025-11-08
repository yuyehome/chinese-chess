// 文件路径: Assets/Scripts/_App/UI/SimpleNetworkUI.cs

using UnityEngine;
using Mirror;

public class SimpleNetworkUI : MonoBehaviour
{
    private INetworkService _networkService;
    private string _networkAddress = "localhost";

    private void Start()
    {
        // 获取网络服务实例
        _networkService = NetworkServiceProvider.Instance;
        if (_networkService == null)
        {
            Debug.LogError("SimpleNetworkUI: 未能找到INetworkService实例！");
            this.enabled = false;
        }
    }

    private void OnGUI()
    {
        // 如果还未连接，显示连接选项
        if (!_networkService.IsConnected)
        {
            DrawConnectionOptions();
        }
        // 如果已经连接，显示状态和断开按钮
        else
        {
            DrawStatus();
        }
    }

    private void DrawConnectionOptions()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));

        // Host 按钮
        if (GUILayout.Button("Host (Server + Client)"))
        {
            _networkService.StartHost();
        }

        // 地址输入框和 Client 按钮
        GUILayout.BeginHorizontal();
        _networkAddress = GUILayout.TextField(_networkAddress);
        if (GUILayout.Button("Client"))
        {
            _networkService.StartClient(_networkAddress);
        }
        GUILayout.EndHorizontal();

        GUILayout.EndArea();
    }

    private void DrawStatus()
    {
        GUILayout.BeginArea(new Rect(10, 10, 300, 100));

        if (_networkService.IsHost)
        {
            GUILayout.Label("Status: Hosting");
        }
        else if (_networkService.IsClient)
        {
            GUILayout.Label($"Status: Connected to {_networkAddress}");
        }

        if (GUILayout.Button("Disconnect"))
        {
            _networkService.Disconnect();
        }

        GUILayout.EndArea();
    }
}