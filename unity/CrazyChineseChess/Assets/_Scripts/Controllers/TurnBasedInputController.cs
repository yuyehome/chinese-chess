// File: _Scripts/Controllers/TurnBasedInputController.cs

using UnityEngine;

/// <summary>
/// 处理传统回合制模式下玩家输入的控制器。
/// </summary>
public class TurnBasedInputController : MonoBehaviour, IPlayerController
{
    private PlayerColor assignedColor;
    private GameManager gameManager;
    private TurnBasedModeController turnBasedMode;
    private Camera mainCamera;

    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
        this.turnBasedMode = gameManager.GetCurrentGameMode() as TurnBasedModeController;
        this.mainCamera = Camera.main;

        if (this.turnBasedMode == null)
        {
            Debug.LogError("[TurnBasedInput] 初始化失败：当前的GameMode不是TurnBasedModeController！");
            this.enabled = false;
        }
    }

    private void Update()
    {
        if (turnBasedMode == null || gameManager.IsGameEnded) return;

        // 只在轮到当前控制器所分配的颜色时才处理输入
        if (turnBasedMode.GetCurrentPlayer() != assignedColor) return;

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
                turnBasedMode.OnPieceClicked(clickedPiece);
            }
            else if (clickedMarker != null)
            {
                turnBasedMode.OnMarkerClicked(clickedMarker);
            }
            else
            {
                turnBasedMode.OnBoardClicked();
            }
        }
    }
}