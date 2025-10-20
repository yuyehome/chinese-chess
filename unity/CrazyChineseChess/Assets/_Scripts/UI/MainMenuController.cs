// File: _Scripts/UI/MainMenuController.cs

using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    // ����ַ�����Ҫ�������Ϸ�����ļ�����ȫһ��
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

    // (��ѡ) ����һ���˳���Ϸ�Ĺ���
    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit();
    }
}