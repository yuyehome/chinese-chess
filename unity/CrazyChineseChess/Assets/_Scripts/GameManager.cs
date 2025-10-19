// File: _Scripts/GameManager.cs
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public BoardState CurrentBoardState { get; private set; }
    private BoardRenderer boardRenderer; // 缓存对BoardRenderer的引用

    private void Awake()
    {
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
        boardRenderer = FindObjectOfType<BoardRenderer>();
        if (boardRenderer == null)
        {
            Debug.LogError("场景中找不到 BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();

        boardRenderer.RenderBoard(CurrentBoardState);
    }

    /// <summary>
    /// 执行一次移动或吃子操作
    /// </summary>
    /// <param name="from">起始坐标</param>
    /// <param name="to">目标坐标</param>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        // 1. 检查目标位置是否有棋子被吃
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        if (targetPiece.Type != PieceType.None)
        {
            // 有棋子被吃，通知渲染器移除该棋子的视觉对象
            boardRenderer.RemovePieceAt(to);
        }

        // 2. 在数据层执行移动
        CurrentBoardState.MovePiece(from, to);

        // 3. 在视觉层执行移动
        boardRenderer.MovePiece(from, to);

        // TODO: 在这里可以加入将军、将死等判断逻辑
    }
}