// File: _Scripts/BoardRenderer.cs

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

/// <summary>
/// 视觉渲染层核心，负责将BoardState中的逻辑数据渲染为场景中的3D对象。
/// 管理所有棋子和视觉元素的创建、销毁、移动和高亮，支持多个棋子同时独立移动。
/// </summary>
public class BoardRenderer : MonoBehaviour
{
    [Header("Prefabs & Materials")]
    [Tooltip("棋子的3D模型预制件")]
    public GameObject gamePiecePrefab;
    public Material redPieceMaterial;
    public Material blackPieceMaterial;

    [Header("UI & Effects")]
    [Tooltip("选中棋子时显示的标记预制件")]
    public GameObject selectionMarkerPrefab;
    [Tooltip("可移动位置的提示标记预制件")]
    public GameObject moveMarkerPrefab;
    [Tooltip("可攻击棋子的高亮颜色")]
    public Color attackHighlightColor = new Color(1f, 0.2f, 0.2f);

    [Header("Animation Settings")]
    [Tooltip("棋子移动速度 (米/秒)")]
    [SerializeField] private float moveSpeed = 0.2f;
    [Tooltip("棋子跳跃动画的高度")]
    [SerializeField] private float jumpHeight = 0.1f;


    // --- 内部状态 ---
    // 存储当前显示的所有移动标记
    private List<GameObject> activeMarkers = new List<GameObject>();
    // 存储当前被高亮的棋子
    private List<PieceComponent> highlightedPieces = new List<PieceComponent>();
    // 二维数组，用于通过坐标快速查找棋子的GameObject，是视觉表现的唯一真实来源
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT];
    // 当前激活的选择标记实例
    private GameObject activeSelectionMarker = null;

    // UV坐标映射字典，用于从贴图集中选择正确的棋子文字
    private readonly Dictionary<PieceType, Vector2> uvOffsets = new Dictionary<PieceType, Vector2>()
    {
        { PieceType.General,   new Vector2(0.0f, 0.5f) },
        { PieceType.Advisor,   new Vector2(0.25f, 0.5f) },
        { PieceType.Elephant,  new Vector2(0.5f, 0.5f) },
        { PieceType.Chariot,   new Vector2(0.75f, 0.5f) },
        { PieceType.Horse,     new Vector2(0.0f, 0.0f) },
        { PieceType.Cannon,    new Vector2(0.25f, 0.0f) },
        { PieceType.Soldier,   new Vector2(0.5f, 0.0f) },
    };

    #region Public Action Methods

    /// <summary>
    /// 在视觉上启动一个棋子的移动动画。
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to, BoardState boardState, Action<PieceComponent, float> onProgressUpdate = null, Action<PieceComponent> onComplete = null)
    {
        GameObject pieceToMoveGO = GetPieceObjectAt(from);
        if (pieceToMoveGO == null)
        {
            Debug.LogWarning($"[Renderer] 尝试移动一个在坐标 {from} 上不存在的棋子。");
            return;
        }

        PieceComponent pc = pieceToMoveGO.GetComponent<PieceComponent>();
        if (pc == null || pc.PieceData.Type == PieceType.None)
        {
            Debug.LogError($"[Error] 尝试移动的棋子 {pieceToMoveGO.name} 没有有效的 PieceComponent 或 PieceData！");
            return;
        }

        // 1. 在视觉数组中将棋子从起点“提起”，使其在移动中不占据起始格
        pieceObjects[from.x, from.y] = null;
        pc.BoardPosition = to; // 更新组件内的目标位置

        // 2. 计算动画参数
        Vector3 startPos = GetLocalPosition(from.x, from.y);
        Vector3 endPos = GetLocalPosition(to.x, to.y);
        bool isJump = IsJumpingPiece(pc.PieceData.Type);

        // 3. 启动协程，并包装 onComplete 回调以在动画结束后更新 pieceObjects 数组
        StartCoroutine(MovePieceCoroutine(pc, startPos, endPos, isJump, onProgressUpdate,
            (completedPiece) => {
                // 动画完成后，在目标位置记录 GameObject
                if (completedPiece != null && completedPiece.RTState != null && !completedPiece.RTState.IsDead)
                {
                    pieceObjects[to.x, to.y] = completedPiece.gameObject;
                }
                // 调用原始的 onComplete 回调（例如，用于重置棋子状态）
                onComplete?.Invoke(completedPiece);
            }
        ));
    }

    /// <summary>
    /// 移动动画的核心协程，负责驱动棋子平滑移动。
    /// </summary>
    private IEnumerator MovePieceCoroutine(PieceComponent piece, Vector3 startPos, Vector3 endPos, bool isJump, Action<PieceComponent, float> onProgressUpdate, Action<PieceComponent> onComplete)
    {
        if (piece == null) yield break;

        float journeyDuration = Vector3.Distance(startPos, endPos) / moveSpeed;
        if (journeyDuration <= 0) journeyDuration = 0.01f;
        float elapsedTime = 0f;

        while (elapsedTime < journeyDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsedTime / journeyDuration);

            // 如果棋子在移动过程中被销毁，则安全退出协程
            if (piece == null || piece.gameObject == null)
            {
                yield break;
            }

            // 计算当前帧的位置
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);
            if (isJump)
            {
                currentPos.y += Mathf.Sin(percent * Mathf.PI) * jumpHeight;
            }
            piece.transform.localPosition = currentPos;

            // 调用回调，将当前进度传递给逻辑层
            onProgressUpdate?.Invoke(piece, percent);

            yield return null;
        }

        // 只有当协程正常完成（未被中途销毁）时，才执行收尾逻辑
        if (piece != null && piece.gameObject != null)
        {
            piece.transform.localPosition = endPos;
            onComplete?.Invoke(piece);
        }
    }

    #endregion

    #region Public Utility & Setup Methods

    /// <summary>
    /// 根据BoardState数据，完全重新绘制整个棋盘。通常只在游戏开始时调用。
    /// </summary>
    public void RenderBoard(BoardState boardState)
    {
        foreach (Transform child in transform) Destroy(child.gameObject);
        System.Array.Clear(pieceObjects, 0, pieceObjects.Length);

        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Piece piece = boardState.GetPieceAt(new Vector2Int(x, y));
                if (piece.Type != PieceType.None)
                {
                    CreatePieceObject(piece, new Vector2Int(x, y));
                }
            }
        }
    }

    /// <summary>
    /// 根据传入的合法移动列表，在棋盘上显示高亮提示。
    /// </summary>
    public void ShowValidMoves(List<Vector2Int> moves, PlayerColor movingPieceColor, BoardState boardState)
    {
        ClearAllHighlights();
        foreach (var move in moves)
        {
            Piece targetPiece = boardState.GetPieceAt(move);
            if (targetPiece.Type != PieceType.None) // 目标点有棋子
            {
                if (targetPiece.Color != movingPieceColor) // 并且是敌方棋子
                {
                    PieceComponent pc = GetPieceComponentAt(move);
                    if (pc != null) HighlightPiece(pc, attackHighlightColor);
                }
            }
            else // 目标点是空格
            {
                InstantiateMoveMarker(move);
            }
        }
    }

    /// <summary>
    /// 清除棋盘上所有的高亮效果、移动标记和选择标记。
    /// </summary>
    public void ClearAllHighlights()
    {
        foreach (var marker in activeMarkers) Destroy(marker);
        activeMarkers.Clear();

        foreach (var pc in highlightedPieces)
        {
            if (pc != null)
            {
                var renderer = pc.GetComponent<MeshRenderer>();
                var propBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_EmissionColor", Color.black);
                renderer.SetPropertyBlock(propBlock);
            }
        }
        highlightedPieces.Clear();

        if (activeSelectionMarker != null)
        {
            Destroy(activeSelectionMarker);
            activeSelectionMarker = null;
        }
    }

    /// <summary>
    /// 在指定棋子上方显示选择标记。
    /// </summary>
    public void ShowSelectionMarker(Vector2Int position)
    {
        if (activeSelectionMarker != null) Destroy(activeSelectionMarker);
        if (selectionMarkerPrefab == null) return;

        GameObject pieceGO = GetPieceObjectAt(position);
        if (pieceGO != null)
        {
            activeSelectionMarker = Instantiate(selectionMarkerPrefab, this.transform);
            Vector3 markerPosition = pieceGO.transform.localPosition + new Vector3(0, 0.03f, 0);
            activeSelectionMarker.transform.localPosition = markerPosition;
        }
    }

    /// <summary>
    /// 在视觉上移除一个棋子（销毁其GameObject）。
    /// </summary>
    public void RemovePieceAt(Vector2Int position)
    {
        GameObject pieceToRemove = GetPieceObjectAt(position);
        if (pieceToRemove != null)
        {
            Debug.Log($"[Renderer] 正在从坐标 {position} 移除GameObject: {pieceToRemove.name}。");
            Destroy(pieceToRemove);
            pieceObjects[position.x, position.y] = null;
        }
        else
        {
            Debug.LogWarning($"[Renderer] 尝试移除坐标 {position} 的棋子，但未找到GameObject。");
        }
    }

    /// <summary>
    /// 通过棋盘坐标快速获取棋子对象的PieceComponent。
    /// </summary>
    public PieceComponent GetPieceComponentAt(Vector2Int position)
    {
        GameObject pieceGO = GetPieceObjectAt(position);
        return pieceGO != null ? pieceGO.GetComponent<PieceComponent>() : null;
    }

    /// <summary>
    /// 通过棋盘坐标快速获取棋子GameObject。
    /// </summary>
    public GameObject GetPieceObjectAt(Vector2Int position)
    {
        if (position.x >= 0 && position.x < BoardState.BOARD_WIDTH && position.y >= 0 && position.y < BoardState.BOARD_HEIGHT)
        {
            return pieceObjects[position.x, position.y];
        }
        return null;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// 创建单个棋子的GameObject并放置在棋盘上。
    /// </summary>
    private void CreatePieceObject(Piece piece, Vector2Int position)
    {
        Vector3 localPosition = GetLocalPosition(position.x, position.y);
        GameObject pieceGO = Instantiate(gamePiecePrefab, this.transform);
        pieceGO.transform.localPosition = localPosition;
        pieceGO.name = $"{piece.Color}_{piece.Type}_{position.x}_{position.y}";

        // 关联组件和数据
        PieceComponent pc = pieceGO.GetComponent<PieceComponent>();
        if (pc != null)
        {
            pc.BoardPosition = position;
            pc.PieceData = piece;
        }

        // 设置材质和贴图
        MeshRenderer renderer = pieceGO.GetComponent<MeshRenderer>();
        if (renderer == null) return;
        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        renderer.material = (piece.Color == PlayerColor.Red) ? redPieceMaterial : blackPieceMaterial;
        if (uvOffsets.ContainsKey(piece.Type))
        {
            Vector2 offset = uvOffsets[piece.Type];
            propBlock.SetVector("_MainTex_ST", new Vector4(0.25f, 0.5f, offset.x, offset.y));
        }
        propBlock.SetColor("_EmissionColor", Color.black);
        renderer.SetPropertyBlock(propBlock);

        // 存储对GameObject的引用
        pieceObjects[position.x, position.y] = pieceGO;
    }

    /// <summary>
    /// 在指定坐标实例化一个移动标记。
    /// </summary>
    private void InstantiateMoveMarker(Vector2Int position)
    {
        Vector3 markerPos = GetLocalPosition(position.x, position.y);
        markerPos.y += 0.001f; // 稍微抬高，防止与棋盘平面穿模
        GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
        marker.transform.localPosition = markerPos;
        var collider = marker.GetComponent<SphereCollider>() ?? marker.AddComponent<SphereCollider>();
        collider.radius = 0.0175f;
        var markerComp = marker.GetComponent<MoveMarkerComponent>() ?? marker.AddComponent<MoveMarkerComponent>();
        markerComp.BoardPosition = position;
        activeMarkers.Add(marker);
    }

    /// <summary>
    /// 高亮单个棋子，通过设置材质的自发光颜色实现。
    /// </summary>
    private void HighlightPiece(PieceComponent piece, Color color)
    {
        var renderer = piece.GetComponent<MeshRenderer>();
        var propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_EmissionColor", color * 2.0f);
        renderer.SetPropertyBlock(propBlock);
        highlightedPieces.Add(piece);
    }

    /// <summary>
    /// 根据棋子类型判断是否应该执行跳跃动画。
    /// </summary>
    private bool IsJumpingPiece(PieceType type)
    {
        switch (type)
        {
            case PieceType.Horse:
            case PieceType.Elephant:
            case PieceType.Cannon:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// 将棋盘格子坐标转换为相对于此对象的本地3D坐标。
    /// </summary>
    private Vector3 GetLocalPosition(int x, int y)
    {
        const float boardLogicalWidth = 0.45f;
        const float boardLogicalHeight = 0.45f * (10f / 9f);
        float cellWidth = boardLogicalWidth / (BoardState.BOARD_WIDTH - 1);
        float cellHeight = boardLogicalHeight / (BoardState.BOARD_HEIGHT - 1);
        float xOffset = boardLogicalWidth / 2f;
        float zOffset = boardLogicalHeight / 2f;
        float xPos = x * cellWidth - xOffset;
        float zPos = y * cellHeight - zOffset;
        float pieceHeight = 0.0175f;
        return new Vector3(xPos, pieceHeight / 2f, zPos);
    }

    #endregion
}