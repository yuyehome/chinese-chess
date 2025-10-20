// File: _Scripts/UI/EnergyBarSegmentsUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // ��Ҫʹ��List

/// <summary>
/// ���Ʒֶ�ʽ������UI�Ľű���
/// ͨ�����ƶ��Image�������������ʵ��������ʾ��
/// </summary>
public class EnergyBarSegmentsUI : MonoBehaviour
{
    [SerializeField] private List<Image> energySegments; // ��Inspector�а�˳������4����ɫ������
    [SerializeField] private TextMeshProUGUI energyText;

    /// <summary>
    /// ���ݵ�ǰ������ֵ��������������������ʾ��
    /// </summary>
    /// <param name="currentEnergy">��ǰ��ȷ������ֵ������ 3.5</param>
    /// <param name="maxEnergy">�������ֵ������ 4.0</param>
    public void UpdateEnergy(float currentEnergy, float maxEnergy)
    {
        int fullSegments = Mathf.FloorToInt(currentEnergy); // ��������������
        float remainder = currentEnergy - fullSegments;     // ���ڻָ���С������

        // ��������
        energyText.text = fullSegments.ToString();

        // ��������������ͼƬ
        for (int i = 0; i < energySegments.Count; i++)
        {
            Image segment = energySegments[i];
            if (segment == null) continue;

            // --- �����߼� ---
            if (i < fullSegments)
            {
                // 1. �����ѳ����ĸ�ֱ����ʾ����
                segment.gameObject.SetActive(true);
                segment.fillAmount = 1f;
            }
            else if (i == fullSegments)
            {
                // 2. �������ڻָ�����һ��
                if (remainder > 0.01f) // ֻ�е��лָ�����ʱ����ʾ
                {
                    segment.gameObject.SetActive(true);
                    segment.fillAmount = remainder; // ����С���������������
                }
                else
                {
                    segment.gameObject.SetActive(false); // ���û�лָ����ȣ�������
                }
            }
            else
            {
                // 3. ����δ���Ŀո���ȫ����
                segment.gameObject.SetActive(false);
            }
        }
    }

    // ע�⣺����·���������ҪOnEnergySpent��������ΪUpdateEnergy�Ѿ��ܴ������������
}