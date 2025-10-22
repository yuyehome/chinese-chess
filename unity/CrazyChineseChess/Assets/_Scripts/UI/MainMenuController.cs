// File: _Scripts/UI/MainMenuController.cs

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// ���˵�������UI��������
/// ������ť����¼�������ѡ�����Ϸģʽ����������Ϸ������
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("Ҫ���ص���Ϸ���������ƣ�������Build Settings�еĳ�����һ��")]
    public string gameSceneName = "Game";

    // --- ������������ɡ���սAI����ť���ã����ڴ��Ѷ�ѡ����� ---
    public void OnAIGameButtonClicked(GameObject difficultyPanel)
    {
        difficultyPanel.SetActive(true);
    }

    // --- �����������ѶȰ�ť�Ĵ����� ---
    public void StartAIGameEasy()
    {
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.Easy;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartAIGameHard()
    {
        // ʵ�ʿ���ʱ����������HardAI����������Easyռλ
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.Hard;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartAIGameVeryHard()
    {
        // ʵ�ʿ���ʱ����������VeryHardAI����������Easyռλ
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.VeryHard;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// ����ʼ�غ�����Ϸ����ť�ĵ���¼���������
    /// </summary>
    public void StartTurnBasedGame()
    {
        GameModeSelector.SelectedMode = GameModeType.TurnBased;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// ����ʼʵʱ��Ϸ����ť�ĵ���¼���������
    /// </summary>
    public void StartRealTimeGame()
    {
        StartAIGameEasy();
    }

    /// <summary>
    /// ���˳���Ϸ����ť�ĵ���¼���������
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("�����˳���Ϸ...");
        Application.Quit();
    }
}