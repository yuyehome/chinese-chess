// File: _Scripts/Controllers/PlayerInputController.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// �������������Ŀ�������
/// ������Ӧ���������������ѡ��͸���������GameManager�ύ�ƶ�����
/// </summary>
public class PlayerInputController : MonoBehaviour, IPlayerController
{
    // --- �ڲ�״̬ ---
    private PlayerColor assignedColor;
    private GameManager gameManager;
    private BoardRenderer boardRenderer;
    private Camera mainCamera;

    private PieceComponent selectedPiece;
    private List<Vector2Int> currentValidMoves = new List<Vector2Int>();
    private List<Vector2Int> lastCalculatedValidMoves = new List<Vector2Int>();

    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
        this.boardRenderer = gameManager.BoardRenderer; // ��GameManager��ȡ����
        this.mainCamera = Camera.main;
        Debug.Log($"[InputController] ��������������Ϊ {assignedColor} ����ʼ����");
    }

    private void Update()
    {
        // ���δ��ʼ������Ϸ��������ִ���κβ���
        if (gameManager == null || gameManager.IsGameEnded) return;

        // ʵʱ����ѡ�����ӵĸ���������̬�ڼܵ������
        UpdateSelectionHighlights();

        // �������������
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            PieceComponent clickedPiece = hit.collider.GetComponent<PieceComponent>();
            MoveMarkerComponent clickedMarker = hit.collider.GetComponent<MoveMarkerComponent>();

            if (clickedPiece != null)
            {
                OnPieceClicked(clickedPiece);
            }
            else if (clickedMarker != null)
            {
                OnMarkerClicked(clickedMarker);
            }
            else
            {
                OnBoardClicked();
            }
        }
    }

    private void OnPieceClicked(PieceComponent clickedPiece)
    {
        Debug.Log($"[Input] ��ҵ��������: {clickedPiece.name}");

        // ������Ǽ�������
        if (clickedPiece.PieceData.Color == assignedColor)
        {
            TrySelectPiece(clickedPiece);
        }
        // ������ǵз�����
        else
        {
            // �����ѡ�м������ӣ��ұ��ε���ǺϷ��ĳ���Ŀ��
            if (selectedPiece != null && currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // �ύ�ƶ�����
                gameManager.RequestMove(assignedColor, selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                ClearSelection();
            }
            else
            {
                // ������Ϊ��Ч���������ѡ��
                ClearSelection();
            }
        }
    }

    private void OnMarkerClicked(MoveMarkerComponent marker)
    {
        Debug.Log($"[Input] ��ҵ�����ƶ���ǣ�Ŀ������: {marker.BoardPosition}");
        if (selectedPiece != null && currentValidMoves.Contains(marker.BoardPosition))
        {
            // �ύ�ƶ�����
            gameManager.RequestMove(assignedColor, selectedPiece.BoardPosition, marker.BoardPosition);
            ClearSelection();
        }
    }

    private void OnBoardClicked()
    {
        Debug.Log("[Input] ��ҵ�������̿հ�����ȡ��ѡ��");
        ClearSelection();
    }

    private void TrySelectPiece(PieceComponent pieceToSelect)
    {
        // ��������Ƿ��㹻ѡ��
        if (gameManager.EnergySystem.CanSpendEnergy(assignedColor))
        {
            SelectPiece(pieceToSelect);
            Debug.Log($"[Input] �ɹ�ѡ������ {pieceToSelect.name}��");
        }
        else
        {
            Debug.Log($"[Input] ѡ��ʧ��: {assignedColor}���ж��㲻�㡣");
            ClearSelection();
        }
    }

    private void SelectPiece(PieceComponent piece)
    {
        // ��������Ƿ������ƶ�
        if (piece.RTState != null && piece.RTState.IsMoving)
        {
            Debug.Log($"[Input] ѡ��ʧ��: {piece.name} �����ƶ��У�����ѡ��");
            ClearSelection();
            return;
        }

        ClearSelection();
        selectedPiece = piece;

        // ��������һ�θ����������ӳ�
        UpdateSelectionHighlights(true);
    }

    private void ClearSelection()
    {
        selectedPiece = null;
        currentValidMoves.Clear();
        lastCalculatedValidMoves.Clear();
        boardRenderer.ClearAllHighlights();
    }

    private void UpdateSelectionHighlights(bool forceUpdate = false)
    {
        if (selectedPiece == null) return;

        BoardState logicalBoard = gameManager.GetLogicalBoardState();
        List<Vector2Int> newValidMoves = RuleEngine.GetValidMoves(selectedPiece.PieceData, selectedPiece.RTState.LogicalPosition, logicalBoard);

        // �����Ϸ��ƶ��б����仯ʱ���ػ棬���Ż�����
        if (forceUpdate || !newValidMoves.SequenceEqual(lastCalculatedValidMoves))
        {
            currentValidMoves = newValidMoves;
            lastCalculatedValidMoves = new List<Vector2Int>(newValidMoves);

            boardRenderer.ClearAllHighlights();
            boardRenderer.ShowValidMoves(currentValidMoves, selectedPiece.PieceData.Color, logicalBoard);
            boardRenderer.ShowSelectionMarker(selectedPiece.RTState.LogicalPosition);
        }
    }
}