// 文件路径: Assets/Scripts/_Core/GameModes/IGameModeLogic.cs

public interface IGameModeLogic
{
    /// <summary>
    /// 在对局开始时调用，用于设置GameState的初始状态。
    /// </summary>
    void Initialize(GameState state);

    /// <summary>
    /// 在指令执行前，检查该指令在当前规则和状态下是否合法。
    /// </summary>
    bool ValidateCommand(ICommand command, GameState state);

    /// <summary>
    /// 在每个固定的逻辑Tick被调用，用于处理随时间变化的状态更新（如行动点恢复）。
    /// </summary>
    void OnTick(GameState state);
}