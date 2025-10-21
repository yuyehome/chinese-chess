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
        GameModeSelector.SelectedMode = GameModeType.RealTime;
        SceneManager.LoadScene(gameSceneName);
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