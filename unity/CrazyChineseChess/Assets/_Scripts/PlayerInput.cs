// File: _Scripts/PlayerInput.cs

using UnityEngine;

/// <summary>
/// 纯粹的玩家输入处理脚本。
/// 它的唯一职责是检测鼠标点击，解析点击目标（棋子、标记、棋盘），
/// 并将这些原始输入事件转发给当前激活的游戏模式控制器进行逻辑处理。
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
        // 检测鼠标左键点击
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    /// <summary>
    /// 处理鼠标点击事件，发射射线并分发输入。
    /// </summary>
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
                // 点击了棋子
                gameMode.OnPieceClicked(clickedPiece);
            }
            else if (clickedMarker != null)
            {
                // 点击了移动标记
                gameMode.OnMarkerClicked(clickedMarker);
            }
            else
            {
                // 点击了棋盘的其他区域
                gameMode.OnBoardClicked(hit);
            }
        }
    }
}