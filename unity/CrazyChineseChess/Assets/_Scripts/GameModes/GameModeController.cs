// File: _Scripts/GameModes/GameModeController.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ������Ϸģʽ�������ĳ������ (Strategy Pattern)��
/// ������������ģʽ��������Ӧ��ͨ������ӿڣ����ṩ�˹����ܡ�
/// </summary>
public abstract class GameModeController
{
    // --- �������� (�����๹�캯��ע��) ---
    protected BoardState boardState;
    protected BoardRenderer boardRenderer;
    protected GameManager gameManager;

    // --- ����״̬ ---
    // ��ǰѡ�е�����
    protected PieceComponent selectedPiece { get; private set; } = null;
    // ��ǰѡ�����ӵĺϷ��ƶ����б�
    protected List<Vector2Int> currentValidMoves = new List<Vector2Int>();

    public GameModeController(GameManager manager, BoardState state, BoardRenderer renderer)
    {
        this.gameManager = manager;
        this.boardState = state;
        this.boardRenderer = renderer;
    }

    #region Abstract Methods (��Ҫ����ʵ��)

    /// <summary>
    /// ��һ�����ӱ����ʱ���á�
    /// </summary>
    public abstract void OnPieceClicked(PieceComponent piece);

    /// <summary>
    /// ��һ���ƶ���Ǳ����ʱ���á�
    /// </summary>
    public abstract void OnMarkerClicked(MoveMarkerComponent marker);

    /// <summary>
    /// �����̵Ŀհ����򱻵��ʱ���á�
    /// </summary>
    public abstract void OnBoardClicked(RaycastHit hit);

    #endregion

    #region Shared Methods (ͨ�ù���)

    /// <summary>
    /// ѡ��һ�����ӣ������㡢��ʾ��Ϸ��ƶ��㡣
    /// </summary>
    protected virtual void SelectPiece(PieceComponent piece)
    {
        ClearSelection();
        selectedPiece = piece;

        // ע�⣺��ʵʱģʽ�У������ʼ�� valid moves �б�ᱻ UpdateSelectionHighlights ��̬���¡�
        // �ڻغ���ģʽ�У�����б������յġ�
        Piece pieceData = boardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, boardState);

        boardRenderer.ShowValidMoves(currentValidMoves, pieceData.Color, boardState);
        boardRenderer.ShowSelectionMarker(piece.BoardPosition);
    }

    /// <summary>
    /// �����ǰ��ѡ��״̬�������Ӿ�������
    /// </summary>
    protected virtual void ClearSelection()
    {
        selectedPiece = null;
        if (currentValidMoves != null) currentValidMoves.Clear();
        if (boardRenderer != null) boardRenderer.ClearAllHighlights();
    }

    #endregion
}