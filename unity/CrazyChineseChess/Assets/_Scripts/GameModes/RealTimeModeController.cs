// File: _Scripts/GameModes/RealTimeModeController.cs

using UnityEngine;

/// <summary>
/// ʵʱģʽ�Ŀ�������
/// ����������ж����ѡ���ƶ��͹����߼���û�лغ����ơ�
/// </summary>
public class RealTimeModeController : GameModeController
{
    // ����ע�������ϵͳ
    private readonly EnergySystem energySystem;

    // ���캯������Ҫ����GameManager�����EnergySystem
    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
    }

    /// <summary>
    /// ���������ӵ��߼�
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // --- Case 1: �Ѿ�ѡ����һ�����ӣ����ڵ�����ǵз����� ---
        if (selectedPiece != null && clickedPieceData.Color != selectedPiece.PieceData.Color)
        {
            // �������з������Ƿ��ںϷ��ƶ��б�������Ա��Ե���
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // �ٴμ����������ֹ��ѡ�к͵���ļ�϶�����ľ�
                if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
                {
                    PlayerColor spentColor = selectedPiece.PieceData.Color; // ����������¼��ɫ
                    // ִ���ƶ�����������
                    gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                    energySystem.SpendEnergy(selectedPiece.PieceData.Color);
                    ClearSelection();
                }
                else
                {
                    Debug.Log("�ж��㲻�㣬�޷����ӣ�");
                    ClearSelection(); // �������㣬ȡ��֮ǰ��ѡ��
                }
            }
            else
            {
                // ����˷Ƿ��ĵз����ӣ���Ϊ���л�ѡ����Case 2���߼�
                TrySelectPiece(clickedPiece);
            }
        }
        // --- Case 2: ������Ǽ������ӣ���û��ѡ���κ����� ---
        else
        {
            TrySelectPiece(clickedPiece);
        }
    }

    /// <summary>
    /// ����ѡ��һ�����ӡ�
    /// </summary>
    private void TrySelectPiece(PieceComponent pieceToSelect)
    {
        Piece pieceData = boardState.GetPieceAt(pieceToSelect.BoardPosition);

        // ��������Ƿ��㹻�ԡ�׼�����ƶ�
        if (energySystem.CanSpendEnergy(pieceData.Color))
        {
            // ��������㹻����ִ��ѡ���������ʾ�Ϸ��ƶ���
            SelectPiece(pieceToSelect);
        }
        else
        {
            // �������㣬��ʾ��Ҳ�����κ�֮ǰ��ѡ��
            Debug.Log($"�ж��㲻�㣡�޷�ѡ�� {pieceData.Color} �������ӡ�");
            ClearSelection();
        }
    }

    /// <summary>
    /// �������ƶ���ǵ��߼�
    /// </summary>
    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;

        // �ٴμ������
        if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
        {
            PlayerColor spentColor = selectedPiece.PieceData.Color; // ����������¼��ɫ
            // ִ���ƶ�����������
            gameManager.ExecuteMove(selectedPiece.BoardPosition, marker.BoardPosition);
            energySystem.SpendEnergy(selectedPiece.PieceData.Color);
            ClearSelection();
        }
        else
        {
            Debug.Log("�ж��㲻�㣬�޷��ƶ���");
            ClearSelection();
        }
    }

    /// <summary>
    /// ������̿հ״������ѡ��
    /// </summary>
    public override void OnBoardClicked(RaycastHit hit)
    {
        ClearSelection();
    }
}