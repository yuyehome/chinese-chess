// File: _Scripts/GameManager.cs
using UnityEngine;

/// <summary>
/// 【已修正】游戏总管理器，终局判断逻辑已更新以符合GDD。
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public BoardState CurrentBoardState { get; private set; }
    public GameModeController CurrentGameMode { get; private set; }

    private BoardRenderer boardRenderer;
    private bool isGameEnded = false; // 新增一个标志位，防止游戏结束后还能继续操作

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
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

        CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
        Debug.Log("游戏开始，已进入回合制模式。");

        boardRenderer.RenderBoard(CurrentBoardState);
    }

    /// <summary>
    /// 【已修正】执行移动操作，并在移动后检查将军和游戏结束（吃掉将/帅）。
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        // 如果游戏已经结束，则不执行任何操作
        if (isGameEnded) return;

        // --- 1. 检查吃子与终局 ---
        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        if (targetPiece.Type != PieceType.None)
        {
            // TODO: 在这里播放“吃子”音效
            boardRenderer.RemovePieceAt(to);

            // 【核心改动】检查被吃的棋子是否是将/帅
            if (targetPiece.Type == PieceType.General)
            {
                // 游戏结束！
                GameStatus status = (targetPiece.Color == PlayerColor.Black) ? GameStatus.RedWin : GameStatus.BlackWin;
                HandleEndGame(status);
                // 必须在更新数据和视觉之前返回，因为游戏已经结束了
                CurrentBoardState.MovePiece(from, to); // 仍然执行数据移动，以便棋盘状态是最终的
                boardRenderer.MovePiece(from, to);
                return;
            }
        }

        // --- 2. 更新数据和视觉 ---
        CurrentBoardState.MovePiece(from, to);
        boardRenderer.MovePiece(from, to);

        // --- 3. 检查将军状态 (作为提示) ---
        CheckForCheck();
    }

    /// <summary>
    /// 【已修正】只检查将军状态，不判断游戏结束。
    /// </summary>
    private void CheckForCheck()
    {
        // 在回合制模式下，检查下一个行动方是否被将军
        if (CurrentGameMode is TurnBasedModeController turnBasedMode)
        {
            PlayerColor nextPlayer = turnBasedMode.GetCurrentPlayer();
            if (RuleEngine.IsKingInCheck(nextPlayer, CurrentBoardState))
            {
                Debug.Log($"将军！{nextPlayer} 方的王正被攻击！");
                // TODO: 在这里播放“将军”音效，并显示将军UI提示
            }
        }
        // TODO: 在实时模式下，可能需要同时检查双方是否被将军
    }

    /// <summary>
    /// 处理游戏结束的逻辑。
    /// </summary>
    private void HandleEndGame(GameStatus status)
    {
        isGameEnded = true; // 设置游戏结束标志
        Debug.Log($"游戏结束！结果: {status}");
        // TODO: 在这里播放“胜利/失败/和棋”音效，并显示游戏结束面板

        // 禁用玩家输入
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }
}