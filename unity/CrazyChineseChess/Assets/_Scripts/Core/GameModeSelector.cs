// File: _Scripts/Core/GameModeSelector.cs

/// <summary>
/// 定义了游戏支持的两种核心模式。
/// </summary>
public enum GameModeType
{
    TurnBased,
    RealTime
}

/// <summary>
/// 一个简单的静态类，用于在场景之间传递玩家选择的游戏模式。
/// </summary>
public static class GameModeSelector
{
    /// <summary>
    /// 存储玩家在主菜单选择的游戏模式。GameManager将根据此值来初始化对应的控制器。
    /// </summary>
    public static GameModeType SelectedMode { get; set; } = GameModeType.TurnBased; // 默认为回合制，以防直接从游戏场景启动
}