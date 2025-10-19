// File: _Scripts/GameModes/TurnBasedModeController.cs
using UnityEngine;

/// <summary>
/// ������������ͳ�غ�����Ϸģʽ�Ŀ�������
/// ����������������߼���
/// </summary>
public class TurnBasedModeController : GameModeController
{
    // ��ǰ�ֵ���һ���ж�
    private PlayerColor currentPlayerTurn = PlayerColor.Red;

    public TurnBasedModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    /// <summary>
    /// �������������������ӵ��߼���
    /// ������߼�˳��������Ҫ������ȷ����ѡ���л�ѡ��͹�����
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        // --- ���һ������Ѿ�ѡ����һ������ ---
        // ��������£��ٴε�����ӣ���ͼ�����ǡ����������л�ѡ�񡱡�
        if (selectedPiece != null)
        {
            // ����µ�������ӣ��Ƿ�����ѡ�����ӵĺϷ�����Ŀ�ꡣ
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // �ǺϷ�Ŀ�ִ꣡���ƶ�/���Ӳ�����
                gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                SwitchTurn(); // ������ɺ��л��غ�
                return; // ���ε��������ϣ�ֱ�ӷ��ء�
            }
        }

        // --- �������ִ�е����˵������һ����Ч�Ĺ������ ---
        // ��ô��ͼ���ǡ�ѡ�񡱻�����ѡ��һ���������ӡ�

        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // ������������Ƿ����ڵ�ǰ�غϷ���
        if (clickedPieceData.Color == currentPlayerTurn)
        {
            // �Ǽ������ӣ�ִ�С�ѡ�񡱲�����
            // SelectPiece�ڲ��ᴦ��������һ��ѡ����߼���
            SelectPiece(clickedPiece);
        }
        else
        {
            // ���������ǵз����ӣ����ֲ��ǺϷ��Ĺ���Ŀ�꣬
            // ��ô�����ֱ���Ĳ���������յ�ǰѡ��
            Debug.Log("����˵з����ӣ������ǺϷ��Ĺ���Ŀ�ꡣȡ��ѡ��");
            ClearSelection();
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
    /// �л��غϣ����������ѡ��״̬��
    /// </summary>
    private void SwitchTurn()
    {
        ClearSelection();
        currentPlayerTurn = (currentPlayerTurn == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        Debug.Log($"�غϽ����������ֵ� {currentPlayerTurn} ���ж���");
        // TODO: ��������Ը���UI����ʾ��ǰ�غϷ�
    }
}