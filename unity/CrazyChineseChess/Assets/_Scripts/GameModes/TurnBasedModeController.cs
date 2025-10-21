// File: _Scripts/GameModes/TurnBasedMode-Controller.cs

using UnityEngine;

/// <summary>
/// 传统回合制游戏模式的控制器。
/// 实现了经典象棋的轮流走棋逻辑。
/// </summary>
public class TurnBasedModeController : GameModeController
{
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    /// <summary>
    /// 处理点击棋子的事件。
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // 如果点击的是非当前回合方的棋子
        if (clickedPieceData.Color != currentPlayerTurn)
        {
            // 如果已选中己方棋子，且本次点击是合法的吃子目标
            if (selectedPiece != null && currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                SwitchTurn();
            }
            else
            {
                // 否则，视为无效操作，清空选择
                ClearSelection();
            }
            return;
        }

        // 如果点击的是当前回合方的棋子，则执行选择/切换选择操作
        SelectPiece(clickedPiece);
    }

    /// <summary>
    /// 处理点击移动标记的事件。
    /// </summary>
    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;

        // 再次确认选中的棋子是否属于当前回合方
        Piece selectedPieceData = boardState.GetPieceAt(selectedPiece.BoardPosition);
        if (selectedPieceData.Color != currentPlayerTurn) return;

        // 如果点击的标记是合法的移动点
        if (currentValidMoves.Contains(marker.BoardPosition))
        {
            gameManager.ExecuteMove(selectedPiece.BoardPosition, marker.BoardPosition);
            SwitchTurn();
        }
    }

    /// <summary>
    /// 点击棋盘空白处，清空选择。
    /// </summary>
    public override void OnBoardClicked(RaycastHit hit)
    {
        ClearSelection();
    }

    /// <summary>
    /// 切换回合。
    /// </summary>
    private void SwitchTurn()
    {
        ClearSelection();
        currentPlayerTurn = (currentPlayerTurn == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        Debug.Log($"回合结束，现在轮到 {currentPlayerTurn} 方行动。");
    }

    /// <summary>
    /// 获取当前轮到哪一方行动。
    /// </summary>
    public PlayerColor GetCurrentPlayer()
    {
        return currentPlayerTurn;
    }
}