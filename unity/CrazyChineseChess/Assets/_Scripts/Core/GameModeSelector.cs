// File: _Scripts/Core/GameModeSelector.cs

public enum GameModeType
{
    TurnBased,
    RealTime
}

public static class GameModeSelector
{
    public static GameModeType SelectedMode { get; set; } = GameModeType.TurnBased; // Ĭ��Ϊ�غ���
}