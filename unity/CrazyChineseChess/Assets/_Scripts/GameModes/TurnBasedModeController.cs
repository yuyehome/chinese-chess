// File: _Scripts/GameModes/TurnBasedModeController.cs
using UnityEngine;

/// <summary>
/// ����������ͳ�غ�����Ϸģʽ�Ŀ�������
/// ����������������߼���
/// </summary>
public class TurnBasedModeController : GameModeController
{
    // ��ǰ�ֵ���һ���ж�
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    public override void OnPieceClicked(PieceComponent piece)
    {
        Piece pieceData = boardState.GetPieceAt(piece.BoardPosition);

        // ����Ƿ��ֵ������ӵ���Ӫ�ж�
        if (pieceData.Color != currentPlayerTurn)
        {
            Debug.Log($"������ {currentPlayerTurn} ���Ļغϣ������ƶ� {pieceData.Color} �������ӡ�");
            return;
        }

        // ����Ѿ������ӱ�ѡ�У�ͨ�����Լ������ӣ�
        if (selectedPiece != null)
        {
            // ����������һ���Ϸ��Ĺ���Ŀ��
            if (currentValidMoves.Contains(piece.BoardPosition))
            {
                gameManager.ExecuteMove(selectedPiece.BoardPosition, piece.BoardPosition);
                SwitchTurn(); // �ƶ����л��غ�
            }
            else // �����л�ѡ�������������
            {
                SelectPiece(piece);
            }
        }
        else // ���֮ǰû�����ӱ�ѡ��
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
            SwitchTurn(); // �ƶ����л��غ�
        }
    }

    public override void OnBoardClicked(RaycastHit hit)
    {
        // ����հ���������ȡ��ѡ��
        ClearSelection();
    }

    /// <summary>
    /// �л��غϡ�
    /// </summary>
    private void SwitchTurn()
    {
        ClearSelection();
        currentPlayerTurn = (currentPlayerTurn == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        Debug.Log($"�غϽ����������ֵ� {currentPlayerTurn} ���ж���");
        // TODO: ��������Ը���UI����ʾ��ǰ�غϷ�
    }
}