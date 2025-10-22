// File: _Scripts/Core/GameModeSelector.cs

/// <summary>
/// ��������Ϸ֧�ֵ����ֺ���ģʽ��
/// </summary>
public enum GameModeType
{
    TurnBased,
    RealTime
}

// --- ����ö�� ---
public enum AIDifficulty
{
    Easy,
    Hard,
    VeryHard
}

/// <summary>
/// һ���򵥵ľ�̬�࣬�����ڳ���֮�䴫�����ѡ�����Ϸģʽ��
/// </summary>
public static class GameModeSelector
{
    /// <summary>
    /// �洢��������˵�ѡ�����Ϸģʽ��GameManager�����ݴ�ֵ����ʼ����Ӧ�Ŀ�������
    /// </summary>
    public static GameModeType SelectedMode { get; set; } = GameModeType.TurnBased; // Ĭ��Ϊ�غ��ƣ��Է�ֱ�Ӵ���Ϸ��������

    public static AIDifficulty SelectedAIDifficulty { get; set; } = AIDifficulty.Easy;

}