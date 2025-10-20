// File: _Scripts/GameModes/RealTimeModeController.cs

using UnityEngine;

/// <summary>
/// 实时模式的控制器。
/// 负责处理基于行动点的选择、移动和攻击逻辑，没有回合限制。
/// </summary>
public class RealTimeModeController : GameModeController
{
    // 依赖注入的能量系统
    private readonly EnergySystem energySystem;

    // 构造函数，需要接收GameManager传入的EnergySystem
    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
    }

    /// <summary>
    /// 处理点击棋子的逻辑
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // --- Case 1: 已经选中了一个棋子，现在点击的是敌方棋子 ---
        if (selectedPiece != null && clickedPieceData.Color != selectedPiece.PieceData.Color)
        {
            // 检查这个敌方棋子是否在合法移动列表里（即可以被吃掉）
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // 再次检查能量，防止在选中和点击的间隙能量耗尽
                if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
                {
                    PlayerColor spentColor = selectedPiece.PieceData.Color; // 【新增】记录颜色
                    // 执行移动并消耗能量
                    gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                    energySystem.SpendEnergy(selectedPiece.PieceData.Color);
                    ClearSelection();
                }
                else
                {
                    Debug.Log("行动点不足，无法吃子！");
                    ClearSelection(); // 能量不足，取消之前的选择
                }
            }
            else
            {
                // 点击了非法的敌方棋子，视为想切换选择，走Case 2的逻辑
                TrySelectPiece(clickedPiece);
            }
        }
        // --- Case 2: 点击的是己方棋子，或没有选中任何棋子 ---
        else
        {
            TrySelectPiece(clickedPiece);
        }
    }

    /// <summary>
    /// 尝试选择一个棋子。
    /// </summary>
    private void TrySelectPiece(PieceComponent pieceToSelect)
    {
        Piece pieceData = boardState.GetPieceAt(pieceToSelect.BoardPosition);

        // 检查能量是否足够以“准备”移动
        if (energySystem.CanSpendEnergy(pieceData.Color))
        {
            // 如果能量足够，则执行选择操作（显示合法移动）
            SelectPiece(pieceToSelect);
        }
        else
        {
            // 能量不足，提示玩家并清空任何之前的选择
            Debug.Log($"行动点不足！无法选择 {pieceData.Color} 方的棋子。");
            ClearSelection();
        }
    }

    /// <summary>
    /// 处理点击移动标记的逻辑
    /// </summary>
    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;

        // 再次检查能量
        if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
        {
            PlayerColor spentColor = selectedPiece.PieceData.Color; // 【新增】记录颜色
            // 执行移动并消耗能量
            gameManager.ExecuteMove(selectedPiece.BoardPosition, marker.BoardPosition);
            energySystem.SpendEnergy(selectedPiece.PieceData.Color);
            ClearSelection();
        }
        else
        {
            Debug.Log("行动点不足，无法移动！");
            ClearSelection();
        }
    }

    /// <summary>
    /// 点击棋盘空白处，清空选择
    /// </summary>
    public override void OnBoardClicked(RaycastHit hit)
    {
        ClearSelection();
    }
}