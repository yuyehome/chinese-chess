// File: _Scripts/GameModes/RealTimeModeController.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 实时模式的核心控制器。
/// 负责处理无回合限制下，基于行动点的选择、移动、攻击以及棋子状态的实时更新。
/// </summary>
public class RealTimeModeController : GameModeController
{
    // --- 依赖模块 ---
    private readonly EnergySystem energySystem;

    public CombatManager CombatManager { get; private set; }

    // --- 内部状态 ---
    // 存储所有正在移动中的棋子，方便每帧集中更新它们的状态
    private readonly List<PieceComponent> movingPieces = new List<PieceComponent>();
    // 缓存上次为选中棋子计算的合法移动列表，用于检测变化以决定是否重绘高亮
    private List<Vector2Int> lastCalculatedValidMoves = new List<Vector2Int>();

    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem, float collisionDistanceSquared)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
        // 传入碰撞距离配置来实例化CombatManager
        this.CombatManager = new CombatManager(state, renderer, collisionDistanceSquared);
    }


    /// <summary>
    /// 初始化棋盘上所有棋子的实时状态数据。此方法必须在BoardRenderer完成渲染后调用。
    /// </summary>
    public void InitializeRealTimeStates()
    {
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (boardState.GetPieceAt(pos).Type != PieceType.None)
                {
                    PieceComponent pc = boardRenderer.GetPieceComponentAt(pos);
                    if (pc != null)
                    {
                        pc.RTState = new RealTimePieceState();
                        pc.RTState.LogicalPosition = pos; // 初始化棋子的逻辑位置
                    }
                }
            }
        }
        Debug.Log("[System] 实时模式控制器：已为所有棋子初始化实时状态(RTState)。");
    }

    #region Main Logic Loop

    /// <summary>
    /// 由GameManager每帧调用，作为实时逻辑的驱动器。
    /// </summary>
    public void Tick()
    {
        UpdateAllPieceStates();
        CombatManager.ProcessCombat(GetAllActivePieces());
        UpdateSelectionHighlights();
    }

    /// <summary>
    /// 遍历所有移动中的棋子，并根据其类型和移动进度，实时更新其攻击/防御状态和逻辑位置。
    /// </summary>
    private void UpdateAllPieceStates()
    {
        // 从后往前遍历，以安全地在循环中移除元素
        for (int i = movingPieces.Count - 1; i >= 0; i--)
        {
            PieceComponent pc = movingPieces[i];
            if (pc == null || pc.RTState == null || pc.RTState.IsDead)
            {
                movingPieces.RemoveAt(i);
                continue;
            }

            RealTimePieceState state = pc.RTState;
            PieceType type = pc.PieceData.Type;
            float progress = state.MoveProgress;

            // 1. 更新移动中棋子的逻辑位置
            UpdatePieceLogicalPosition(pc);

            // 2. 根据规则应用不同的攻防状态变化
            state.IsAttacking = false;
            state.IsVulnerable = true;

            switch (type)
            {
                case PieceType.Chariot:
                case PieceType.Soldier:
                case PieceType.General:
                case PieceType.Advisor:
                    state.IsAttacking = true;
                    state.IsVulnerable = true;
                    break;
                case PieceType.Cannon:
                    if (progress > 0.9f) { state.IsAttacking = true; }
                    state.IsVulnerable = true;
                    break;
                case PieceType.Horse:
                case PieceType.Elephant:
                    if (progress > 0.8f) { state.IsAttacking = true; }
                    if (progress > 0.1f && progress < 0.8f) { state.IsVulnerable = false; }
                    break;
            }

            // 调试日志，定期打印移动中棋子的状态
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"[State-Update] 移动中棋子: {pc.name}, type:{type}, Progress: {progress:F2}, LogicalPos: {pc.RTState.LogicalPosition}, Attacking: {pc.RTState.IsAttacking}, Vulnerable: {pc.RTState.IsVulnerable}");
            }
        }
    }

    /// <summary>
    /// 当有棋子被选中时，每帧检查并更新其合法移动点的视觉高亮。
    /// 这是实现“动态炮架”等实时战术的关键。
    /// </summary>
    private void UpdateSelectionHighlights()
    {
        if (selectedPiece == null) return;

        BoardState logicalBoard = GetLogicalBoardState();
        List<Vector2Int> newValidMoves = RuleEngine.GetValidMoves(selectedPiece.PieceData, selectedPiece.RTState.LogicalPosition, logicalBoard);

        // 仅当合法移动列表发生变化时才重绘，以优化性能
        if (!newValidMoves.SequenceEqual(lastCalculatedValidMoves))
        {
            currentValidMoves = newValidMoves;
            lastCalculatedValidMoves = new List<Vector2Int>(newValidMoves);

            boardRenderer.ClearAllHighlights();
            boardRenderer.ShowValidMoves(currentValidMoves, selectedPiece.PieceData.Color, logicalBoard);
            boardRenderer.ShowSelectionMarker(selectedPiece.RTState.LogicalPosition);
        }
    }

    #endregion

    #region Player Input Handling

    /// <summary>
    /// 处理玩家点击棋子的事件。
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        Debug.Log($"[Input] 玩家点击了棋子: {clickedPiece.name}");
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // 分支1: 已有选中棋子，且本次点击的是敌方棋子（意图吃子）
        if (selectedPiece != null && clickedPieceData.Color != selectedPiece.PieceData.Color)
        {
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                if (selectedPiece.RTState == null)
                {
                    Debug.LogError($"[Error] 严重错误：选中的棋子 {selectedPiece.name} 没有实时状态(RTState)！");
                    ClearSelection();
                    return;
                }
                if (selectedPiece.RTState.IsMoving)
                {
                    Debug.Log($"[Action] 操作失败: {selectedPiece.name} 已在移动中，不可操作。");
                    ClearSelection();
                    return;
                }
                if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
                {
                    PerformMove(selectedPiece, clickedPiece.BoardPosition);
                }
                else
                {
                    Debug.Log($"[Action] 操作失败: {selectedPiece.PieceData.Color}方行动点不足，无法吃子。");
                    ClearSelection();
                }
            }
            else
            {
                // 点击了非法的敌方目标，视为切换选择
                TrySelectPiece(clickedPiece);
            }
        }
        // 分支2: 点击己方棋子，或未选中任何棋子（意图选择）
        else
        {
            TrySelectPiece(clickedPiece);
        }
    }

    /// <summary>
    /// 处理玩家点击移动标记的事件。
    /// </summary>
    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        Debug.Log($"[Input] 玩家点击了移动标记，目标坐标: {marker.BoardPosition}");
        if (selectedPiece == null) return;

        if (selectedPiece.RTState == null)
        {
            Debug.LogError($"[Error] 严重错误：选中的棋子 {selectedPiece.name} 没有实时状态(RTState)！");
            ClearSelection();
            return;
        }
        if (selectedPiece.RTState.IsMoving)
        {
            Debug.Log($"[Action] 操作失败: {selectedPiece.name} 已在移动中，不可操作。");
            ClearSelection();
            return;
        }
        if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
        {
            PerformMove(selectedPiece, marker.BoardPosition);
        }
        else
        {
            Debug.Log($"[Action] 操作失败: {selectedPiece.PieceData.Color}方行动点不足，无法移动。");
            ClearSelection();
        }
    }

    /// <summary>
    /// 处理玩家点击棋盘空白区域的事件。
    /// </summary>
    public override void OnBoardClicked(RaycastHit hit)
    {
        Debug.Log("[Input] 玩家点击了棋盘空白区域，取消选择。");
        ClearSelection();
    }

    #endregion

    #region Helper Methods

    /// <summary>
    /// 尝试选择一个棋子，此操作会检查行动点是否足够。
    /// </summary>
    private void TrySelectPiece(PieceComponent pieceToSelect)
    {
        Piece pieceData = boardState.GetPieceAt(pieceToSelect.BoardPosition);
        if (energySystem.CanSpendEnergy(pieceData.Color))
        {
            SelectPiece(pieceToSelect);
            Debug.Log($"[Action] 成功选择棋子 {pieceToSelect.name}。");
        }
        else
        {
            Debug.Log($"[Action] 选择失败: {pieceData.Color}方行动点不足。");
            ClearSelection();
        }
    }

    /// <summary>
    /// 封装了执行一次移动的所有逻辑步骤。
    /// </summary>
    private void PerformMove(PieceComponent pieceToMove, Vector2Int targetPosition)
    {
        PlayerColor movingColor = pieceToMove.PieceData.Color;
        Debug.Log($"[Action] {movingColor}方 {pieceToMove.name} 开始移动到 {targetPosition}。");

        // 1. 更新棋子内部状态，标记为“移动中”并记录路径
        pieceToMove.RTState.IsMoving = true;
        pieceToMove.RTState.MoveStartPos = pieceToMove.BoardPosition;
        pieceToMove.RTState.MoveEndPos = targetPosition;
        movingPieces.Add(pieceToMove);

        // 2. 调用GameManager执行移动，并传入回调函数用于状态更新
        gameManager.ExecuteMove(
            pieceToMove.BoardPosition,
            targetPosition,
            // OnProgress: 动画播放过程中的回调，用于更新进度
            (pc, progress) => {
                if (pc != null && pc.RTState != null) pc.RTState.MoveProgress = progress;
            },
            // OnComplete: 动画完成时的回调
            (pc) => {
                // 在执行任何落子逻辑前，必须检查棋子是否在中途被击杀
                if (pc != null && pc.RTState != null && !pc.RTState.IsDead)
                {
                    // 只有存活的棋子才能执行落子和状态重置
                    boardState.SetPieceAt(pc.RTState.MoveEndPos, pc.PieceData);
                    pc.BoardPosition = pc.RTState.MoveEndPos;
                    pc.RTState.ResetToDefault(pc.RTState.MoveEndPos);
                    movingPieces.Remove(pc);
                    Debug.Log($"[State] {pc.name} 移动完成，状态已重置于 {pc.RTState.MoveEndPos}。");
                }
                else if (pc != null)
                {
                    // 如果棋子在中途死亡，只需确保它从移动列表中移除
                    movingPieces.Remove(pc);
                    Debug.Log($"[State] 已死亡的棋子 {pc.name} 动画结束，不执行落子逻辑。");
                }
            }
        );

        // 3. 消耗能量
        energySystem.SpendEnergy(movingColor);

        // 4. 清理当前的选择状态（高亮、标记等）
        ClearSelection();
    }

    /// <summary>
    /// 动态构建一个反映当前帧所有棋子逻辑位置的“虚拟”棋盘状态。
    /// </summary>
    private BoardState GetLogicalBoardState()
    {
        BoardState logicalBoard = boardState.Clone(); // 包含所有静止棋子
        foreach (var piece in movingPieces)
        {
            if (piece.RTState.IsDead) continue;

            // 根据棋子类型判断其在移动中是否为逻辑阻碍物
            switch (piece.PieceData.Type)
            {
                // 跳跃单位在空中不产生阻碍
                case PieceType.Horse:
                case PieceType.Elephant:
                case PieceType.Cannon:
                    break;
                // 实体单位在移动中会实时产生阻碍
                case PieceType.Chariot:
                case PieceType.Soldier:
                case PieceType.General:
                case PieceType.Advisor:
                default:
                    logicalBoard.SetPieceAt(piece.RTState.LogicalPosition, piece.PieceData);
                    break;
            }
        }
        return logicalBoard;
    }

    /// <summary>
    /// 通过线性插值，估算移动中棋子当前所在的逻辑格子坐标。
    /// </summary>
    private void UpdatePieceLogicalPosition(PieceComponent piece)
    {
        float progress = piece.RTState.MoveProgress;
        Vector2 start = piece.RTState.MoveStartPos;
        Vector2 end = piece.RTState.MoveEndPos;

        float logicalX = Mathf.Lerp(start.x, end.x, progress);
        float logicalY = Mathf.Lerp(start.y, end.y, progress);

        // 四舍五入到最近的格子
        piece.RTState.LogicalPosition = new Vector2Int(Mathf.RoundToInt(logicalX), Mathf.RoundToInt(logicalY));
    }

    /// <summary>
    /// 获取场景中所有存活棋子的列表，用于战斗检测。
    /// </summary>
    private List<PieceComponent> GetAllActivePieces()
    {
        List<PieceComponent> allPieces = new List<PieceComponent>();

        // 1. 添加所有移动中的棋子
        allPieces.AddRange(movingPieces.Where(p => p != null && !p.RTState.IsDead));

        // 2. 遍历 BoardState 添加所有静止的棋子
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (boardState.GetPieceAt(pos).Type != PieceType.None)
                {
                    PieceComponent pc = boardRenderer.GetPieceComponentAt(pos);
                    if (pc != null && pc.RTState != null && !pc.RTState.IsDead)
                    {
                        allPieces.Add(pc);
                    }
                }
            }
        }

        return allPieces.Distinct().ToList(); // 去重并返回
    }

    #endregion
}