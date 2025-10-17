// File: _Scripts/GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    // 单例模式，方便全局访问 GameManager 实例
    public static GameManager Instance { get; private set; }
    
    public BoardState CurrentBoardState { get; private set; }

    private void Awake()
    {
        // 实现简单的单例模式
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
        }
        else
        {
            Instance = this;
        }
    }

    void Start()
    {
        // 创建并初始化棋盘逻辑数据
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        
        // 通知渲染器根据新的棋盘状态绘制棋子
        // 我们通过 FindObjectOfType 找到它，更优的方式是事件或依赖注入，但目前这样最直观
        BoardRenderer renderer = FindObjectOfType<BoardRenderer>();
        if (renderer != null)
        {
            renderer.RenderBoard(CurrentBoardState);
        }
        else
        {
            Debug.LogError("场景中找不到 BoardRenderer!");
        }
    }
}