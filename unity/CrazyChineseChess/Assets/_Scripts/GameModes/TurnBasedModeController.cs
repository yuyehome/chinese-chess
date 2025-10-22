// File: _Scripts/GameModes/TurnBasedModeController.cs

using UnityEngine;

/// <summary>
/// 传统回合制游戏模式的控制器。
/// 实现了经典象棋的轮流走棋逻辑。
/// </summary>
public class TurnBasedModeController : GameModeController
{
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    // --- 新增：选择状态现在由模式本身管理 ---
    private PieceComponent selectedPiece;
    private System.Collections.Generic.List<Vector2Int> currentValidMoves = new System.Collections.Generic.List<Vector2Int>();

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    // --- 方法签名修改：移除 override 关键字，并设为 public ---
    /// <summary>
    /// 处理点击棋子的事件。
    /// </summary>
    public void OnPieceClicked(PieceComponent clickedPiece)
    {
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // 如果点击的是非当前回合方的棋子
        if (clickedPieceData.Color != currentPlayerTurn)
        {
            // 如果已选中己方棋子，且本次点击是合法的吃子目标
            if (selectedPiece != null && currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // 注意：这里不再直接调用gameManager.ExecuteMove
                // 而是调用一个内部方法来处理移动和回合切换
                PerformMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
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
    public void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;

        // 再次确认选中的棋子是否属于当前回合方
        Piece selectedPieceData = boardState.GetPieceAt(selectedPiece.BoardPosition);
        if (selectedPieceData.Color != currentPlayerTurn) return;

        // 如果点击的标记是合法的移动点
        if (currentValidMoves.Contains(marker.BoardPosition))
        {
            PerformMove(selectedPiece.BoardPosition, marker.BoardPosition);
        }
    }

    /// <summary>
    /// 点击棋盘空白处，清空选择。
    /// </summary>
    public void OnBoardClicked()
    {
        ClearSelection();
    }

    // --- 新增：封装移动和回合切换的逻辑 ---
    private void PerformMove(Vector2Int from, Vector2Int to)
    {
        // 移动棋子（模型和视图）
        boardState.MovePiece(from, to);
        boardRenderer.MovePiece(from, to, null, null); // 回合制没有复杂回调

        // 切换回合
        SwitchTurn();
    }

    /// <summary>
    /// 切换回合。
    /// </summary>
    private void SwitchTurn()
    {
        ClearSelection();
        currentPlayerTurn = (currentPlayerTurn == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        Debug.Log($"[TurnBased] 回合结束，现在轮到 {currentPlayerTurn} 方行动。");
    }

    // --- 新增：选择和高亮的相关方法 ---
    private void SelectPiece(PieceComponent piece)
    {
        ClearSelection();
        selectedPiece = piece;

        Piece pieceData = boardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, boardState);

        boardRenderer.ShowValidMoves(currentValidMoves, pieceData.Color, boardState);
        boardRenderer.ShowSelectionMarker(piece.BoardPosition);
    }

    private void ClearSelection()
    {
        selectedPiece = null;
        if (currentValidMoves != null) currentValidMoves.Clear();
        if (boardRenderer != null) boardRenderer.ClearAllHighlights();
    }

    /// <summary>
    /// 获取当前轮到哪一方行动。
    /// </summary>
    public PlayerColor GetCurrentPlayer()
    {
        return currentPlayerTurn;
    }
}