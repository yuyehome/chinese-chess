// File: _Scripts/GameModes/RealTimeModeController.cs

using UnityEngine;
using System.Collections.Generic; // <--- 必须有这一行
using System.Linq;

/// <summary>
/// 实时模式的控制器。
/// 负责处理基于行动点的选择、移动和攻击逻辑，没有回合限制。
/// </summary>
public class RealTimeModeController : GameModeController
{
    private readonly EnergySystem energySystem;

    // 使用List来存储所有正在移动的棋子，方便每帧更新它们的状态
    private readonly List<PieceComponent> movingPieces = new List<PieceComponent>();

    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
    }

    /// <summary>
    /// 初始化棋盘上所有棋子的实时状态数据。
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
        Debug.Log("实时模式控制器：已为所有棋子初始化实时状态(RTState)。");
    }

    /// <summary>
    /// 由GameManager每帧调用，用于驱动状态更新。
    /// </summary>
    public void Tick()
    {
        UpdateAllPieceStates();
    }

    /// <summary>
    /// 遍历所有正在移动的棋子，并根据规则更新它们的状态。
    /// </summary>
    private void UpdateAllPieceStates()
    {
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
                    if (progress > 0.6f) { state.IsAttacking = true; }
                    if (progress > 0.2f && progress < 0.8f) { state.IsVulnerable = false; }
                    break;
            }
        }
    }

    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // --- Case 1: 已经选中了一个棋子，现在点击的是敌方棋子 ---
        if (selectedPiece != null && clickedPieceData.Color != selectedPiece.PieceData.Color)
        {
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // ================== 防御性代码开始 ==================
                // 增加一个检查，确保selectedPiece和RTState不为null
                if (selectedPiece == null || selectedPiece.RTState == null)
                {
                    Debug.LogError("严重错误：尝试移动一个没有实时状态的棋子！");
                    ClearSelection();
                    return;
                }
                // ================== 防御性代码结束 ==================

                // ================== 检查是否移动中 开始 ==================
                if (selectedPiece.RTState.IsMoving)
                {
                    Debug.Log("该棋子已在移动中，不可操作！");
                    ClearSelection();
                    return;
                }
                // ================== 检查是否移动中 结束 ==================

                if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
                {
                    // ================== 核心修复开始 ==================
                    PlayerColor spentColor = selectedPiece.PieceData.Color;

                    PieceComponent pieceToMove = selectedPiece;
                    pieceToMove.RTState.IsMoving = true;
                    movingPieces.Add(pieceToMove);
                    Debug.Log($"{pieceToMove.name} 开始移动 (吃子)。");

                    // 【重要】调用带有回调函数的 ExecuteMove 版本
                    gameManager.ExecuteMove(
                        pieceToMove.BoardPosition,
                        clickedPiece.BoardPosition,
                        (pc, progress) => {
                            if (pc != null && pc.RTState != null) pc.RTState.MoveProgress = progress;
                        },
                        (pc) => {
                            if (pc != null && pc.RTState != null)
                            {
                                pc.RTState.ResetToDefault();
                                movingPieces.Remove(pc);
                                Debug.Log($"{pc.name} 移动完成 (吃子)，状态已重置。");
                            }
                        }
                    );

                    energySystem.SpendEnergy(spentColor);
                    // ================== 核心修复结束 ==================

                    ClearSelection();
                }
                else
                {
                    Debug.Log("行动点不足，无法吃子！");
                    ClearSelection();
                }
            }
            else
            {
                TrySelectPiece(clickedPiece);
            }
        }
        else
        {
            TrySelectPiece(clickedPiece);
        }
    }

    private void TrySelectPiece(PieceComponent pieceToSelect)
    {
        Piece pieceData = boardState.GetPieceAt(pieceToSelect.BoardPosition);
        if (energySystem.CanSpendEnergy(pieceData.Color))
        {
            SelectPiece(pieceToSelect);
        }
        else
        {
            Debug.Log($"行动点不足！无法选择 {pieceData.Color} 方的棋子。");
            ClearSelection();
        }
    }

    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;


        // 增加同样的null检查，这是最稳妥的做法
        if (selectedPiece.RTState == null)
        {
            Debug.LogError($"严重错误：选中的棋子 {selectedPiece.name} 没有实时状态(RTState)！");
            ClearSelection();
            return;
        }

        if (selectedPiece.RTState.IsMoving)
        {
            Debug.Log("该棋子已在移动中，不可操作！");
            ClearSelection();
            return;
        }

        if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
        {
            PlayerColor spentColor = selectedPiece.PieceData.Color;

            PieceComponent pieceToMove = selectedPiece;
            pieceToMove.RTState.IsMoving = true;
            movingPieces.Add(pieceToMove);
            Debug.Log($"{pieceToMove.name} 开始移动。");

            gameManager.ExecuteMove(
                pieceToMove.BoardPosition,
                marker.BoardPosition,
                (pc, progress) => {
                    if (pc != null && pc.RTState != null) { pc.RTState.MoveProgress = progress; }
                },
                (pc) => {
                    if (pc != null && pc.RTState != null)
                    {
                        pc.RTState.ResetToDefault();
                        movingPieces.Remove(pc);
                        Debug.Log($"{pc.name} 移动完成，状态已重置。");
                    }
                }
            );

            energySystem.SpendEnergy(spentColor);
            ClearSelection();
        }
        else
        {
            Debug.Log("行动点不足，无法移动！");
            ClearSelection();
        }
    }

    public override void OnBoardClicked(RaycastHit hit)
    {
        ClearSelection();
    }
}