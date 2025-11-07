// 文件路径: Assets/Scripts/_Core/GameModes/GameModeManager.cs

using System;

public static class GameModeManager
{
    public static IGameModeLogic CreateLogic(GameModeType modeType)
    {
        switch (modeType)
        {
            case GameModeType.RealTime_Fair:
            case GameModeType.RealTime_Skill:
                return new RealTimeLogic();
            // case GameModeType.Traditional_TurnBased:
            // return new TurnBasedLogic(); // TODO: Phase B
            default:
                throw new ArgumentOutOfRangeException(nameof(modeType), $"不支持的游戏模式: {modeType}");
        }
    }
}