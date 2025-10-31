// File: _Scripts/UI/PingDisplay.cs

using UnityEngine;
using TMPro; // ���ڿ��� TextMeshPro ���
using FishNet; // ����FishNet�����ռ���ʹ��InstanceFinder

/// <summary>
/// ������UI����ʾ�ͻ��˵��������������ӳ�(Ping)��
/// </summary>
public class PingDisplay : MonoBehaviour
{
    [Tooltip("������ʾPingֵ��TextMeshPro�ı����")]
    [SerializeField] private TextMeshProUGUI pingText;

    [Tooltip("ÿ�����������һ��Pingֵ��ʾ")]
    [SerializeField] private float updateRate = 1.0f;

    private float _updateTimer;

    private void Start()
    {
        // ��ʼ��飬���δ����UI�ı�������ô˽ű��Է�ֹUpdate�б���
        if (pingText == null)
        {
            Debug.LogWarning("PingDisplay: pingText δ��ָ�����ű��ѽ��á�", this);
            this.enabled = false;
            return;
        }

        // Host/��������Ping��������0������ֻ�ڿͻ�������ʾ�������Pingֵ
        // ������ǿͻ��ˣ���������δ���ӣ�����ʾ�κ�����
        if (!InstanceFinder.IsClient)
        {
            pingText.gameObject.SetActive(false);
        }
    }

    private void Update()
    {
        // ֻ����Ϊ�ͻ�������ʱ��ִ�и���
        if (!InstanceFinder.IsClient)
        {
            return;
        }

        _updateTimer += Time.deltaTime;

        // �ﵽ����Ƶ��
        if (_updateTimer >= updateRate)
        {
            _updateTimer = 0f;

            // ��FishNet��TimeManager��ȡ����ʱ��(RTT)����λ�Ѿ��Ǻ���
            double roundTripTime = InstanceFinder.TimeManager.RoundTripTime;

            // Pingͨ����ָ�����ӳ٣���RTT��һ��
            // roundTripTime���ص���double���ͣ�����תΪint����ʾ
            int ping = (int)(roundTripTime / 2.0);

            // ����UI�ı�
            pingText.text = $"Ping: {ping} ms";
        }
    }
}