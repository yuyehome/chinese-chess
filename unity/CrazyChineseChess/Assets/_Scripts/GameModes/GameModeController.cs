// File: _Scripts/GameModes/GameModeController.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 所有游戏模式控制器的抽象基类 (Strategy Pattern)。
/// 它定义了所有模式都必须响应的通用输入接口，并提供了共享功能。
/// </summary>
public abstract class GameModeController
{
    // --- 依赖引用 (由子类构造函数注入) ---
    protected BoardState boardState;
    protected BoardRenderer boardRenderer;
    protected GameManager gameManager;

    // --- 共享状态 ---
    // 当前选中的棋子
    protected PieceComponent selectedPiece { get; private set; } = null;
    // 当前选中棋子的合法移动点列表
    protected List<Vector2Int> currentValidMoves = new List<Vector2Int>();

    public GameModeController(GameManager manager, BoardState state, BoardRenderer renderer)
    {
        this.gameManager = manager;
        this.boardState = state;
        this.boardRenderer = renderer;
    }

    #region Abstract Methods (需要子类实现)

    /// <summary>
    /// 当一个棋子被点击时调用。
    /// </summary>
    public abstract void OnPieceClicked(PieceComponent piece);

    /// <summary>
    /// 当一个移动标记被点击时调用。
    /// </summary>
    public abstract void OnMarkerClicked(MoveMarkerComponent marker);

    /// <summary>
    /// 当棋盘的空白区域被点击时调用。
    /// </summary>
    public abstract void OnBoardClicked(RaycastHit hit);

    #endregion

    #region Shared Methods (通用功能)

    /// <summary>
    /// 选中一个棋子，并计算、显示其合法移动点。
    /// </summary>
    protected virtual void SelectPiece(PieceComponent piece)
    {
        ClearSelection();
        selectedPiece = piece;

        // 注意：在实时模式中，这个初始的 valid moves 列表会被 UpdateSelectionHighlights 动态更新。
        // 在回合制模式中，这个列表是最终的。
        Piece pieceData = boardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, boardState);

        boardRenderer.ShowValidMoves(currentValidMoves, pieceData.Color, boardState);
        boardRenderer.ShowSelectionMarker(piece.BoardPosition);
    }

    /// <summary>
    /// 清除当前的选择状态和所有视觉高亮。
    /// </summary>
    protected virtual void ClearSelection()
    {
        selectedPiece = null;
        if (currentValidMoves != null) currentValidMoves.Clear();
        if (boardRenderer != null) boardRenderer.ClearAllHighlights();
    }

    #endregion
}