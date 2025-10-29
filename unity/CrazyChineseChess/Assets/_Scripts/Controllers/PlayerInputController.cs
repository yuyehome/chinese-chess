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

    // --- 新增：可点击层的LayerMask ---
    [SerializeField]
    [Tooltip("定义哪些层可以被玩家点击。在Inspector中，取消勾选'EtherealPieces'层。")]
    private LayerMask clickableLayers;

    // --- 内部状态 ---
    private PlayerColor assignedColor;
    private GameManager gameManager;
    private BoardRenderer boardRenderer;
    private Camera mainCamera;

    private PieceComponent selectedPiece;
    private List<Vector2Int> currentValidMoves = new List<Vector2Int>();
    private List<Vector2Int> lastCalculatedValidMoves = new List<Vector2Int>();

    private void Awake()
    {
        // 默认禁用自身，等待GameManager在正确的时机通过Initialize()来激活。
        // 这样可以防止在未初始化完成时执行Update逻辑。
        this.enabled = false;
        Debug.Log("[InputController] PlayerInputController Awake: aelf-disabled, waiting for initialization.");
    }

    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
        this.boardRenderer = gameManager.BoardRenderer;
        this.mainCamera = Camera.main;

        // 关键：在初始化完成后，启用此组件，使其Update()方法开始执行。
        this.enabled = true;

        Debug.Log($"[InputController] PlayerInputController has been initialized for {assignedColor} and is now enabled.");
    }

    private void Update()
    {
        Debug.Log($"update 1");
        if (gameManager == null)
        {
            // 这个日志不应该出现，如果出现说明Initialize完全没被调用
            Debug.LogError("[InputController-DIAGNOSTIC] GameManager is NULL in Update!");
            return;
        }
        Debug.Log($"update 2");

        if (gameManager.IsGameEnded) return;

        Debug.Log($"update 3");

        // 日志 1: 检查Update是否在为正确的阵营运行
        // 这个日志会每帧都刷，有点烦人，但在找到问题前很有用。找到问题后可以注释掉。
        Debug.Log($"[InputController-DIAG-FRAME] Update running for color: {assignedColor}. IsMouseBtnDown: {Input.GetMouseButtonDown(0)}");
        // ----- DIAGNOSTIC LOG END -----

        // 实时更新选中棋子的高亮（处理动态炮架等情况）
        UpdateSelectionHighlights();

        // 处理鼠标点击输入
        if (Input.GetMouseButtonDown(0))
        {
            // ----- DIAGNOSTIC LOG START (添加诊断日志) -----
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
                gameManager.Client_RequestMove(selectedPiece.BoardPosition, clickedPiece.BoardPosition);
                Debug.Log($"[Input] 提交吃子移动: 从 {selectedPiece.BoardPosition} 到 {clickedPiece.BoardPosition}");
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
            gameManager.Client_RequestMove(selectedPiece.BoardPosition, marker.BoardPosition);
            Debug.Log($"[Input] 提交空格移动: 从 {selectedPiece.BoardPosition} 到 {marker.BoardPosition}");
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
        // MODIFICATION: Use .Value for SyncVar
        List<Vector2Int> newValidMoves = RuleEngine.GetValidMoves(selectedPiece.PieceData, selectedPiece.RTState.LogicalPosition, logicalBoard);

        // 仅当合法移动列表发生变化时才重绘，以优化性能
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