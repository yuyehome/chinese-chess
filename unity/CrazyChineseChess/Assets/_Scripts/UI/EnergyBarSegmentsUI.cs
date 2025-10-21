// File: _Scripts/UI/EnergyBarSegmentsUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// ���Ʒֶ�ʽ������UI�Ľű���
/// ͨ�����ƶ��Image�Ӷ�������������(fillAmount)��ʵ�������Ķ�̬��ʾ��
/// </summary>
public class EnergyBarSegmentsUI : MonoBehaviour
{
    [Tooltip("��Inspector�а�˳����������������Image���(���磬�����һ���µ���)")]
    [SerializeField] private List<Image> energySegments;
    [Tooltip("������ʾ��ǰ��������ֵ��TextMeshProUGUI���")]
    [SerializeField] private TextMeshProUGUI energyText;

    /// <summary>
    /// ���ݵ�ǰ�ľ�ȷ����ֵ�������������������Ӿ����֡�
    /// </summary>
    /// <param name="currentEnergy">��ǰ��ȷ������ֵ (���� 3.5)</param>
    /// <param name="maxEnergy">�������ֵ (���� 4.0)</param>
    public void UpdateEnergy(float currentEnergy, float maxEnergy)
    {
        int fullSegments = Mathf.FloorToInt(currentEnergy); // �ѳ�������������
        float remainder = currentEnergy - fullSegments;     // ���ڻָ���С������ (0.0 to 1.0)

        // ����������ʾ
        if (energyText != null)
        {
            energyText.text = fullSegments.ToString();
        }

        // ��������������Image����������ֵ������״̬
        for (int i = 0; i < energySegments.Count; i++)
        {
            Image segment = energySegments[i];
            if (segment == null) continue;

            if (i < fullSegments)
            {
                // �����ѳ����ĸ�����Ϊ�ɼ�����ȫ���
                segment.gameObject.SetActive(true);
                segment.fillAmount = 1f;
            }
            else if (i == fullSegments)
            {
                // �������ڻָ�����һ��
                if (remainder > 0.01f) // ֻ�е��лָ�����ʱ����ʾ��������˸
                {
                    segment.gameObject.SetActive(true);
                    segment.fillAmount = remainder; // ����С���������������������ƽ����������
                }
                else
                {
                    segment.gameObject.SetActive(false); // ���û�лָ����ȣ�������
                }
            }
            else
            {
                // ����δ���Ŀո���ȫ����
                segment.gameObject.SetActive(false);
            }
        }
    }
}