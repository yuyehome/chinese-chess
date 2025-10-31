// File: _Scripts/UI/SyncTestUI.cs

using UnityEngine;
using TMPro;

public class SyncTestUI : MonoBehaviour
{
    public TextMeshProUGUI timeDisplayText;

    private void Update()
    {
        // =============================【修改开始】=============================
        // 确保UI和GameNetworkManager实例都已准备好
        if (timeDisplayText == null || GameNetworkManager.Instance == null)
        {
            // 如果GameNetworkManager还未生成，显示等待信息
            if (timeDisplayText != null) timeDisplayText.text = "Waiting for Network Manager...";
            return;
        }

        // 从 GameNetworkManager.Instance 中读取同步过来的 ServerTime 的值
        timeDisplayText.text = $"时间同步: {GameNetworkManager.Instance.ServerTime.Value:F2}";
        // =============================【修改结束】=============================
    }
}