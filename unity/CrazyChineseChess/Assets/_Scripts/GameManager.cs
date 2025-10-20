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

    public EnergySystem EnergySystem { get; private set; }

    private BoardRenderer boardRenderer;
    private bool isGameEnded = false; // 新增一个标志位，防止游戏结束后还能继续操作

    public bool IsAnimating { get; private set; } = false; // 【新增】动画状态锁

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

        // 【修正】只有在实时模式下才需要创建EnergySystem
        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem = new EnergySystem();
        }

        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                Debug.Log("游戏开始，已进入【传统回合制】模式。");
                break;
            case GameModeType.RealTime:
                // 【关键修正】确保在这里将已经创建的 EnergySystem 实例传递进去
                CurrentGameMode = new RealTimeModeController(this, CurrentBoardState, boardRenderer, EnergySystem);
                Debug.Log("游戏开始，已进入【实时对战】模式。");
                break;
            default:
                Debug.LogError("未知的游戏模式！默认进入回合制。");
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                break;
        }

        boardRenderer.RenderBoard(CurrentBoardState);
    }


    // Update方法来驱动能量系统
    private void Update()
    {
        // 只有在实时模式下才更新能量
        if (GameModeSelector.SelectedMode == GameModeType.RealTime && !isGameEnded)
        {
            EnergySystem?.Tick();

            // (可选) 在这里打印能量值用于调试
            // Debug.Log($"Red Energy: {EnergySystem.GetEnergyInt(PlayerColor.Red)}, Black Energy: {EnergySystem.GetEnergyInt(PlayerColor.Black)}");
        }
    }

    /// <summary>
    /// 【修改】现在只负责逻辑移动，并将isCapture传递给BoardRenderer
    /// </summary>
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        if (isGameEnded) return;

        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        bool isCapture = targetPiece.Type != PieceType.None;

        // 【旧的吃子逻辑删除】
        // 碰撞系统会自动处理吃子，但如果终点是静止棋子，我们需要特殊处理
        // 这是一个复杂点，我们先简化：假设移动到终点时，如果目标是静止棋子，也算碰撞

        // 1. 更新数据层
        CurrentBoardState.MovePiece(from, to);

        // 2. 触发视觉移动
        boardRenderer.MovePiece(from, to, isCapture);

        // 3. 将军检查暂时可以保留，但其有效性会受实时状态影响
        CheckForCheck();
    }

    /// <summary>
    /// 当PieceStateController报告一个棋子死亡时，此方法被调用以更新逻辑棋盘。
    /// </summary>
    /// <param name="position">死亡棋子所在的棋盘坐标</param>

    public void ReportPieceDeath(Vector2Int position)
    {
        // 获取死亡棋子的数据，用于后续可能的判断（比如是不是将/帅）
        Piece deadPiece = CurrentBoardState.GetPieceAt(position);

        // 从逻辑棋盘上移除
        CurrentBoardState.RemovePieceAt(position);

        Debug.Log($"逻辑棋盘 BoardState 已在坐标 {position} 处移除棋子。");

        // 【未来扩展】在这里检查游戏是否结束
        if (deadPiece.Type == PieceType.General)
        {
            GameStatus status = (deadPiece.Color == PlayerColor.Black) ? GameStatus.RedWin : GameStatus.BlackWin;
            // HandleEndGame(status); // 注意：HandleEndGame方法可能需要从ExecuteMove中提取出来，成为公共方法
        }
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