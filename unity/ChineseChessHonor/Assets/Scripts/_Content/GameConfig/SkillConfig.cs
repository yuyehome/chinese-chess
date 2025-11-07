// 文件路径: Assets/Scripts/_Content/GameConfig/SkillConfig.cs

using UnityEngine;

[CreateAssetMenu(fileName = "SkillConfig", menuName = "ChessHonor/Configs/Skill Config", order = 2)]
public class SkillConfig : ScriptableObject
{
    public int skillId;
    public string skillNameKey; // 用于本地化的Key
    public float cooldown;
    // ... 其他技能参数，如范围、伤害等
}