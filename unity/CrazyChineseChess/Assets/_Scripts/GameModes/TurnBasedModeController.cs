// File: _Scripts/GameModes/TurnBasedModeController.cs
using UnityEngine;

/// <summary>
/// 【已修正】传统回合制游戏模式的控制器。
/// 负责处理轮流走棋的逻辑。
/// </summary>
public class TurnBasedModeController : GameModeController
{
    // 当前轮到哪一方行动
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    /// <summary>
    /// 【已修正】处理点击棋子的逻辑。
    /// 这里的逻辑顺序至关重要，以正确处理选择、切换选择和攻击。
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        // --- 情况一：玩家已经选中了一个棋子 ---
        // 这种情况下，再次点击棋子，意图可能是“攻击”或“切换选择”。
        if (selectedPiece != null)
        {
            // 检查新点击的棋子，是否是已选中棋子的合法攻击目标。
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // 是合法目标！执行移动/吃子操作。
                gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                SwitchTurn(); // 操作完成后切换回合
                return; // 本次点击处理完毕，直接返回。
            }
        }

        // --- 情况二：执行到这里，说明不是一次有效的攻击点击 ---
        // 那么意图就是“选择”或“重新选择”一个己方棋子。

        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // 检查点击的棋子是否属于当前回合方。
        if (clickedPieceData.Color == currentPlayerTurn)
        {
            // 是己方棋子，执行“选择”操作。
            // SelectPiece内部会处理好清除上一个选择的逻辑。
            SelectPiece(clickedPiece);
        }
        else
        {
            // 如果点击的是敌方棋子，但又不是合法的攻击目标，
            // 那么最符合直觉的操作就是清空当前选择。
            Debug.Log("点击了敌方棋子，但不是合法的攻击目标。取消选择。");
            ClearSelection();
        }
    }

    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;

        if (currentValidMoves.Contains(marker.BoardPosition))
        {
            gameManager.ExecuteMove(selectedPiece.BoardPosition, marker.BoardPosition);
            SwitchTurn(); // 移动后切换回合
        }
    }

    public override void OnBoardClicked(RaycastHit hit)
    {
        // 点击空白区域总是取消选择
        ClearSelection();
    }

    /// <summary>
    /// 切换回合，并清空所有选择状态。
    /// </summary>
    private void SwitchTurn()
    {
        ClearSelection();
        currentPlayerTurn = (currentPlayerTurn == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        Debug.Log($"回合结束，现在轮到 {currentPlayerTurn} 方行动。");
        // TODO: 在这里可以更新UI，提示当前回合方
    }
}