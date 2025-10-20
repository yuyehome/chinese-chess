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
    // 【修改】现在由PieceStateController自己管理高亮，但BoardRenderer需要记录哪些被高亮了，以便清除
    private List<PieceStateController> highlightedControllers = new List<PieceStateController>(); 
    private List<GameObject> activeMarkers = new List<GameObject>(); // 存储当前显示的所有移动标记
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT]; // 二维数组，用于快速通过坐标查找棋子GameObject

    /// <summary>
    /// 【已重构】根据合法移动列表，显示可移动的标记和可攻击的敌人高亮。
    /// </summary>
    public void ShowValidMoves(List<Vector2Int> moves, PlayerColor movingPieceColor, BoardState boardState)
    {
        ClearAllHighlights(); // 先清除所有旧的反馈

        foreach (var move in moves)
        {
            Piece targetPiece = boardState.GetPieceAt(move);

            // 如果目标点是敌方棋子
            if (targetPiece.Type != PieceType.None && targetPiece.Color != movingPieceColor)
            {
                GameObject targetPieceGO = GetPieceObjectAt(move);
                if (targetPieceGO != null)
                {
                    var psc = targetPieceGO.GetComponent<PieceStateController>();
                    if (psc != null)
                    {
                        psc.Highlight(attackHighlightColor); // 调用高亮方法
                        highlightedControllers.Add(psc);   // 记录下来以便之后清除
                    }
                }
            }
            // 如果目标点是空格
            else if (targetPiece.Type == PieceType.None)
            {
                // --- 创建移动标记的逻辑保持不变 ---
                Vector3 markerPos = GetLocalPosition(move.x, move.y);
                markerPos.y += 0.001f;
                GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
                marker.transform.localPosition = markerPos;

                var collider = marker.GetComponent<SphereCollider>();
                if (collider == null) collider = marker.AddComponent<SphereCollider>();
                collider.radius = 0.0175f;

                var markerComp = marker.GetComponent<MoveMarkerComponent>();
                if (markerComp == null) markerComp = marker.AddComponent<MoveMarkerComponent>();
                markerComp.BoardPosition = move;

                activeMarkers.Add(marker);
            }
        }
    }


    /// <summary>
    /// 【已重构】清除棋盘上所有的高亮效果、移动标记和选择标记。
    /// </summary>
    public void ClearAllHighlights()
    {
        // 清除移动标记
        foreach (var marker in activeMarkers) Destroy(marker);
        activeMarkers.Clear();

        // 清除被高亮的棋子
        foreach (var psc in highlightedControllers)
        {
            psc?.ClearHighlight(); // 调用清除高亮的方法
        }
        highlightedControllers.Clear();

        // 清除选择标记（箭头）
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

        // 【新增】初始化PieceStateController
        PieceStateController psc = pieceGO.GetComponent<PieceStateController>();
        if (psc != null)
        {
            psc.Initialize(piece);
        }
        else
        {
            Debug.LogError("棋子Prefab上没有PieceStateController！");
        }

        pieceObjects[position.x, position.y] = pieceGO;
    }


    /// <summary>
    /// 【核心修改】启动一个独立的移动协程，并传递isCapture信息
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to, bool isCapture)
    {
        GameObject pieceToMove = GetPieceObjectAt(from);
        if (pieceToMove != null)
        {
            Vector3 startPos = GetLocalPosition(from.x, from.y);
            Vector3 endPos = GetLocalPosition(to.x, to.y);

            // 更新 pieceObjects 数组
            pieceObjects[to.x, to.y] = pieceToMove;
            pieceObjects[from.x, from.y] = null;
            PieceComponent pc = pieceToMove.GetComponent<PieceComponent>();
            if (pc != null) pc.BoardPosition = to;

            // 启动协程，并把isCapture信息传进去
            StartCoroutine(MovePieceCoroutine(pieceToMove, startPos, endPos, isCapture));
        }
    }

    /// <summary>
    /// 移动动画的核心协程，现在负责驱动PieceStateController的状态更新
    /// </summary>
    private System.Collections.IEnumerator MovePieceCoroutine(GameObject piece, Vector3 startPos, Vector3 endPos, bool isCapture)
    {
        var stateController = piece.GetComponent<PieceStateController>();
        // 【新增】获取该棋子的移动策略
        var movementStrategy = PieceStrategyFactory.GetStrategy(stateController.pieceComponent.PieceData.Type);

        if (stateController == null || movementStrategy == null)
        {
            Debug.LogError("移动的棋子没有PieceStateController或MovementStrategy！");
            yield break;
        }

        // --- 状态管理 ---
        stateController.OnMoveStart();
        // 【炮的特殊逻辑】如果这步是吃子，我们需要通知炮的策略
        if (stateController.pieceComponent.PieceData.Type == PieceType.Cannon)
        {
            // 这是一个简化的通知方式，更严谨的方案是策略模式包含一个SetContext方法
            // 我们暂时通过修改CannonStrategy来处理
            CannonStrategy.isNextMoveCapture = isCapture;
        }

        float journeyDuration = Vector3.Distance(startPos, endPos) / moveSpeed;
        if (journeyDuration <= 0) journeyDuration = 0.1f;
        float elapsedTime = 0f;

        while (elapsedTime < journeyDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsedTime / journeyDuration);

            // 状态更新
            stateController.OnMoveUpdate(percent);


            // --- 【核心修改】位置更新逻辑 ---
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);

            // 【新增】从策略获取Y轴高度并应用
            float currentJumpHeight = movementStrategy.GetJumpHeight(percent, this.jumpHeight);
            currentPos.y = startPos.y + currentJumpHeight; // 在基础高度上增加跳跃偏移

            if (piece != null)
            {
                // 使用Rigidbody.MovePosition来移动，以获得正确的物理交互
                piece.GetComponent<Rigidbody>().MovePosition(this.transform.TransformPoint(currentPos));
            }
            else
            {
                yield break;
            }
            yield return null;
        }

        if (piece != null)
        {
            // 确保结束时Y轴回到原位
            piece.GetComponent<Rigidbody>().MovePosition(this.transform.TransformPoint(endPos));
            stateController.OnMoveEnd();
        }
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
        /// 【新增】接收销毁请求，并安全地清理数据和对象。
        /// 这是处理棋子视觉销毁的唯一入口。
        /// </summary>
        public void RequestDestroyPiece(GameObject pieceToDestroy)
        {
            if (pieceToDestroy == null) return;

            var pc = pieceToDestroy.GetComponent<PieceComponent>();
            if (pc != null)
            {
                Vector2Int pos = pc.BoardPosition;

                // 确保我们销毁的是记录在数组中的同一个对象
                if (pieceObjects[pos.x, pos.y] == pieceToDestroy)
                {
                    // 从数组中移除引用
                    pieceObjects[pos.x, pos.y] = null;
                }
            }

            // 销毁游戏对象
            Destroy(pieceToDestroy);
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
