using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 实时模式的核心控制器。
/// 负责处理无回合限制下，基于行动点的选择、移动、攻击以及棋子状态的实时更新。
/// </summary>
public class RealTimeModeController : GameModeController
{
    private readonly EnergySystem energySystem;

    private readonly CombatManager combatManager;

    // 存储所有正在移动中的棋子，方便每帧集中更新它们的状态
    private readonly List<PieceComponent> movingPieces = new List<PieceComponent>();

    // 缓存上次计算的合法移动，用于检测变化
    private List<Vector2Int> lastCalculatedValidMoves = new List<Vector2Int>();

    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
        this.combatManager = new CombatManager(state, renderer);
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
                        // 初始化棋子的逻辑位置
                        pc.RTState.LogicalPosition = pos;
                    }
                }
            }
        }
        Debug.Log("[System] 实时模式控制器：已为所有棋子初始化实时状态(RTState)。");
    }

    /// <summary>
    /// 由GameManager每帧调用，作为实时逻辑的驱动器。
    /// </summary>
    public void Tick()
    {
        UpdateAllPieceStates();
        combatManager.ProcessCombat(GetAllActivePieces()); // 【修改】传递棋子列表
        UpdateSelectionHighlights();
    }

    /// <summary>
    /// 遍历所有移动中的棋子，并根据其类型和移动进度，实时更新其攻击/防御状态。
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

            // 每帧开始时，先重置为移动中的基础状态
            state.IsAttacking = false;
            state.IsVulnerable = true;

            // 更新移动中棋子的逻辑位置
            UpdatePieceLogicalPosition(pc);

            // 根据规则应用不同的状态变化
            switch (type)
            {
                case PieceType.Chariot:
                case PieceType.Soldier:
                case PieceType.General:
                case PieceType.Advisor:
                    // 实体移动棋子，全程保持攻击性和可被攻击
                    state.IsAttacking = true;
                    state.IsVulnerable = true;
                    break;
                case PieceType.Cannon:
                    // 炮在跳跃攻击的最后阶段才具有攻击性
                    if (progress > 0.9f) { state.IsAttacking = true; }
                    state.IsVulnerable = true;
                    break;
                case PieceType.Horse:
                case PieceType.Elephant:
                    // 马和象在移动后半段具有攻击性
                    if (progress > 0.8f) { state.IsAttacking = true; }
                    // 在移动中间阶段处于无敌状态
                    if (progress > 0.1f && progress < 0.8f) { state.IsVulnerable = false; }
                    break;
            }

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[State-Update] 移动中棋子: {pc.name}, Progress: {progress:F2}, LogicalPos: {pc.RTState.LogicalPosition}, Attacking: {pc.RTState.IsAttacking}, Vulnerable: {pc.RTState.IsVulnerable}");
            }

        }
    }

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
                // 防御性编程：确保选中的棋子状态正常
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
                    PerformMove(selectedPiece, clickedPiece.BoardPosition, true);
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

        // 防御性编程：确保选中的棋子状态正常
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
            PerformMove(selectedPiece, marker.BoardPosition, false);
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

    /// <summary>
    /// 尝试选择一个棋子，会检查行动点是否足够。
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
    /// 封装了执行移动的核心逻辑，供OnPieceClicked和OnMarkerClicked调用。
    /// </summary>
    private void PerformMove(PieceComponent pieceToMove, Vector2Int targetPosition, bool isCapture)
    {
        PlayerColor movingColor = pieceToMove.PieceData.Color;
        string moveType = isCapture ? "吃子" : "移动";
        Debug.Log($"[Action] {movingColor}方 {pieceToMove.name} 开始 {moveType} 到 {targetPosition}。");

        // 【重要】在启动移动时，决定炮是否应该播放跳跃动画
        bool isCannonJump = pieceToMove.PieceData.Type == PieceType.Cannon && isCapture;

        // 步骤1: 更新棋子内部状态，并加入到移动列表中
        pieceToMove.RTState.IsMoving = true;

        pieceToMove.RTState.MoveStartPos = pieceToMove.BoardPosition;
        pieceToMove.RTState.MoveEndPos = targetPosition;

        movingPieces.Add(pieceToMove);

        // 步骤2: 调用GameManager执行移动，并传入回调函数用于状态更新
        gameManager.ExecuteMove(
            pieceToMove.BoardPosition,
            targetPosition,
            // OnProgress: 动画过程中的回调
            (pc, progress) => {
                if (pc != null && pc.RTState != null) pc.RTState.MoveProgress = progress;
            },
            // OnComplete: 动画完成时的回调
            (pc) => {
                if (pc != null && pc.RTState != null)
                {

                    // 在棋盘上正式“落座”
                    boardState.SetPieceAt(pc.RTState.MoveEndPos, pc.PieceData);
                    pc.BoardPosition = pc.RTState.MoveEndPos;
                    pc.RTState.ResetToDefault(pc.RTState.MoveEndPos);
                    movingPieces.Remove(pc);
                    Debug.Log($"[State] {pc.name} 移动完成，状态已重置于 {pc.RTState.MoveEndPos}。");

                }
            }
        );

        // 步骤3: 消耗能量
        energySystem.SpendEnergy(movingColor);

        // 步骤4: 清理选择状态
        ClearSelection();
    }


    /// <summary>
    /// 当有棋子被选中时，每帧检查并更新其合法移动点的视觉高亮。
    /// </summary>
    private void UpdateSelectionHighlights()
    {
        if (selectedPiece == null) return;

        // 1. 获取当前的“虚拟”棋盘状态
        BoardState logicalBoard = GetLogicalBoardState();

        // 2. 重新计算合法移动
        // 注意：这里我们暂时继续使用旧的RuleEngine，它不认识实时状态，但能读取虚拟棋盘
        // 如果需要更复杂的实时规则（如穿人），则需要创建 RealTimeRuleEngine
        List<Vector2Int> newValidMoves = RuleEngine.GetValidMoves(selectedPiece.PieceData, selectedPiece.RTState.LogicalPosition, logicalBoard);

        // 3. 检查列表是否有变化
        if (!newValidMoves.SequenceEqual(lastCalculatedValidMoves))
        {
            // 4. 如果有变化，则更新当前合法移动列表并重绘高亮
            currentValidMoves = newValidMoves;
            lastCalculatedValidMoves = new List<Vector2Int>(newValidMoves); // 必须创建新列表

            // 清除旧高亮并显示新高亮
            boardRenderer.ClearAllHighlights();
            boardRenderer.ShowValidMoves(currentValidMoves, selectedPiece.PieceData.Color, logicalBoard);
            // 重新显示选择标记，因为它可能被ClearAllHighlights清除了
            boardRenderer.ShowSelectionMarker(selectedPiece.RTState.LogicalPosition);
        }
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

            // TODO: 这里可以根据您的规则扩展，例如“虚无”状态的棋子不产生阻挡
            // 目前，所有移动中的棋子都在其逻辑位置上产生阻挡
            logicalBoard.SetPieceAt(piece.RTState.LogicalPosition, piece.PieceData);
        }
        return logicalBoard;
    }

    /// <summary>
    /// 根据棋子的3D世界坐标，反向计算出其当前所在的逻辑格子坐标。
    /// </summary>
    private void UpdatePieceLogicalPosition(PieceComponent piece)
    {
        // 这个反向计算比较复杂，我们先用一个简化的线性插值来模拟
        // 在低帧率下可能不精确，但足以验证逻辑
        float progress = piece.RTState.MoveProgress;
        Vector2 start = piece.RTState.MoveStartPos;
        Vector2 end = piece.RTState.MoveEndPos;

        // 线性插值计算理论位置
        float logicalX = Mathf.Lerp(start.x, end.x, progress);
        float logicalY = Mathf.Lerp(start.y, end.y, progress);

        // 四舍五入到最近的格子
        piece.RTState.LogicalPosition = new Vector2Int(Mathf.RoundToInt(logicalX), Mathf.RoundToInt(logicalY));
    }

    /// <summary>
    /// 获取场景中所有存活棋子的列表。
    /// </summary>
    // 【修改】GetAllActivePieces 方法，使其不再依赖 pieceObjects 数组
    private List<PieceComponent> GetAllActivePieces()
    {
        List<PieceComponent> allPieces = new List<PieceComponent>();

        // 1. 获取所有移动中的棋子
        allPieces.AddRange(movingPieces.Where(p => p != null && !p.RTState.IsDead));

        // 2. 遍历 BoardState 获取所有静止的棋子
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (boardState.GetPieceAt(pos).Type != PieceType.None)
                {
                    // 通过 BoardRenderer 获取 GameObject，进而获取 Component
                    // 这一步现在是安全的，因为静止棋子的 pieceObjects 引用是正确的
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

}