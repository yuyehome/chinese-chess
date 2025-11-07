// 文件路径: Assets/Scripts/_Core/Command/EndTurnCommand.cs

using UnityEngine;

public struct EndTurnCommand : ICommand
{
    public PlayerTeam team;

    public EndTurnCommand(PlayerTeam team)
    {
        this.team = team;
    }

    public void Execute(GameState state)
    {
        if (state.currentTurn == team)
        {
            state.currentTurn = (team == PlayerTeam.Red) ? PlayerTeam.Black : PlayerTeam.Red;
            Debug.Log($"回合结束，轮到 {state.currentTurn} 行动。");
        }
    }
}