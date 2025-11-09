// 文件路径: Assets/Scripts/_Core/Command/RequestDrawCommand.cs

public struct RequestDrawCommand : ICommand
{
    public readonly PlayerTeam requestingTeam;

    public RequestDrawCommand(PlayerTeam requestingTeam)
    {
        this.requestingTeam = requestingTeam;
    }

    public void Execute(GameState state)
    {
        // 游戏逻辑将在 TurnBasedLogic.ValidateCommand 中检查是否可以求和
        // Execute 仅负责改变状态
        state.drawRequestFrom = requestingTeam;
    }
}