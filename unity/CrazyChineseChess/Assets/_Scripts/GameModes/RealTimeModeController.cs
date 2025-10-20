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
    /// 【已重构】处理实时模式下点击棋子的逻辑
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        // 首先，获取被点击棋子的状态控制器
        var clickedStateController = clickedPiece.GetComponent<PieceStateController>();

        // 检查1：如果点击的棋子正在移动，则忽略本次操作
        if (clickedStateController != null && clickedStateController.IsMoving)
        {
            Debug.Log("该棋子正在移动中，不可操作！");
            // 如果玩家可能想取消当前选择，可以在这里清除
            // ClearSelection(); 
            return;
        }

        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // ------------------------------------------------------------------
        // Case 1: 已经选中了一个己方棋子，现在点击的是一个敌方棋子
        // ------------------------------------------------------------------
        if (selectedPiece != null && clickedPieceData.Color != selectedPiece.PieceData.Color)
        {
            // 检查2：这个敌方棋子是否在我们之前计算出的合法移动/攻击范围内
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // 获取操作方（也就是我们选中的棋子）的颜色
                PlayerColor movingPlayerColor = selectedPiece.PieceData.Color;

                // 检查3：操作方是否有足够的能量
                if (energySystem.CanSpendEnergy(movingPlayerColor))
                {
                    // --- 执行移动 ---
                    // 命令GameManager开始移动，后续的碰撞和吃子将自动发生
                    gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);

                    // 消耗能量
                    energySystem.SpendEnergy(movingPlayerColor);

                    // 操作完成，清除当前的选择状态（高亮、箭头等）
                    ClearSelection();
                }
                else
                {
                    Debug.Log("行动点不足，无法攻击！");
                    ClearSelection(); // 能量不足，也取消选择
                }
            }
            else
            {
                // 如果点击的敌方棋子不在合法移动范围内，
                // 我们将这个行为解释为“玩家想切换选择到这个新点击的棋子”。
                // 但因为它是敌方棋子，我们不能选择它，所以我们只清除当前选择。
                ClearSelection();
            }
            return; // 处理完毕，退出方法
        }

        // ------------------------------------------------------------------
        // Case 2: 点击的是己方棋子，或者是第一次点击（没有已选中的棋子）
        // ------------------------------------------------------------------
        // (这段逻辑和之前类似，主要是选择棋子或切换选择)

        // 如果点击的是已经选中的棋子，则取消选择
        if (selectedPiece == clickedPiece)
        {
            ClearSelection();
        }
        else // 否则，尝试选择这个新点击的己方棋子
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