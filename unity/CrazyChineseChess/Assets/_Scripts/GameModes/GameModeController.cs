// File: _Scripts/GameModes/GameModeController.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// ��������������Ϸģʽ�������ĳ�����ࡣ
/// ����������Ϸģʽ�ĺ�����Ϊ�ӿڣ����ṩ��һЩ����ģʽ�������õ���ͨ�ù��ܡ�
/// </summary>
public abstract class GameModeController
{
    // ������Ա�������������ֱ�ӷ���
    protected BoardState boardState;
    protected BoardRenderer boardRenderer;
    protected GameManager gameManager;

    // ��ǰѡ�е����Ӻ���Ϸ��ƶ�
    protected PieceComponent selectedPiece = null;
    protected List<Vector2Int> currentValidMoves = new List<Vector2Int>();

    /// <summary>
    /// ���캯�����ڴ���ʵ��ʱע���Ҫ�������
    /// </summary>
    public GameModeController(GameManager manager, BoardState state, BoardRenderer renderer)
    {
        this.gameManager = manager;
        this.boardState = state;
        this.boardRenderer = renderer;
    }

    // --- ��Ҫ�������ʵ�ֵĳ��󷽷� ---

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

    // --- ����ģʽ�����ͨ�÷��� ---

    /// <summary>
    /// ѡ��һ�����Ӳ����㡢��ʾ��Ϸ��ƶ���
    /// </summary>
    protected virtual void SelectPiece(PieceComponent piece)
    {
        ClearSelection(); // ������ɵ�ѡ��
        selectedPiece = piece;

        Piece pieceData = boardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, boardState);

        boardRenderer.ShowValidMoves(currentValidMoves, pieceData.Color, boardState);
    }

    /// <summary>
    /// �����ǰ��ѡ��״̬�����и�����
    /// </summary>
    protected virtual void ClearSelection()
    {
        selectedPiece = null;
        if (currentValidMoves != null) currentValidMoves.Clear();
        if (boardRenderer != null) boardRenderer.ClearAllHighlights();
    }
}