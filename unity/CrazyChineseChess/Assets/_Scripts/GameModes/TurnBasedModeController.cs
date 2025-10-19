// File: _Scripts/GameModes/TurnBasedModeController.cs
using UnityEngine;

/// <summary>
/// 【新增】传统回合制游戏模式的控制器。
/// 负责处理轮流走棋的逻辑。
/// </summary>
public class TurnBasedModeController : GameModeController
{
    // 当前轮到哪一方行动
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    public override void OnPieceClicked(PieceComponent piece)
    {
        Piece pieceData = boardState.GetPieceAt(piece.BoardPosition);

        // 检查是否轮到该棋子的阵营行动
        if (pieceData.Color != currentPlayerTurn)
        {
            Debug.Log($"现在是 {currentPlayerTurn} 方的回合，不能移动 {pieceData.Color} 方的棋子。");
            return;
        }

        // 如果已经有棋子被选中（通常是自己的棋子）
        if (selectedPiece != null)
        {
            // 如果点击的是一个合法的攻击目标
            if (currentValidMoves.Contains(piece.BoardPosition))
            {
                gameManager.ExecuteMove(selectedPiece.BoardPosition, piece.BoardPosition);
                SwitchTurn(); // 移动后切换回合
            }
            else // 否则，切换选择到这个新棋子上
            {
                SelectPiece(piece);
            }
        }
        else // 如果之前没有棋子被选中
        {
            SelectPiece(piece);
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
    /// 切换回合。
    /// </summary>
    private void SwitchTurn()
    {
        ClearSelection();
        currentPlayerTurn = (currentPlayerTurn == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        Debug.Log($"回合结束，现在轮到 {currentPlayerTurn} 方行动。");
        // TODO: 在这里可以更新UI，提示当前回合方
    }
}