// File: _Scripts/UI/EnergyBarSegmentsUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // 需要使用List

/// <summary>
/// 控制分段式能量条UI的脚本。
/// 通过控制多个Image的显隐和填充来实现能量显示。
/// </summary>
public class EnergyBarSegmentsUI : MonoBehaviour
{
    [SerializeField] private List<Image> energySegments; // 在Inspector中按顺序拖入4个蓝色能量格
    [SerializeField] private TextMeshProUGUI energyText;

    /// <summary>
    /// 根据当前的能量值，更新整个能量条的显示。
    /// </summary>
    /// <param name="currentEnergy">当前精确的能量值，例如 3.5</param>
    /// <param name="maxEnergy">最大能量值，例如 4.0</param>
    public void UpdateEnergy(float currentEnergy, float maxEnergy)
    {
        int fullSegments = Mathf.FloorToInt(currentEnergy); // 完整的能量格数
        float remainder = currentEnergy - fullSegments;     // 正在恢复的小数部分

        // 更新数字
        energyText.text = fullSegments.ToString();

        // 遍历所有能量格图片
        for (int i = 0; i < energySegments.Count; i++)
        {
            Image segment = energySegments[i];
            if (segment == null) continue;

            // --- 核心逻辑 ---
            if (i < fullSegments)
            {
                // 1. 对于已充满的格，直接显示完整
                segment.gameObject.SetActive(true);
                segment.fillAmount = 1f;
            }
            else if (i == fullSegments)
            {
                // 2. 对于正在恢复的那一格
                if (remainder > 0.01f) // 只有当有恢复进度时才显示
                {
                    segment.gameObject.SetActive(true);
                    segment.fillAmount = remainder; // 根据小数部分设置填充量
                }
                else
                {
                    segment.gameObject.SetActive(false); // 如果没有恢复进度，则隐藏
                }
            }
            else
            {
                // 3. 对于未来的空格，完全隐藏
                segment.gameObject.SetActive(false);
            }
        }
    }

    // 注意：这个新方案不再需要OnEnergySpent方法，因为UpdateEnergy已经能处理所有情况。
}