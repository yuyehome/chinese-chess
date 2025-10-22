// File: _Scripts/UI/MainMenuController.cs

using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 主菜单场景的UI控制器。
/// 负责处理按钮点击事件，设置选择的游戏模式，并加载游戏场景。
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Tooltip("要加载的游戏场景的名称，必须与Build Settings中的场景名一致")]
    public string gameSceneName = "Game";

    // --- 假设这个方法由“对战AI”按钮调用，用于打开难度选择面板 ---
    public void OnAIGameButtonClicked(GameObject difficultyPanel)
    {
        difficultyPanel.SetActive(true);
    }

    // --- 以下是三个难度按钮的处理函数 ---
    public void StartAIGameEasy()
    {
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.Easy;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartAIGameHard()
    {
        // 实际开发时，这里会加载HardAI，现在先用Easy占位
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.Hard;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartAIGameVeryHard()
    {
        // 实际开发时，这里会加载VeryHardAI，现在先用Easy占位
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        GameModeSelector.SelectedAIDifficulty = AIDifficulty.VeryHard;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// “开始回合制游戏”按钮的点击事件处理函数。
    /// </summary>
    public void StartTurnBasedGame()
    {
        GameModeSelector.SelectedMode = GameModeType.TurnBased;
        SceneManager.LoadScene(gameSceneName);
    }

    /// <summary>
    /// “开始实时游戏”按钮的点击事件处理函数。
    /// </summary>
    public void StartRealTimeGame()
    {
        StartAIGameEasy();
    }

    /// <summary>
    /// “退出游戏”按钮的点击事件处理函数。
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("正在退出游戏...");
        Application.Quit();
    }
}