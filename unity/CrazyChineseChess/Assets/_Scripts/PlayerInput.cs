// File: _Scripts/PlayerInput.cs
using UnityEngine;

/// <summary>
/// 【重构后】纯粹的玩家输入处理脚本。
/// 它的唯一职责是检测鼠标点击，并将事件转发给当前激活的游戏模式控制器。
/// 它不再包含任何游戏规则或流程逻辑。
/// </summary>
public class PlayerInput : MonoBehaviour
{
    private Camera mainCamera;

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
        // 获取当前的游戏模式控制器，如果不存在则不进行任何操作
        GameModeController gameMode = GameManager.Instance.CurrentGameMode;
        if (gameMode == null) return;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // --- 输入事件转发 ---
            PieceComponent clickedPiece = hit.collider.GetComponent<PieceComponent>();
            MoveMarkerComponent clickedMarker = hit.collider.GetComponent<MoveMarkerComponent>();

            if (clickedPiece != null)
            {
                gameMode.OnPieceClicked(clickedPiece);
            }
            else if (clickedMarker != null)
            {
                gameMode.OnMarkerClicked(clickedMarker);
            }
            else
            {
                gameMode.OnBoardClicked(hit);
            }
        }
    }
}