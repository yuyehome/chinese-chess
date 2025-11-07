// 文件路径: Assets/Scripts/_Core/Controller/InputController.cs

using UnityEngine;

public class InputController : MonoBehaviour
{
    public LayerMask boardLayer; // 用于Raycast，仅检测棋盘地面
    public LayerMask pieceLayer; // 用于Raycast，仅检测棋子

    private PieceView _selectedPieceView;

    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    private void HandleMouseClick()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        // 优先检测是否点到棋子
        if (Physics.Raycast(ray, out RaycastHit pieceHit, 100f, pieceLayer))
        {
            PieceView clickedPiece = pieceHit.collider.GetComponent<PieceView>();
            if (clickedPiece != null && clickedPiece.team == PlayerTeam.Red) // 简单起见，只允许操作红方
            {
                SelectPiece(clickedPiece);
                return;
            }
        }

        // 如果没有选中棋子，或者点到了棋盘地面
        if (_selectedPieceView != null && Physics.Raycast(ray, out RaycastHit boardHit, 100f, boardLayer))
        {
            Debug.Log("Raycast 击中了棋盘！击中点: " + boardHit.point);

            Vector2Int targetGridPos = PieceView.WorldToGrid(boardHit.point);

            // 创建并发送移动指令
            var moveCmd = new MoveCommand(_selectedPieceView.pieceId, targetGridPos, _selectedPieceView.team);
            GameLoopController.Instance.RequestProcessCommand(moveCmd); // 通过单例发送

            DeselectPiece();
        }
        else if (_selectedPieceView != null)
        {
            // 如果选中了棋子，但上面那个if没进去，说明射线没打中
            Debug.LogWarning("已选中棋子，但Raycast没有击中任何'Board'层上的物体。");
        }
    }

    private void SelectPiece(PieceView piece)
    {
        // 取消上一个选择的棋子的高亮
        DeselectPiece();

        _selectedPieceView = piece;

        // 简单的高亮效果：放大一点
        _selectedPieceView.transform.localScale = Vector3.one * 1.2f;
    }

    private void DeselectPiece()
    {
        if (_selectedPieceView != null)
        {
            // 恢复正常大小
            _selectedPieceView.transform.localScale = Vector3.one;
            _selectedPieceView = null;
        }
    }
}