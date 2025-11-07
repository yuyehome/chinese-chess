// 文件路径: Assets/Scripts/_Content/GameConfig/HeroConfig.cs

using UnityEngine;

[CreateAssetMenu(fileName = "HeroConfig", menuName = "ChessHonor/Configs/Hero Config", order = 1)]
public class HeroConfig : ScriptableObject
{
    public int heroId;
    public string heroNameKey;
    public string heroDescriptionKey;
    public PieceType allowedPieceType; // 该武将能绑定的棋子类型
    public SkillConfig skill;
}