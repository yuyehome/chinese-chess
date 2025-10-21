// File: _Scripts/GameModes/TurnBasedMode-Controller.cs

using UnityEngine;

/// <summary>
/// ��ͳ�غ�����Ϸģʽ�Ŀ�������
/// ʵ���˾�����������������߼���
/// </summary>
public class TurnBasedModeController : GameModeController
{
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    /// <summary>
    /// ���������ӵ��¼���
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // ���������Ƿǵ�ǰ�غϷ�������
        if (clickedPieceData.Color != currentPlayerTurn)
        {
            // �����ѡ�м������ӣ��ұ��ε���ǺϷ��ĳ���Ŀ��
            if (selectedPiece != null && currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                SwitchTurn();
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
    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;

        // �ٴ�ȷ��ѡ�е������Ƿ����ڵ�ǰ�غϷ�
        Piece selectedPieceData = boardState.GetPieceAt(selectedPiece.BoardPosition);
        if (selectedPieceData.Color != currentPlayerTurn) return;

        // �������ı���ǺϷ����ƶ���
        if (currentValidMoves.Contains(marker.BoardPosition))
        {
            gameManager.ExecuteMove(selectedPiece.BoardPosition, marker.BoardPosition);
            SwitchTurn();
        }
    }

    /// <summary>
    /// ������̿հ״������ѡ��
    /// </summary>
    public override void OnBoardClicked(RaycastHit hit)
    {
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
    }

    /// <summary>
    /// ��ȡ��ǰ�ֵ���һ���ж���
    /// </summary>
    public PlayerColor GetCurrentPlayer()
    {
        return currentPlayerTurn;
    }
}