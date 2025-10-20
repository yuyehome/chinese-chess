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

    // 存储所有正在移动中的棋子，方便每帧集中更新它们的状态
    private readonly List<PieceComponent> movingPieces = new List<PieceComponent>();

    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
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
                    if (progress > 0.6f) { state.IsAttacking = true; }
                    // 在移动中间阶段处于无敌状态
                    if (progress > 0.2f && progress < 0.8f) { state.IsVulnerable = false; }
                    break;
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

        // 步骤1: 更新棋子内部状态，并加入到移动列表中
        pieceToMove.RTState.IsMoving = true;
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
                    pc.RTState.ResetToDefault();
                    movingPieces.Remove(pc);
                    Debug.Log($"[State] {pc.name} 移动完成，状态已重置。");
                }
            }
        );

        // 步骤3: 消耗能量
        energySystem.SpendEnergy(movingColor);

        // 步骤4: 清理选择状态
        ClearSelection();
    }
}