// File: _Scripts/GameModes/TurnBasedModeController.cs
using UnityEngine;

/// <summary>
/// 【已修正】传统回合制游戏模式的控制器。
/// 移除了“必须解将”的强制逻辑，允许玩家自由行棋。
/// </summary>
public class TurnBasedModeController : GameModeController
{
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        // 检查点击的棋子是否属于当前回合方
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);
        if (clickedPieceData.Color != currentPlayerTurn)
        {
            // 如果已经有棋子被选中，并且点击的敌方棋子是合法攻击目标
            if (selectedPiece != null && currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                SwitchTurn();
            }
            else
            {
                // 如果点击了非当前回合方的棋子，且不是为了吃子，则不响应或清空选择
                ClearSelection();
            }
            return;
        }

        // 如果点击的是己方棋子，则执行选择/切换选择操作
        SelectPiece(clickedPiece);
    }

    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;

        // 检查是否轮到该棋子行动
        Piece selectedPieceData = boardState.GetPieceAt(selectedPiece.BoardPosition);
        if (selectedPieceData.Color != currentPlayerTurn) return;

        if (currentValidMoves.Contains(marker.BoardPosition))
        {
            gameManager.ExecuteMove(selectedPiece.BoardPosition, marker.BoardPosition);
            SwitchTurn();
        }
    }

    public override void OnBoardClicked(RaycastHit hit)
    {
        ClearSelection();
    }

    private void SwitchTurn()
    {
        ClearSelection();
        currentPlayerTurn = (currentPlayerTurn == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        Debug.Log($"回合结束，现在轮到 {currentPlayerTurn} 方行动。");
    }

    public PlayerColor GetCurrentPlayer()
    {
        return currentPlayerTurn;
    }
}