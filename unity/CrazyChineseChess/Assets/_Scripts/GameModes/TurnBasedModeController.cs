// File: _Scripts/GameModes/TurnBasedModeController.cs
using UnityEngine;

/// <summary>
/// ������������ͳ�غ�����Ϸģʽ�Ŀ�������
/// �Ƴ��ˡ�����⽫����ǿ���߼�����������������塣
/// </summary>
public class TurnBasedModeController : GameModeController
{
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        // ������������Ƿ����ڵ�ǰ�غϷ�
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);
        if (clickedPieceData.Color != currentPlayerTurn)
        {
            // ����Ѿ������ӱ�ѡ�У����ҵ���ĵз������ǺϷ�����Ŀ��
            if (selectedPiece != null && currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                SwitchTurn();
            }
            else
            {
                // �������˷ǵ�ǰ�غϷ������ӣ��Ҳ���Ϊ�˳��ӣ�����Ӧ�����ѡ��
                ClearSelection();
            }
            return;
        }

        // ���������Ǽ������ӣ���ִ��ѡ��/�л�ѡ�����
        SelectPiece(clickedPiece);
    }

    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;

        // ����Ƿ��ֵ��������ж�
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
        Debug.Log($"�غϽ����������ֵ� {currentPlayerTurn} ���ж���");
    }

    public PlayerColor GetCurrentPlayer()
    {
        return currentPlayerTurn;
    }
}