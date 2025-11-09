// 文件路径: Assets/Scripts/_Core/Command/AcceptDrawCommand.cs

public struct AcceptDrawCommand : ICommand
{
    public readonly PlayerTeam acceptingTeam;

    public AcceptDrawCommand(PlayerTeam acceptingTeam)
    {
        this.acceptingTeam = acceptingTeam;
    }

    public void Execute(GameState state)
    {
        // 游戏逻辑将在 TurnBasedLogic.ValidateCommand 中检查是否存在有效求和请求
        // Execute 仅负责改变状态
        state.phase = GamePhase.GameOver;
        state.winner = PlayerTeam.None; // None 代表和棋
    }
}