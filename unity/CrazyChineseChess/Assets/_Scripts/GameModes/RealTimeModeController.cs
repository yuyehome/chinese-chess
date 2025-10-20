// File: _Scripts/GameModes/RealTimeModeController.cs

using UnityEngine;
using System.Collections.Generic; // <--- ��������һ��
using System.Linq;

/// <summary>
/// ʵʱģʽ�Ŀ�������
/// ����������ж����ѡ���ƶ��͹����߼���û�лغ����ơ�
/// </summary>
public class RealTimeModeController : GameModeController
{
    private readonly EnergySystem energySystem;

    // ʹ��List���洢���������ƶ������ӣ�����ÿ֡�������ǵ�״̬
    private readonly List<PieceComponent> movingPieces = new List<PieceComponent>();

    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
    }

    /// <summary>
    /// ��ʼ���������������ӵ�ʵʱ״̬���ݡ�
    /// </summary>
    public void InitializeRealTimeStates()
    {
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (boardState.GetPieceAt(pos).Type != PieceType.None)
                {
                    PieceComponent pc = boardRenderer.GetPieceComponentAt(pos);
                    if (pc != null)
                    {
                        pc.RTState = new RealTimePieceState();
                    }
                }
            }
        }
        Debug.Log("ʵʱģʽ����������Ϊ�������ӳ�ʼ��ʵʱ״̬(RTState)��");
    }

    /// <summary>
    /// ��GameManagerÿ֡���ã���������״̬���¡�
    /// </summary>
    public void Tick()
    {
        UpdateAllPieceStates();
    }

    /// <summary>
    /// �������������ƶ������ӣ������ݹ���������ǵ�״̬��
    /// </summary>
    private void UpdateAllPieceStates()
    {
        for (int i = movingPieces.Count - 1; i >= 0; i--)
        {
            PieceComponent pc = movingPieces[i];
            if (pc == null || pc.RTState == null || pc.RTState.IsDead)
            {
                movingPieces.RemoveAt(i);
                continue;
            }

            RealTimePieceState state = pc.RTState;
            PieceType type = pc.PieceData.Type;
            float progress = state.MoveProgress;

            state.IsAttacking = false;
            state.IsVulnerable = true;

            switch (type)
            {
                case PieceType.Chariot:
                case PieceType.Soldier:
                case PieceType.General:
                case PieceType.Advisor:
                    state.IsAttacking = true;
                    state.IsVulnerable = true;
                    break;
                case PieceType.Cannon:
                    if (progress > 0.9f) { state.IsAttacking = true; }
                    state.IsVulnerable = true;
                    break;
                case PieceType.Horse:
                case PieceType.Elephant:
                    if (progress > 0.6f) { state.IsAttacking = true; }
                    if (progress > 0.2f && progress < 0.8f) { state.IsVulnerable = false; }
                    break;
            }
        }
    }

    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // --- Case 1: �Ѿ�ѡ����һ�����ӣ����ڵ�����ǵз����� ---
        if (selectedPiece != null && clickedPieceData.Color != selectedPiece.PieceData.Color)
        {
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // ================== �����Դ��뿪ʼ ==================
                // ����һ����飬ȷ��selectedPiece��RTState��Ϊnull
                if (selectedPiece == null || selectedPiece.RTState == null)
                {
                    Debug.LogError("���ش��󣺳����ƶ�һ��û��ʵʱ״̬�����ӣ�");
                    ClearSelection();
                    return;
                }
                // ================== �����Դ������ ==================

                // ================== ����Ƿ��ƶ��� ��ʼ ==================
                if (selectedPiece.RTState.IsMoving)
                {
                    Debug.Log("�����������ƶ��У����ɲ�����");
                    ClearSelection();
                    return;
                }
                // ================== ����Ƿ��ƶ��� ���� ==================

                if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
                {
                    // ================== �����޸���ʼ ==================
                    PlayerColor spentColor = selectedPiece.PieceData.Color;

                    PieceComponent pieceToMove = selectedPiece;
                    pieceToMove.RTState.IsMoving = true;
                    movingPieces.Add(pieceToMove);
                    Debug.Log($"{pieceToMove.name} ��ʼ�ƶ� (����)��");

                    // ����Ҫ�����ô��лص������� ExecuteMove �汾
                    gameManager.ExecuteMove(
                        pieceToMove.BoardPosition,
                        clickedPiece.BoardPosition,
                        (pc, progress) => {
                            if (pc != null && pc.RTState != null) pc.RTState.MoveProgress = progress;
                        },
                        (pc) => {
                            if (pc != null && pc.RTState != null)
                            {
                                pc.RTState.ResetToDefault();
                                movingPieces.Remove(pc);
                                Debug.Log($"{pc.name} �ƶ���� (����)��״̬�����á�");
                            }
                        }
                    );

                    energySystem.SpendEnergy(spentColor);
                    // ================== �����޸����� ==================

                    ClearSelection();
                }
                else
                {
                    Debug.Log("�ж��㲻�㣬�޷����ӣ�");
                    ClearSelection();
                }
            }
            else
            {
                TrySelectPiece(clickedPiece);
            }
        }
        else
        {
            TrySelectPiece(clickedPiece);
        }
    }

    private void TrySelectPiece(PieceComponent pieceToSelect)
    {
        Piece pieceData = boardState.GetPieceAt(pieceToSelect.BoardPosition);
        if (energySystem.CanSpendEnergy(pieceData.Color))
        {
            SelectPiece(pieceToSelect);
        }
        else
        {
            Debug.Log($"�ж��㲻�㣡�޷�ѡ�� {pieceData.Color} �������ӡ�");
            ClearSelection();
        }
    }

    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        if (selectedPiece == null) return;


        // ����ͬ����null��飬���������׵�����
        if (selectedPiece.RTState == null)
        {
            Debug.LogError($"���ش���ѡ�е����� {selectedPiece.name} û��ʵʱ״̬(RTState)��");
            ClearSelection();
            return;
        }

        if (selectedPiece.RTState.IsMoving)
        {
            Debug.Log("�����������ƶ��У����ɲ�����");
            ClearSelection();
            return;
        }

        if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
        {
            PlayerColor spentColor = selectedPiece.PieceData.Color;

            PieceComponent pieceToMove = selectedPiece;
            pieceToMove.RTState.IsMoving = true;
            movingPieces.Add(pieceToMove);
            Debug.Log($"{pieceToMove.name} ��ʼ�ƶ���");

            gameManager.ExecuteMove(
                pieceToMove.BoardPosition,
                marker.BoardPosition,
                (pc, progress) => {
                    if (pc != null && pc.RTState != null) { pc.RTState.MoveProgress = progress; }
                },
                (pc) => {
                    if (pc != null && pc.RTState != null)
                    {
                        pc.RTState.ResetToDefault();
                        movingPieces.Remove(pc);
                        Debug.Log($"{pc.name} �ƶ���ɣ�״̬�����á�");
                    }
                }
            );

            energySystem.SpendEnergy(spentColor);
            ClearSelection();
        }
        else
        {
            Debug.Log("�ж��㲻�㣬�޷��ƶ���");
            ClearSelection();
        }
    }

    public override void OnBoardClicked(RaycastHit hit)
    {
        ClearSelection();
    }
}