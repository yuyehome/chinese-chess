// File: _Scripts/GameManager.cs
using UnityEngine;

/// <summary>
/// 游戏总管理器，【重构后】负责初始化游戏、管理核心状态和当前的游戏模式。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public BoardState CurrentBoardState { get; private set; }

    // 【新增】当前激活的游戏模式控制器
    public GameModeController CurrentGameMode { get; private set; }

    // 缓存对BoardRenderer的引用
    private BoardRenderer boardRenderer;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        // --- 依赖获取 ---
        boardRenderer = FindObjectOfType<BoardRenderer>();
        if (boardRenderer == null)
        {
            Debug.LogError("场景中找不到 BoardRenderer!");
            return;
        }

        // --- 核心数据初始化 ---
        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();

        // --- 游戏模式初始化 ---
        // 在这里决定启动哪种游戏模式。我们先默认启动回合制模式。
        // 未来可以根据主菜单的选择来实例化不同的控制器。
        CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
        Debug.Log("游戏开始，已进入回合制模式。");

        // --- 初始渲染 ---
        boardRenderer.RenderBoard(CurrentBoardState);
    }

    /// <summary>
    /// 执行移动操作。这个方法保持不变，因为它代表了一个原子性的游戏行为，
    /// 无论在哪种模式下，“移动”这个动作本身是不变的。
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        if (targetPiece.Type != PieceType.None)
        {
            boardRenderer.RemovePieceAt(to);
        }
        CurrentBoardState.MovePiece(from, to);
        boardRenderer.MovePiece(from, to);
    }
}