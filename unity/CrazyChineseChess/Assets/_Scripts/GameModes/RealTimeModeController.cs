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
    /// �����ع�������ʵʱģʽ�µ�����ӵ��߼�
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        // ���ȣ���ȡ��������ӵ�״̬������
        var clickedStateController = clickedPiece.GetComponent<PieceStateController>();

        // ���1�������������������ƶ�������Ա��β���
        if (clickedStateController != null && clickedStateController.IsMoving)
        {
            Debug.Log("�����������ƶ��У����ɲ�����");
            // �����ҿ�����ȡ����ǰѡ�񣬿������������
            // ClearSelection(); 
            return;
        }

        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // ------------------------------------------------------------------
        // Case 1: �Ѿ�ѡ����һ���������ӣ����ڵ������һ���з�����
        // ------------------------------------------------------------------
        if (selectedPiece != null && clickedPieceData.Color != selectedPiece.PieceData.Color)
        {
            // ���2������з������Ƿ�������֮ǰ������ĺϷ��ƶ�/������Χ��
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // ��ȡ��������Ҳ��������ѡ�е����ӣ�����ɫ
                PlayerColor movingPlayerColor = selectedPiece.PieceData.Color;

                // ���3���������Ƿ����㹻������
                if (energySystem.CanSpendEnergy(movingPlayerColor))
                {
                    // --- ִ���ƶ� ---
                    // ����GameManager��ʼ�ƶ�����������ײ�ͳ��ӽ��Զ�����
                    gameManager.ExecuteMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);

                    // ��������
                    energySystem.SpendEnergy(movingPlayerColor);

                    // ������ɣ������ǰ��ѡ��״̬����������ͷ�ȣ�
                    ClearSelection();
                }
                else
                {
                    Debug.Log("�ж��㲻�㣬�޷�������");
                    ClearSelection(); // �������㣬Ҳȡ��ѡ��
                }
            }
            else
            {
                // �������ĵз����Ӳ��ںϷ��ƶ���Χ�ڣ�
                // ���ǽ������Ϊ����Ϊ��������л�ѡ������µ�������ӡ���
                // ����Ϊ���ǵз����ӣ����ǲ���ѡ��������������ֻ�����ǰѡ��
                ClearSelection();
            }
            return; // ������ϣ��˳�����
        }

        // ------------------------------------------------------------------
        // Case 2: ������Ǽ������ӣ������ǵ�һ�ε����û����ѡ�е����ӣ�
        // ------------------------------------------------------------------
        // (����߼���֮ǰ���ƣ���Ҫ��ѡ�����ӻ��л�ѡ��)

        // �����������Ѿ�ѡ�е����ӣ���ȡ��ѡ��
        if (selectedPiece == clickedPiece)
        {
            ClearSelection();
        }
        else // ���򣬳���ѡ������µ���ļ�������
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