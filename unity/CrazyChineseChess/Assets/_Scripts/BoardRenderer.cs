// File: _Scripts/BoardRenderer.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 负责将BoardState中的逻辑数据“渲染”为场景中的3D对象。
/// 管理所有棋子和UI标记的创建、销毁、移动和高亮。
/// </summary>
public class BoardRenderer : MonoBehaviour
{
    // --- 在Unity编辑器中指定的资源 ---
    [Header("Prefabs & Materials")]
    public GameObject gamePiecePrefab;  // 棋子的3D模型预制件
    public Material redPieceMaterial;   // 红棋的材质
    public Material blackPieceMaterial; // 黑棋的材质

    [Header("UI & Effects")]
    public GameObject selectionMarkerPrefab; // 【新增】在Inspector中指定选择标记的预制件
    public GameObject moveMarkerPrefab; // 可移动位置的提示标记 (小绿片)
    public Color attackHighlightColor = new Color(1f, 0.2f, 0.2f); // 可攻击棋子的高亮颜色 (改为红色更直观)

    [Header("Animation Settings")]
    public float moveSpeed = 0.5f; // 棋子移动速度 (单位/秒)
    public float jumpHeight = 0.1f;  // 棋子跳跃高度

    // --- 内部状态变量 ---
    private GameObject activeSelectionMarker = null; // 【新增】用于存储当前的选择标记实例
    private List<GameObject> activeMarkers = new List<GameObject>(); // 存储当前显示的所有移动标记
    private List<PieceComponent> highlightedPieces = new List<PieceComponent>(); // 存储当前被高亮的棋子
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT]; // 二维数组，用于快速通过坐标查找棋子GameObject

    /// <summary>
    /// 根据传入的合法移动列表，在棋盘上显示高亮提示。
    /// </summary>
    /// <param name="moves">所有合法移动的坐标列表</param>
    /// <param name="movingPieceColor">正在移动的棋子的颜色</param>
    /// <param name="boardState">当前的棋盘状态</param>
    public void ShowValidMoves(List<Vector2Int> moves, PlayerColor movingPieceColor, BoardState boardState)
    {
        ClearAllHighlights(); // 在显示新标记前，清除所有旧的

        foreach (var move in moves)
        {
            Piece targetPiece = boardState.GetPieceAt(move);
            if (targetPiece.Type != PieceType.None) // 如果目标点有棋子
            {
                if (targetPiece.Color != movingPieceColor) // 并且是敌方棋子
                {
                    PieceComponent pc = GetPieceComponentAt(move);
                    if (pc != null) HighlightPiece(pc, attackHighlightColor); // 高亮该敌方棋子
                }
            }
            else // 如果目标点是空格
            {
                Vector3 markerPos = GetLocalPosition(move.x, move.y);
                markerPos.y += 0.001f; // 稍微抬高，防止与棋盘平面穿模
                GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
                marker.transform.localPosition = markerPos;

                // 给标记添加Collider和Component，以便被射线检测到
                var collider = marker.GetComponent<SphereCollider>();
                if (collider == null) collider = marker.AddComponent<SphereCollider>();
                collider.radius = 0.0175f; // 设置点击半径为棋子半径

                var markerComp = marker.GetComponent<MoveMarkerComponent>();
                if (markerComp == null) markerComp = marker.AddComponent<MoveMarkerComponent>();
                markerComp.BoardPosition = move; // 记录该标记对应的棋盘坐标

                activeMarkers.Add(marker);
            }
        }
    }

    /// <summary>
    /// 清除棋盘上所有的高亮效果和移动标记。
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
                propBlock.SetColor("_EmissionColor", Color.black); // 将自发光颜色重置为黑
                renderer.SetPropertyBlock(propBlock);
            }
        }
        highlightedPieces.Clear();

        // 【新增】清除选择标记
        if (activeSelectionMarker != null)
        {
            Destroy(activeSelectionMarker);
            activeSelectionMarker = null;
        }

    }

    /// <summary>
    /// 【已修正】显示选中棋子的标记。
    /// </summary>
    public void ShowSelectionMarker(Vector2Int position)
    {
        // 1. 清除任何可能存在的旧标记
        if (activeSelectionMarker != null)
        {
            Destroy(activeSelectionMarker);
        }

        // 2. 检查预制件是否存在
        if (selectionMarkerPrefab == null)
        {
            Debug.LogWarning("SelectionMarkerPrefab 未在 BoardRenderer 中指定！");
            return;
        }

        // 3. 获取被选中的棋子的GameObject
        GameObject pieceGO = GetPieceObjectAt(position);
        if (pieceGO != null)
        {
            // 4. 【核心修正】将标记实例化为 pieceGO 的子对象
            //    这样它的坐标系就是相对于棋子的，并且会自动跟随棋子移动。
            activeSelectionMarker = Instantiate(selectionMarkerPrefab, pieceGO.transform);

            // 5. 【核心修正】设置标记的局部位置 (localPosition)，让它在棋子正上方
            //    因为父对象就是棋子本身，所以我们只需要一个简单的向上偏移。
            activeSelectionMarker.transform.localPosition = new Vector3(0, 0.03f, 0);

            // 6. （可选）如果你希望标记不随棋子旋转，可以重置它的局部旋转
            activeSelectionMarker.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// 【新增】辅助方法：通过棋盘坐标快速获取棋子的GameObject。
    /// </summary>
    public GameObject GetPieceObjectAt(Vector2Int position)
    {
        // 先检查坐标是否在棋盘的有效范围内
        if (position.x >= 0 && position.x < BoardState.BOARD_WIDTH &&
            position.y >= 0 && position.y < BoardState.BOARD_HEIGHT)
        {
            // 如果有效，则返回存储在二维数组中的GameObject
            return pieceObjects[position.x, position.y];
        }

        // 如果坐标无效，则返回null
        return null;
    }


    /// <summary>
    /// 高亮单个棋子，通过设置材质的自发光颜色实现。
    /// </summary>
    private void HighlightPiece(PieceComponent piece, Color color)
    {
        var renderer = piece.GetComponent<MeshRenderer>();
        var propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);

        propBlock.SetColor("_EmissionColor", color * 2.0f); // 乘以一个系数让它更亮
        renderer.SetPropertyBlock(propBlock);
        highlightedPieces.Add(piece);
    }

    /// <summary>
    /// 辅助方法：通过棋盘坐标快速获取棋子对象的PieceComponent。
    /// </summary>
    public PieceComponent GetPieceComponentAt(Vector2Int position)
    {
        if (position.x >= 0 && position.x < BoardState.BOARD_WIDTH &&
            position.y >= 0 && position.y < BoardState.BOARD_HEIGHT)
        {
            GameObject pieceGO = pieceObjects[position.x, position.y];
            if (pieceGO != null) return pieceGO.GetComponent<PieceComponent>();
        }
        return null;
    }

    // UV坐标映射字典，用于从贴图集中选择正确的棋子文字
    private Dictionary<PieceType, Vector2> uvOffsets = new Dictionary<PieceType, Vector2>()
    {
        { PieceType.General,   new Vector2(0.0f, 0.5f) },
        { PieceType.Advisor,   new Vector2(0.25f, 0.5f) },
        { PieceType.Elephant,  new Vector2(0.5f, 0.5f) },
        { PieceType.Chariot,   new Vector2(0.75f, 0.5f) },
        { PieceType.Horse,     new Vector2(0.0f, 0.0f) },
        { PieceType.Cannon,    new Vector2(0.25f, 0.0f) },
        { PieceType.Soldier,   new Vector2(0.5f, 0.0f) },
    };

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
    /// 创建单个棋子的GameObject并放置在棋盘上。
    /// </summary>
    private void CreatePieceObject(Piece piece, Vector2Int position)
    {
        Vector3 localPosition = GetLocalPosition(position.x, position.y);
        GameObject pieceGO = Instantiate(gamePiecePrefab, this.transform);
        pieceGO.transform.localPosition = localPosition;
        pieceGO.name = $"{piece.Color}_{piece.Type}_{position.x}_{position.y}";

        PieceComponent pc = pieceGO.GetComponent<PieceComponent>();
        if (pc != null)
        {
            pc.BoardPosition = position;
            pc.PieceData = piece; // 【新增】将棋子的逻辑数据存入组件
        }

        //if (piece.Color == PlayerColor.Red) pieceGO.transform.Rotate(0, 95, 0, Space.World);
        //else if (piece.Color == PlayerColor.Black) pieceGO.transform.Rotate(0, -85, 0, Space.World);

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
        pieceObjects[position.x, position.y] = pieceGO;
    }


    /// <summary>
    /// 【核心修改】在视觉上移动一个棋子。
    /// 这个方法现在会启动一个协程来执行平滑的移动动画。
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to, BoardState boardState, bool isCapture)
    {
        GameObject pieceToMove = pieceObjects[from.x, from.y];
        if (pieceToMove != null)
        {
            Vector3 startPos = GetLocalPosition(from.x, from.y);
            Vector3 endPos = GetLocalPosition(to.x, to.y);
            Piece pieceData = boardState.GetPieceAt(to);

            // 【修改】直接使用传入的 isCapture 参数
            bool isJump = IsJumpingPiece(pieceData.Type, isCapture);

            pieceObjects[to.x, to.y] = pieceToMove;
            pieceObjects[from.x, from.y] = null;
            PieceComponent pc = pieceToMove.GetComponent<PieceComponent>();
            if (pc != null) pc.BoardPosition = to;

            StartCoroutine(MovePieceCoroutine(pieceToMove, startPos, endPos, isJump));
        }
    }

    /// <summary>
    /// 移动动画的核心协程。
    /// </summary>
    private System.Collections.IEnumerator MovePieceCoroutine(GameObject piece, Vector3 startPos, Vector3 endPos, bool isJump)
    {
        //GameManager.Instance.SetAnimating(true);
        float journeyDuration = Vector3.Distance(startPos, endPos) / moveSpeed;
        if (journeyDuration <= 0) journeyDuration = 0.1f; // 防止除零错误
        float elapsedTime = 0f;

        while (elapsedTime < journeyDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsedTime / journeyDuration);
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);
            if (isJump)
            {
                currentPos.y += Mathf.Sin(percent * Mathf.PI) * jumpHeight;
            }
            if (piece != null) piece.transform.localPosition = currentPos;
            yield return null;
        }
        if (piece != null) piece.transform.localPosition = endPos;
        //GameManager.Instance.SetAnimating(false);
    }

    /// <summary>
    /// 【已修正】辅助方法：根据棋子类型和移动情况判断是否应该执行跳跃动画。
    /// 使用 pieceObjects 数组来正确判断炮是否在吃子。
    /// </summary>
    private bool IsJumpingPiece(PieceType type, bool isCapture)
    {
        switch (type)
        {
            case PieceType.Horse:
            case PieceType.Elephant:
                return true;

            case PieceType.Cannon:
                // 炮只有在吃子时才跳跃
                return isCapture;

            default:
                return false;
        }
    }

    /// <summary>
    /// 在视觉上移除一个棋子（GameObject）。
    /// </summary>
    public void RemovePieceAt(Vector2Int position)
    {
        GameObject pieceToRemove = pieceObjects[position.x, position.y];
        if (pieceToRemove != null)
        {
            Destroy(pieceToRemove);
            pieceObjects[position.x, position.y] = null;
        }
    }

    /// <summary>
    /// 【已修正】将棋盘格子坐标转换为相对于此对象的本地3D坐标。
    /// </summary>
    private Vector3 GetLocalPosition(int x, int y)
    {
        // --- 设计常量 ---
        const float boardLogicalWidth = 0.45f;
        const float boardLogicalHeight = 0.45f * (10f / 9f);

        // --- 计算 ---
        float cellWidth = boardLogicalWidth / (BoardState.BOARD_WIDTH - 1);
        float cellHeight = boardLogicalHeight / (BoardState.BOARD_HEIGHT - 1);

        float xOffset = boardLogicalWidth / 2f;
        float zOffset = boardLogicalHeight / 2f;

        float xPos = x * cellWidth - xOffset;
        float zPos = y * cellHeight - zOffset;

        float pieceHeight = 0.0175f;

        // 必须有一个返回值，否则会产生 CS0161 错误
        return new Vector3(xPos, pieceHeight / 2f, zPos);
    }
}