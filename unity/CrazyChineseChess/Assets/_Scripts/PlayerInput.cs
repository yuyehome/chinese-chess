// File: _Scripts/PlayerInput.cs
using System.Collections.Generic;
using UnityEngine;
using System.Linq; // 引入Linq以便使用 .FirstOrDefault()

public class PlayerInput : MonoBehaviour
{
    private Camera mainCamera;
    private BoardRenderer boardRenderer; // 缓存对BoardRenderer的引用，提高性能

    private PieceComponent selectedPiece = null; // 当前选中的棋子
    private List<Vector2Int> currentValidMoves = new List<Vector2Int>(); // 当前选中棋子的所有合法移动点

    void Start()
    {
        mainCamera = Camera.main;
        // 在Start中获取并缓存BoardRenderer，这是推荐的做法，避免每次都Find
        boardRenderer = FindObjectOfType<BoardRenderer>();
    }

    void Update()
    {
        // 每一帧都检测鼠标左键是否按下
        if (Input.GetMouseButtonDown(0))
        {
            HandleMouseClick();
        }
    }

    /// <summary>
    /// 处理鼠标点击事件的总入口
    /// </summary>
    private void HandleMouseClick()
    {
        // 从摄像机向鼠标点击位置发射一条射线
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        // --- [新增调试代码] ---
        // 从射线起点，沿着射线方向，画一条红色的线，持续2秒钟
        Debug.DrawRay(ray.origin, ray.direction * 100f, Color.red, 10.0f);

        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // --- [新增调试代码] ---
            // 打印出被击中物体的名字，以及它身上有什么我们关心的组件
            Debug.Log($"射线击中了: {hit.collider.gameObject.name}");
            PieceComponent pc = hit.collider.GetComponent<PieceComponent>();
            MoveMarkerComponent mc = hit.collider.GetComponent<MoveMarkerComponent>();
            if (pc != null) Debug.Log("—— 它有一个 PieceComponent!");
            if (mc != null) Debug.Log("—— 它有一个 MoveMarkerComponent!");
            // --------------------

            // --- 行为分发 ---
            // 检查射线碰到的物体是什么
            PieceComponent clickedPiece = hit.collider.GetComponent<PieceComponent>();
            MoveMarkerComponent clickedMarker = hit.collider.GetComponent<MoveMarkerComponent>(); // 新增：检查是否点到移动标记

            if (clickedPiece != null)
            {
                // 1. 如果点击到了一个棋子
                HandlePieceClick(clickedPiece);
            }
            else if (clickedMarker != null)
            {
                // 2. 如果点击到了一个移动标记 (小绿片)
                HandleMarkerClick(clickedMarker);
            }
            else
            {
                // 3. 如果点击到棋盘或其他地方
                HandleBoardClick(hit);
            }
        } else
        {

            // --- [新增调试代码] ---
            Debug.Log("射线没有击中任何带有碰撞体的物体。");
            // --------------------
        }
    }

    /// <summary>
    /// 处理点击棋子的逻辑
    /// </summary>
    private void HandlePieceClick(PieceComponent piece)
    {
        // 如果已经有棋子被选中
        if (selectedPiece != null)
        {
            // 检查被点击的棋子是不是一个合法的攻击目标
            if (currentValidMoves.Contains(piece.BoardPosition))
            {
                // 是合法攻击目标，执行移动/吃子
                GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, piece.BoardPosition);
                ClearSelection(); // 操作完成后清除选择状态
            }
            else
            {
                // 如果点击的不是合法目标，则切换选择到这个新棋子上
                SelectPiece(piece);
            }
        }
        else
        {
            // 如果之前没有棋子被选中，则选中这个棋子
            SelectPiece(piece);
        }
    }

    /// <summary>
    /// 【新增】处理点击移动标记的逻辑
    /// </summary>
    private void HandleMarkerClick(MoveMarkerComponent marker)
    {
        // 如果没有棋子被选中，则忽略这次点击
        if (selectedPiece == null) return;

        // 检查这个标记对应的位置是否在合法移动列表中 (双重保险)
        if (currentValidMoves.Contains(marker.BoardPosition))
        {
            // 执行移动
            GameManager.Instance.ExecuteMove(selectedPiece.BoardPosition, marker.BoardPosition);
            ClearSelection(); // 操作完成后清除选择状态
        }
    }

    /// <summary>
    /// 处理点击棋盘或其他空白区域的逻辑
    /// </summary>
    private void HandleBoardClick(RaycastHit hit)
    {
        // 任何在有棋子选中状态下的空白点击，都视为取消选择
        if (selectedPiece != null)
        {
            ClearSelection();
        }
    }

    /// <summary>
    /// 选中一个棋子，并显示其合法的移动位置
    /// </summary>
    private void SelectPiece(PieceComponent piece)
    {
        ClearSelection(); // 先清除上一次的选择状态
        selectedPiece = piece;

        // 从数据核心获取棋子的逻辑信息
        Piece pieceData = GameManager.Instance.CurrentBoardState.GetPieceAt(piece.BoardPosition);
        // 调用规则引擎计算所有合法移动
        currentValidMoves = RuleEngine.GetValidMoves(pieceData, piece.BoardPosition, GameManager.Instance.CurrentBoardState);

        // 通知BoardRenderer显示高亮
        boardRenderer.ShowValidMoves(currentValidMoves, pieceData.Color, GameManager.Instance.CurrentBoardState);
    }

    /// <summary>
    /// 清除所有选择状态和UI高亮
    /// </summary>
    private void ClearSelection()
    {
        selectedPiece = null;
        currentValidMoves.Clear();
        boardRenderer.ClearAllHighlights();
    }
}
