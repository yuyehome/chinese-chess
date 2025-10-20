// File: _Scripts/UI/MainMenuController.cs

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // 这个字符串需要与你的游戏场景文件名完全一致
    public string gameSceneName = "Game";

    public void StartTurnBasedGame()
    {
        GameModeSelector.SelectedMode = GameModeType.TurnBased;
        SceneManager.LoadScene(gameSceneName);
    }

    public void StartRealTimeGame()
    {
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        SceneManager.LoadScene(gameSceneName);
    }

    // (可选) 增加一个退出游戏的功能
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}