// File: _Scripts/GameManager.cs
using UnityEngine;

/// <summary>
/// 游戏总管理器，负责协调游戏流程和核心状态。
/// 采用单例模式，方便全局访问。
/// </summary>
public class GameManager : MonoBehaviour
{
    // 单例模式的静态实例
    public static GameManager Instance { get; private set; }

    // 游戏的核心数据状态，代表了棋盘上所有棋子的逻辑位置和信息
    public BoardState CurrentBoardState { get; private set; }

    // 缓存对BoardRenderer的引用，避免在运行时频繁查找，提高性能
    private BoardRenderer boardRenderer;

    private void Awake()
    {
        // 实现简单的单例模式，确保场景中只有一个GameManager实例
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
        // 在游戏开始时，找到场景中的BoardRenderer
        boardRenderer = FindObjectOfType<BoardRenderer>();
        if (boardRenderer == null)
        {
            // 如果找不到，这是一个严重错误，需要报错并停止执行
            Debug.LogError("场景中找不到 BoardRenderer!");
            return;
        }

        // 创建并初始化棋盘的逻辑数据
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();

        // 通知渲染器根据初始化的棋盘数据，在场景中创建棋子模型
        boardRenderer.RenderBoard(CurrentBoardState);
    }

    /// <summary>
    /// 执行一次移动或吃子操作。这是游戏状态变更的唯一入口，确保了逻辑的集中控制。
    /// </summary>
    /// <param name="from">棋子移动的起始坐标</param>
    /// <param name="to">棋子移动的目标坐标</param>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        // --- 核心移动逻辑 ---

        // 1. 在执行移动之前，先检查目标位置是否有棋子将被吃掉
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        if (targetPiece.Type != PieceType.None)
        {
            // 如果有棋子，通知渲染器从场景中移除该棋子的视觉对象(GameObject)
            boardRenderer.RemovePieceAt(to);
        }

        // 2. 在数据层(BoardState)中更新棋子的位置信息
        //    这是游戏状态的“真实来源”，必须先于视觉更新
        CurrentBoardState.MovePiece(from, to);

        // 3. 在视觉层(BoardRenderer)中平滑地移动棋子的GameObject
        boardRenderer.MovePiece(from, to);

        // TODO: 在这个方法的末尾，可以加入将军、将死、胜负判断等后续逻辑
    }
}