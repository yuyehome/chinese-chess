// 文件路径: Assets/Scripts/_Core/Command/ResignCommand.cs

public struct ResignCommand : ICommand
{
    public readonly PlayerTeam resigningTeam;

    public ResignCommand(PlayerTeam resigningTeam)
    {
        this.resigningTeam = resigningTeam;
    }

    public void Execute(GameState state)
    {
        state.phase = GamePhase.GameOver;

        // 胜者是另一方。这里简单处理1v1的情况。
        // 2v2的逻辑会更复杂，后续再实现。
        if (resigningTeam == PlayerTeam.Red)
        {
            state.winner = PlayerTeam.Black;
        }
        else if (resigningTeam == PlayerTeam.Black)
        {
            state.winner = PlayerTeam.Red;
        }
    }
}