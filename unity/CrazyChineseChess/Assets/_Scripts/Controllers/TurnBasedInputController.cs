// File: _Scripts/Controllers/TurnBasedInputController.cs

using UnityEngine;

/// <summary>
/// ����ͳ�غ���ģʽ���������Ŀ�������
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
            Debug.LogError("[TurnBasedInput] ��ʼ��ʧ�ܣ���ǰ��GameMode����TurnBasedModeController��");
            this.enabled = false;
        }
    }

    private void Update()
    {
        if (turnBasedMode == null || gameManager.IsGameEnded) return;

        // ֻ���ֵ���ǰ���������������ɫʱ�Ŵ�������
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