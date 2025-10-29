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

    // --- �������ɵ�����LayerMask ---
    [SerializeField]
    [Tooltip("������Щ����Ա���ҵ������Inspector�У�ȡ����ѡ'EtherealPieces'�㡣")]
    private LayerMask clickableLayers;

    // --- �ڲ�״̬ ---
    private PlayerColor assignedColor;
    private GameManager gameManager;
    private BoardRenderer boardRenderer;
    private Camera mainCamera;

    private PieceComponent selectedPiece;
    private List<Vector2Int> currentValidMoves = new List<Vector2Int>();
    private List<Vector2Int> lastCalculatedValidMoves = new List<Vector2Int>();

    private void Awake()
    {
        // Ĭ�Ͻ��������ȴ�GameManager����ȷ��ʱ��ͨ��Initialize()�����
        // �������Է�ֹ��δ��ʼ�����ʱִ��Update�߼���
        this.enabled = false;
        Debug.Log("[InputController] PlayerInputController Awake: aelf-disabled, waiting for initialization.");
    }

    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
        this.boardRenderer = gameManager.BoardRenderer;
        this.mainCamera = Camera.main;

        // �ؼ����ڳ�ʼ����ɺ����ô������ʹ��Update()������ʼִ�С�
        this.enabled = true;

        Debug.Log($"[InputController] PlayerInputController has been initialized for {assignedColor} and is now enabled.");
    }

    private void Update()
    {
        Debug.Log($"update 1");
        if (gameManager == null)
        {
            // �����־��Ӧ�ó��֣��������˵��Initialize��ȫû������
            Debug.LogError("[InputController-DIAGNOSTIC] GameManager is NULL in Update!");
            return;
        }
        Debug.Log($"update 2");

        if (gameManager.IsGameEnded) return;

        Debug.Log($"update 3");

        // ��־ 1: ���Update�Ƿ���Ϊ��ȷ����Ӫ����
        // �����־��ÿ֡��ˢ���е㷳�ˣ������ҵ�����ǰ�����á��ҵ���������ע�͵���
        Debug.Log($"[InputController-DIAG-FRAME] Update running for color: {assignedColor}. IsMouseBtnDown: {Input.GetMouseButtonDown(0)}");
        // ----- DIAGNOSTIC LOG END -----

        // ʵʱ����ѡ�����ӵĸ���������̬�ڼܵ������
        UpdateSelectionHighlights();

        // �������������
        if (Input.GetMouseButtonDown(0))
        {
            // ----- DIAGNOSTIC LOG START (��������־) -----
            Debug.Log($"[InputController-DIAGNOSTIC] Mouse button down detected for color: {assignedColor}. Firing Raycast...");
            // ----- DIAGNOSTIC LOG END -----
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit, 100f, clickableLayers))
        {
            Debug.Log($"[InputController-DIAGNOSTIC] Raycast HIT! Object: {hit.collider.name}, Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}");
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
        else
        {
            Debug.LogWarning($"[InputController-DIAGNOSTIC] Raycast MISSED. No object hit on clickable layers.");
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
                gameManager.Client_RequestMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                Debug.Log($"[Input] �ύ�����ƶ�: �� {selectedPiece.BoardPosition} �� {clickedPiece.BoardPosition}");
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
            gameManager.Client_RequestMove(selectedPiece.BoardPosition, marker.BoardPosition);
            Debug.Log($"[Input] �ύ�ո��ƶ�: �� {selectedPiece.BoardPosition} �� {marker.BoardPosition}");
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
        // MODIFICATION: Use .Value for SyncVar
        List<Vector2Int> newValidMoves = RuleEngine.GetValidMoves(selectedPiece.PieceData, selectedPiece.RTState.LogicalPosition, logicalBoard);

        // �����Ϸ��ƶ��б����仯ʱ���ػ棬���Ż�����
        if (forceUpdate || !newValidMoves.SequenceEqual(lastCalculatedValidMoves))
        {
            currentValidMoves = newValidMoves;
            lastCalculatedValidMoves = new List<Vector2Int>(newValidMoves);

            boardRenderer.ClearAllHighlights();
            // MODIFICATION: Use .Value for SyncVar
            boardRenderer.ShowValidMoves(currentValidMoves, selectedPiece.Color.Value, logicalBoard);
            boardRenderer.ShowSelectionMarker(selectedPiece.RTState.LogicalPosition);
        }
    }


}