// File: _Scripts/PlayerInput.cs
using System.Collections.Generic;
using System.Linq; // 需要使用Linq
using UnityEngine;

public class PlayerInput : MonoBehaviour
{
    private Camera mainCamera;
    
    private PieceComponent selectedPiece = null;
    private List<Vector2Int> currentValidMoves = new List<Vector2Int>();

    void Start()
    {
        mainCamera = Camera.main;
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
                // 点击到了一个棋子
                HandlePieceClick(clickedPiece);
            }
            else
            {
                // 点击到了棋盘上的高亮标记(或其他地方)
                HandleBoardClick(hit);
            }
        }
    }

    private void HandlePieceClick(PieceComponent piece)
    {
        // 如果当前有棋子被选中
        if (selectedPiece != null)
        {
            // 检查点击的这个棋子是否是一个合法的攻击目标
            if (currentValidMoves.Contains(piece.BoardPosition))
            {
                // 是，执行吃子移动
                GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, piece.BoardPosition);
                ClearSelection();
            }
            else
            {
                // 不是，则切换选择到这个新棋子
                SelectPiece(piece);
            }
        }
        else
        {
            // 当前没有棋子被选中，直接选择这个棋子
            SelectPiece(piece);
        }
    }
    
    private void HandleBoardClick(RaycastHit hit)
    {
        // 必须先有选中的棋子
        if (selectedPiece == null) return;
        
        // 我们需要将世界坐标的点击点，转换为棋盘格子坐标
        Vector2Int clickedGridPos = ConvertWorldToGridPosition(hit.point);

        // 检查点击的格子是否在合法的移动列表中
        if (currentValidMoves.Contains(clickedGridPos))
        {
            // 是，执行移动
            GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, clickedGridPos);
            ClearSelection();
        }
        else
        {
            // 点击了无效位置，清除选择
            ClearSelection();
        }
    }
    
    /// <summary>
    /// 选中一个棋子并显示其可移动位置
    /// </summary>
    private void SelectPiece(PieceComponent piece)
    {
        // TODO: 加入阵营判断和回合判断
        ClearSelection();
        selectedPiece = piece;
        
        Piece pieceData = GameManager.Instance.CurrentBoardState.GetPieceAt(piece.BoardPosition);
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, GameManager.Instance.CurrentBoardState);
        
        FindObjectOfType<BoardRenderer>().ShowValidMoves(currentValidMoves, pieceData.Color, GameManager.Instance.CurrentBoardState);
    }

    private void ClearSelection()
    {
        selectedPiece = null;
        currentValidMoves.Clear();
        FindObjectOfType<BoardRenderer>().ClearAllHighlights();
    }

    /// <summary>
    /// 辅助方法：将世界坐标转换为棋盘格子坐标
    /// </summary>
    private Vector2Int ConvertWorldToGridPosition(Vector3 worldPos)
    {
        // 这个转换是 GetLocalPosition 的逆运算
        // 为了简化，我们直接从 BoardRenderer 获取转换后的本地坐标
        Vector3 localPos = FindObjectOfType<BoardRenderer>().transform.InverseTransformPoint(worldPos);
        
        const float TOTAL_BOARD_WIDTH = 0.45f;
        const float TOTAL_BOARD_HEIGHT = 0.45f * (10f / 9f);
        const float MARGIN_X = 0.025f;
        const float MARGIN_Y = 0.025f;
        float playingAreaWidth = TOTAL_BOARD_WIDTH - 2 * MARGIN_X;
        float playingAreaHeight = TOTAL_BOARD_HEIGHT - 2 * MARGIN_Y;

        float startX = -playingAreaWidth / 2f;
        float startZ = -playingAreaHeight / 2f;

        // 通过反向计算得到百分比，然后乘以格子数
        int x = Mathf.RoundToInt((localPos.x - startX) / playingAreaWidth * (BoardState.BOARD_WIDTH - 1));
        int y = Mathf.RoundToInt((localPos.z - startZ) / playingAreaHeight * (BoardState.BOARD_HEIGHT - 1));

        return new Vector2Int(x, y);
    }
}