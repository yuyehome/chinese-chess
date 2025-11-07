// 文件路径: Assets/Scripts/_Core/Command/UseSkillCommand.cs

using UnityEngine;

public struct UseSkillCommand : ICommand
{
    public int casterPieceId;
    public int skillId;
    public Vector2Int targetGrid;
    public int targetPieceId;

    public void Execute(GameState state)
    {
        Debug.Log($"执行技能指令: Caster={casterPieceId}, Skill={skillId}");
        // TODO: 在技能系统(B-3.1)阶段实现具体逻辑
    }
}