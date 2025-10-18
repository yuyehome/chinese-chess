// File: _Scripts/PlayerInput.cs
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private Camera mainCamera;
    private BoardRenderer boardRenderer;
    
    private PieceComponent selectedPiece = null;
    private List<Vector2Int> currentValidMoves = new List<Vector2Int>();

    void Start()
    {
        mainCamera = Camera.main;
        boardRenderer = FindObjectOfType<BoardRenderer>();
    }

    void Update()
    {
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
            PieceComponent pieceComponent = hit.collider.GetComponent<PieceComponent>();

            if (pieceComponent != null)
            {
                // �������һ������
                HandlePieceClick(pieceComponent);
            }
            else
            {
                // ����������̻������ط�
                HandleBoardClick(hit.point);
            }
        }
    }

    private void HandlePieceClick(PieceComponent piece)
    {
        // TODO: ֮����Ҫ������Ӫ�жϣ���������������Ӷ��ܱ�ѡ��
        ClearSelection();

        selectedPiece = piece;
        
        Piece pieceData = GameManager.Instance.CurrentBoardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, GameManager.Instance.CurrentBoardState);
        
        boardRenderer.ShowValidMoves(currentValidMoves);
    }

    private void HandleBoardClick(Vector3 hitPoint)
    {
        if (selectedPiece == null) return;
        
        // TODO: ���ｫ���Ǵ����ƶ�ָ����߼�
        // �򵥵����ѡ��
        ClearSelection();
    }
    
    private void ClearSelection()
    {
        selectedPiece = null;
        currentValidMoves.Clear();
        boardRenderer.ClearValidMoves();
    }
}