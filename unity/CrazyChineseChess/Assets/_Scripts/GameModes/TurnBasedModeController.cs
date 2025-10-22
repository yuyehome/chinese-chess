// File: _Scripts/GameModes/TurnBasedModeController.cs

using UnityEngine;

/// <summary>
/// ��ͳ�غ�����Ϸģʽ�Ŀ�������
/// ʵ���˾�����������������߼���
/// </summary>
public class TurnBasedModeController : GameModeController
{
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    // --- ������ѡ��״̬������ģʽ������� ---
    private PieceComponent selectedPiece;
    private System.Collections.Generic.List<Vector2Int> currentValidMoves = new System.Collections.Generic.List<Vector2Int>();

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    // --- ����ǩ���޸ģ��Ƴ� override �ؼ��֣�����Ϊ public ---
    /// <summary>
    /// ���������ӵ��¼���
    /// </summary>
    public void OnPieceClicked(PieceComponent clickedPiece)
    {
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // ���������Ƿǵ�ǰ�غϷ�������
        if (clickedPieceData.Color != currentPlayerTurn)
        {
            // �����ѡ�м������ӣ��ұ��ε���ǺϷ��ĳ���Ŀ��
            if (selectedPiece != null && currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // ע�⣺���ﲻ��ֱ�ӵ���gameManager.ExecuteMove
                // ���ǵ���һ���ڲ������������ƶ��ͻغ��л�
                PerformMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
            }
            else
            {
                // ������Ϊ��Ч���������ѡ��
                ClearSelection();
            }
            return;
        }

        // ���������ǵ�ǰ�غϷ������ӣ���ִ��ѡ��/�л�ѡ�����
        SelectPiece(clickedPiece);
    }

    /// <summary>
    /// �������ƶ���ǵ��¼���
    /// </summary>
    public void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;

        // �ٴ�ȷ��ѡ�е������Ƿ����ڵ�ǰ�غϷ�
        Piece selectedPieceData = boardState.GetPieceAt(selectedPiece.BoardPosition);
        if (selectedPieceData.Color != currentPlayerTurn) return;

        // �������ı���ǺϷ����ƶ���
        if (currentValidMoves.Contains(marker.BoardPosition))
        {
            PerformMove(selectedPiece.BoardPosition, marker.BoardPosition);
        }
    }

    /// <summary>
    /// ������̿հ״������ѡ��
    /// </summary>
    public void OnBoardClicked()
    {
        ClearSelection();
    }

    // --- ��������װ�ƶ��ͻغ��л����߼� ---
    private void PerformMove(Vector2Int from, Vector2Int to)
    {
        // �ƶ����ӣ�ģ�ͺ���ͼ��
        boardState.MovePiece(from, to);
        boardRenderer.MovePiece(from, to, null, null); // �غ���û�и��ӻص�

        // �л��غ�
        SwitchTurn();
    }

    /// <summary>
    /// �л��غϡ�
    /// </summary>
    private void SwitchTurn()
    {
        ClearSelection();
        currentPlayerTurn = (currentPlayerTurn == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        Debug.Log($"[TurnBased] �غϽ����������ֵ� {currentPlayerTurn} ���ж���");
    }

    // --- ������ѡ��͸�������ط��� ---
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
    /// ��ȡ��ǰ�ֵ���һ���ж���
    /// </summary>
    public PlayerColor GetCurrentPlayer()
    {
        return currentPlayerTurn;
    }
}