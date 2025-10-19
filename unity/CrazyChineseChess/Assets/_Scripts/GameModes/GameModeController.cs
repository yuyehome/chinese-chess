// File: _Scripts/GameModes/GameModeController.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【新增】所有游戏模式控制器的抽象基类。
/// 它定义了游戏模式的核心行为接口，并提供了一些所有模式都可能用到的通用功能。
/// </summary>
public abstract class GameModeController
{
    // 保护成员变量，子类可以直接访问
    protected BoardState boardState;
    protected BoardRenderer boardRenderer;
    protected GameManager gameManager;

    // 当前选中的棋子和其合法移动
    protected PieceComponent selectedPiece = null;
    protected List<Vector2Int> currentValidMoves = new List<Vector2Int>();

    /// <summary>
    /// 构造函数，在创建实例时注入必要的依赖项。
    /// </summary>
    public GameModeController(GameManager manager, BoardState state, BoardRenderer renderer)
    {
        this.gameManager = manager;
        this.boardState = state;
        this.boardRenderer = renderer;
    }

    // --- 需要子类具体实现的抽象方法 ---

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

    // --- 所有模式共享的通用方法 ---

    /// <summary>
    /// 选中一个棋子并计算、显示其合法移动。
    /// </summary>
    protected virtual void SelectPiece(PieceComponent piece)
    {
        ClearSelection(); // 先清除旧的选择
        selectedPiece = piece;

        Piece pieceData = boardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, boardState);

        boardRenderer.ShowValidMoves(currentValidMoves, pieceData.Color, boardState);
    }

    /// <summary>
    /// 清除当前的选择状态和所有高亮。
    /// </summary>
    protected virtual void ClearSelection()
    {
        selectedPiece = null;
        if (currentValidMoves != null) currentValidMoves.Clear();
        if (boardRenderer != null) boardRenderer.ClearAllHighlights();
    }
}