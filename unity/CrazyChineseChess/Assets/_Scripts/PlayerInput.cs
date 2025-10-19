// In File: _Scripts/PlayerInput.cs

// --- REPLACE the entire file with this corrected and updated version ---
// (In this case, a full replace is clearer because the changes are intertwined throughout the file)
using System.Collections.Generic;
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private Camera mainCamera;
    private BoardRenderer boardRenderer; // 恢复缓存的引用

    private PieceComponent selectedPiece = null;
    private List<Vector2Int> currentValidMoves = new List<Vector2Int>();

    void Start()
    {
        mainCamera = Camera.main;
        boardRenderer = FindObjectOfType<BoardRenderer>(); // 在Start中获取并缓存，这是正确的做法
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
            PieceComponent clickedPiece = hit.collider.GetComponent<PieceComponent>();

            if (clickedPiece != null)
            {
                HandlePieceClick(clickedPiece);
            }
            else
            {
                HandleBoardClick(hit);
            }
        }
    }

    private void HandlePieceClick(PieceComponent piece)
    {
        if (selectedPiece != null)
        {
            if (currentValidMoves.Contains(piece.BoardPosition))
            {
                GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, piece.BoardPosition);
                ClearSelection();
            }
            else
            {
                SelectPiece(piece);
            }
        }
        else
        {
            SelectPiece(piece);
        }
    }

    private void HandleBoardClick(RaycastHit hit)
    {
        if (selectedPiece == null) return;

        Vector2Int clickedGridPos = ConvertWorldToGridPosition(hit.point);

        if (currentValidMoves.Contains(clickedGridPos))
        {
            GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, clickedGridPos);
            ClearSelection();
        }
        else
        {
            ClearSelection();
        }
    }

    private void SelectPiece(PieceComponent piece)
    {
        ClearSelection();
        selectedPiece = piece;

        Piece pieceData = GameManager.Instance.CurrentBoardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, GameManager.Instance.CurrentBoardState);

        // 使用缓存的 boardRenderer 引用
        boardRenderer.ShowValidMoves(currentValidMoves, pieceData.Color, GameManager.Instance.CurrentBoardState);
    }

    private void ClearSelection()
    {
        selectedPiece = null;
        currentValidMoves.Clear();
        // 使用缓存的 boardRenderer 引用
        boardRenderer.ClearAllHighlights();
    }

    private Vector2Int ConvertWorldToGridPosition(Vector3 worldPos)
    {
        Vector3 localPos = boardRenderer.transform.InverseTransformPoint(worldPos);

        const float boardLogicalWidth = 0.45f;
        const float boardLogicalHeight = 0.45f * (10f / 9f);
        const float MARGIN_X = 0.025f;
        const float MARGIN_Y = 0.025f;
        float playingAreaWidth = boardLogicalWidth - 2 * MARGIN_X;
        float playingAreaHeight = boardLogicalHeight - 2 * MARGIN_Y;

        float startX = -playingAreaWidth / 2f;
        float startZ = -playingAreaHeight / 2f;

        int x = Mathf.RoundToInt((localPos.x - startX) / playingAreaWidth * (BoardState.BOARD_WIDTH - 1));
        int y = Mathf.RoundToInt((localPos.z - startZ) / playingAreaHeight * (BoardState.BOARD_HEIGHT - 1));

        return new Vector2Int(x, y);
    }
}