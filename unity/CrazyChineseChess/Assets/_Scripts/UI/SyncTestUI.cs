// File: _Scripts/UI/SyncTestUI.cs

using UnityEngine;
using TMPro;

public class SyncTestUI : MonoBehaviour
{
    public TextMeshProUGUI timeDisplayText;

    private void Update()
    {
        // =============================���޸Ŀ�ʼ��=============================
        // ȷ��UI��GameNetworkManagerʵ������׼����
        if (timeDisplayText == null || GameNetworkManager.Instance == null)
        {
            // ���GameNetworkManager��δ���ɣ���ʾ�ȴ���Ϣ
            if (timeDisplayText != null) timeDisplayText.text = "Waiting for Network Manager...";
            return;
        }

        // �� GameNetworkManager.Instance �ж�ȡͬ�������� ServerTime ��ֵ
        timeDisplayText.text = $"ʱ��ͬ��: {GameNetworkManager.Instance.ServerTime.Value:F2}";
        // =============================���޸Ľ�����=============================
    }
}