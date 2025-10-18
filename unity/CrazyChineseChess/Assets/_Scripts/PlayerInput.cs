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
                // 点击到了一个棋子
                HandlePieceClick(pieceComponent);
            }
            else
            {
                // 点击到了棋盘或其他地方
                HandleBoardClick(hit.point);
            }
        }
    }

    private void HandlePieceClick(PieceComponent piece)
    {
        // TODO: 之后需要加入阵营判断，这里假设所有棋子都能被选中
        ClearSelection();

        selectedPiece = piece;
        
        Piece pieceData = GameManager.Instance.CurrentBoardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, GameManager.Instance.CurrentBoardState);
        
        boardRenderer.ShowValidMoves(currentValidMoves);
    }

    private void HandleBoardClick(Vector3 hitPoint)
    {
        if (selectedPiece == null) return;
        
        // TODO: 这里将来是处理移动指令的逻辑
        // 简单地清除选择
        ClearSelection();
    }
    
    private void ClearSelection()
    {
        selectedPiece = null;
        currentValidMoves.Clear();
        boardRenderer.ClearValidMoves();
    }
}