// 文件路径: Assets/Scripts/_Content/GameConfig/GameBalanceConfig.cs

using UnityEngine;

[CreateAssetMenu(fileName = "GameBalanceConfig", menuName = "ChessHonor/Configs/Game Balance Config", order = 0)]
public class GameBalanceConfig : ScriptableObject
{
    [Header("实时模式 - 核心数值")]
    public float actionPointMax = 4f;
    public float pieceMoveSpeed = 5.0f; // 棋子在棋盘上的移动速度 (单位/秒)
    // 在这里添加其他所有需要策划配置的游戏平衡数值
}