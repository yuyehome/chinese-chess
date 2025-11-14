// 文件路径: Assets/Scripts/_Core/Controller/InputController.cs

using UnityEngine;

public class InputController : MonoBehaviour
{
    [Header("场景引用")]
    [SerializeField] private BoardView boardView; 

    public LayerMask boardLayer;
    public LayerMask pieceLayer;
    private PieceView _selectedPieceView;

    void Update()
    {
        // 只允许在作为客户端连接后进行操作
        if (!NetworkServiceProvider.Instance.IsConnected) return;

        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit pieceHit, 100f, pieceLayer))
        {
            PieceView clickedPiece = pieceHit.collider.GetComponent<PieceView>();
            // TODO: 需要一个方法来确定当前玩家是哪个队伍
            if (clickedPiece != null && clickedPiece.team == PlayerTeam.Red)
            {
                SelectPiece(clickedPiece);
                return;
            }
        }

        if (_selectedPieceView != null && Physics.Raycast(ray, out RaycastHit boardHit, 100f, boardLayer))
        {

            if (boardView == null || boardView.Config == null)
            {
                Debug.LogError("InputController 错误: BoardView 或其 Config 未设置!", this);
                return;
            }

            // 使用 BoardView.Config 进行坐标转换
            Vector2Int targetGridPos = boardView.Config.WorldToGrid(boardHit.point);

            // 1. 创建NetworkCommand
            var moveCmd = new NetworkCommand
            {
                type = CommandType.Move,
                pieceId = _selectedPieceView.pieceId,
                targetPosition = targetGridPos,
                requestTeam = _selectedPieceView.team
            };

            // 2. 通过网络服务发送
            NetworkServiceProvider.Instance.SendCommandToServer(moveCmd);

            DeselectPiece();
        }
    }

    // SelectPiece 和 DeselectPiece 方法保持不变
    private void SelectPiece(PieceView piece)
    {
        DeselectPiece();
        _selectedPieceView = piece;
        _selectedPieceView.transform.localScale = Vector3.one * 1.2f;
    }

    private void DeselectPiece()
    {
        if (_selectedPieceView != null)
        {
            _selectedPieceView.transform.localScale = Vector3.one;
            _selectedPieceView = null;
        }
    }
}