// File: _Scripts/UI/PingDisplay.cs

using UnityEngine;
using TMPro; // 用于控制 TextMeshPro 组件
using FishNet; // 引入FishNet命名空间以使用InstanceFinder

/// <summary>
/// 负责在UI上显示客户端到服务器的网络延迟(Ping)。
/// </summary>
public class PingDisplay : MonoBehaviour
{
    [Tooltip("用于显示Ping值的TextMeshPro文本组件")]
    [SerializeField] private TextMeshProUGUI pingText;

    [Tooltip("每隔多少秒更新一次Ping值显示")]
    [SerializeField] private float updateRate = 1.0f;

    private float _updateTimer;

    private void Start()
    {
        // 初始检查，如果未设置UI文本，则禁用此脚本以防止Update中报错
        if (pingText == null)
        {
            Debug.LogWarning("PingDisplay: pingText 未被指定，脚本已禁用。", this);
            this.enabled = false;
            return;
        }

        // Host/服务器的Ping理论上是0，所以只在客户端上显示有意义的Ping值
        // 如果不是客户端，或者网络未连接，则不显示任何内容
        if (!InstanceFinder.IsClient)
        {
            pingText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 只有作为客户端连接时才执行更新
        if (!InstanceFinder.IsClient)
        {
            return;
        }

        _updateTimer += Time.deltaTime;

        // 达到更新频率
        if (_updateTimer >= updateRate)
        {
            _updateTimer = 0f;

            // 从FishNet的TimeManager获取往返时间(RTT)，单位已经是毫秒
            double roundTripTime = InstanceFinder.TimeManager.RoundTripTime;

            // Ping通常是指单向延迟，即RTT的一半
            // roundTripTime返回的是double类型，我们转为int来显示
            int ping = (int)(roundTripTime / 2.0);

            // 更新UI文本
            pingText.text = $"Ping: {ping} ms";
        }
    }
}