// File: _Scripts/Controllers/PlayerInputController.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// 处理本地玩家输入的控制器。
/// 负责响应鼠标点击、管理棋子选择和高亮，并向GameManager提交移动请求。
/// </summary>
public class PlayerInputController : MonoBehaviour, IPlayerController
{
    // --- 内部状态 ---
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
        this.boardRenderer = gameManager.BoardRenderer; // 从GameManager获取引用
        this.mainCamera = Camera.main;
        Debug.Log($"[InputController] 玩家输入控制器已为 {assignedColor} 方初始化。");
    }

    private void Update()
    {
        // 如果未初始化或游戏结束，则不执行任何操作
        if (gameManager == null || gameManager.IsGameEnded) return;

        // 实时更新选中棋子的高亮（处理动态炮架等情况）
        UpdateSelectionHighlights();

        // 处理鼠标点击输入
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
        Debug.Log($"[Input] 玩家点击了棋子: {clickedPiece.name}");

        // 点击的是己方棋子
        if (clickedPiece.PieceData.Color == assignedColor)
        {
            TrySelectPiece(clickedPiece);
        }
        // 点击的是敌方棋子
        else
        {
            // 如果已选中己方棋子，且本次点击是合法的吃子目标
            if (selectedPiece != null && currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // 提交移动请求
                gameManager.RequestMove(assignedColor, selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                ClearSelection();
            }
            else
            {
                // 否则，视为无效操作，清空选择
                ClearSelection();
            }
        }
    }

    private void OnMarkerClicked(MoveMarkerComponent marker)
    {
        Debug.Log($"[Input] 玩家点击了移动标记，目标坐标: {marker.BoardPosition}");
        if (selectedPiece != null && currentValidMoves.Contains(marker.BoardPosition))
        {
            // 提交移动请求
            gameManager.RequestMove(assignedColor, selectedPiece.BoardPosition, marker.BoardPosition);
            ClearSelection();
        }
    }

    private void OnBoardClicked()
    {
        Debug.Log("[Input] 玩家点击了棋盘空白区域，取消选择。");
        ClearSelection();
    }

    private void TrySelectPiece(PieceComponent pieceToSelect)
    {
        // 检查能量是否足够选择
        if (gameManager.EnergySystem.CanSpendEnergy(assignedColor))
        {
            SelectPiece(pieceToSelect);
            Debug.Log($"[Input] 成功选择棋子 {pieceToSelect.name}。");
        }
        else
        {
            Debug.Log($"[Input] 选择失败: {assignedColor}方行动点不足。");
            ClearSelection();
        }
    }

    private void SelectPiece(PieceComponent piece)
    {
        // 检查棋子是否正在移动
        if (piece.RTState != null && piece.RTState.IsMoving)
        {
            Debug.Log($"[Input] 选择失败: {piece.name} 正在移动中，不可选择。");
            ClearSelection();
            return;
        }

        ClearSelection();
        selectedPiece = piece;

        // 立即计算一次高亮，避免延迟
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

        // 仅当合法移动列表发生变化时才重绘，以优化性能
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