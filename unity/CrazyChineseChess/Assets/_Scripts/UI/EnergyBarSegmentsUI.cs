// File: _Scripts/UI/EnergyBarSegmentsUI.cs

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// 控制分段式能量条UI的脚本。
/// 通过控制多个Image子对象的显隐和填充(fillAmount)来实现能量的动态显示。
/// </summary>
public class EnergyBarSegmentsUI : MonoBehaviour
{
    [Tooltip("在Inspector中按顺序拖入代表能量格的Image组件(例如，从左到右或从下到上)")]
    [SerializeField] private List<Image> energySegments;
    [Tooltip("用于显示当前能量整数值的TextMeshProUGUI组件")]
    [SerializeField] private TextMeshProUGUI energyText;

    /// <summary>
    /// 根据当前的精确能量值，更新整个能量条的视觉表现。
    /// </summary>
    /// <param name="currentEnergy">当前精确的能量值 (例如 3.5)</param>
    /// <param name="maxEnergy">最大能量值 (例如 4.0)</param>
    public void UpdateEnergy(float currentEnergy, float maxEnergy)
    {
        int fullSegments = Mathf.FloorToInt(currentEnergy); // 已充满的能量格数
        float remainder = currentEnergy - fullSegments;     // 正在恢复的小数部分 (0.0 to 1.0)

        // 更新数字显示
        if (energyText != null)
        {
            energyText.text = fullSegments.ToString();
        }

        // 遍历所有能量格Image，根据能量值更新其状态
        for (int i = 0; i < energySegments.Count; i++)
        {
            Image segment = energySegments[i];
            if (segment == null) continue;

            if (i < fullSegments)
            {
                // 对于已充满的格，设置为可见并完全填充
                segment.gameObject.SetActive(true);
                segment.fillAmount = 1f;
            }
            else if (i == fullSegments)
            {
                // 对于正在恢复的那一格
                if (remainder > 0.01f) // 只有当有恢复进度时才显示，避免闪烁
                {
                    segment.gameObject.SetActive(true);
                    segment.fillAmount = remainder; // 根据小数部分设置填充量，产生平滑增长动画
                }
                else
                {
                    segment.gameObject.SetActive(false); // 如果没有恢复进度，则隐藏
                }
            }
            else
            {
                // 对于未来的空格，完全隐藏
                segment.gameObject.SetActive(false);
            }
        }
    }
}