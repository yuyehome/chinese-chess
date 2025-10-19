// File: _Scripts/BoardRenderer.cs
using UnityEngine;
using System.Collections.Generic; // 需要使用字典
using System.Linq; // 需要使用Linq

public class BoardRenderer : MonoBehaviour
{
    // --- 资源引用 ---
    [Header("Prefabs & Materials")]
    public GameObject gamePiecePrefab; // 新的棋子Prefab
    public Material redPieceMaterial;  // 红棋材质
    public Material blackPieceMaterial;// 黑棋材质
    
    [Header("UI & Effects")]
    public GameObject moveMarkerPrefab; // 高亮提示的Prefab
    public Color attackHighlightColor = Color.green; // <-- [新增] 在这里添加颜色变量
    private List<GameObject> activeMarkers = new List<GameObject>();
    
    private List<PieceComponent> highlightedPieces = new List<PieceComponent>(); // 用于追踪被高亮的棋子

    public void ShowValidMoves(List<Vector2Int> moves, PlayerColor movingPieceColor, BoardState boardState)
    {
        ClearAllHighlights();

        foreach (var move in moves)
        {
            Piece targetPiece = boardState.GetPieceAt(move);
            if (targetPiece.Type != PieceType.None)
            {
                if (targetPiece.Color != movingPieceColor)
                {
                    PieceComponent pc = GetPieceComponentAt(move);
                    if (pc != null) HighlightPiece(pc, attackHighlightColor);
                }
            }
            else
            {
                Vector3 markerPos = GetLocalPosition(move.x, move.y);
                markerPos.y += 0.001f;
                GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
                marker.transform.localPosition = markerPos;
                activeMarkers.Add(marker);
            }
        }
    }

    // 清除所有高亮和标记
    public void ClearAllHighlights()
    {
        // 清除移动标记
        foreach (var marker in activeMarkers) Destroy(marker);
        activeMarkers.Clear();
        
        // 清除棋子高光
        foreach (var pc in highlightedPieces)
        {
            if (pc != null) // 棋子可能已经被吃掉
            {
                // 重置自发光颜色
                var renderer = pc.GetComponent<MeshRenderer>();
                var propBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_EmissionColor", Color.black);
                renderer.SetPropertyBlock(propBlock);
            }
        }
        highlightedPieces.Clear();
    }
    
    // 高亮单个棋子
    private void HighlightPiece(PieceComponent piece, Color color)
    {
        var renderer = piece.GetComponent<MeshRenderer>();
        var propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        
        // 设置自发光颜色。乘以一个系数让它更亮
        propBlock.SetColor("_EmissionColor", color * 2.0f); 
        renderer.SetPropertyBlock(propBlock);

        highlightedPieces.Add(piece);
    }

    /// 辅助方法：通过坐标获取棋子的Component (O(1)查找)
    public PieceComponent GetPieceComponentAt(Vector2Int position)
    {
        // 修复：直接进行数学边界检查，不再依赖错误的 boardState 实例
        if (position.x >= 0 && position.x < BoardState.BOARD_WIDTH &&
            position.y >= 0 && position.y < BoardState.BOARD_HEIGHT)
        {
            GameObject pieceGO = pieceObjects[position.x, position.y];
            if (pieceGO != null)
            {
                return pieceGO.GetComponent<PieceComponent>();
            }
        }
        return null;
    }

    // --- UV坐标映射 ---
    // 这个字典定义了每种棋子的文字在贴图集(Atlas)上的UV偏移量
    // Key: PieceType
    // Value: Vector2(x_offset, y_offset)
    // 假设我们的贴图集是 4x2 的网格
    private Dictionary<PieceType, Vector2> uvOffsets = new Dictionary<PieceType, Vector2>()
    {
        // 假设第一行: 帅, 仕, 相, 车
        { PieceType.General,   new Vector2(0.0f, 0.5f) },
        { PieceType.Advisor,   new Vector2(0.25f, 0.5f) },
        { PieceType.Elephant,  new Vector2(0.5f, 0.5f) },
        { PieceType.Chariot,   new Vector2(0.75f, 0.5f) },
        // 假设第二行: 马, 炮, 兵
        { PieceType.Horse,     new Vector2(0.0f, 0.0f) },
        { PieceType.Cannon,    new Vector2(0.25f, 0.0f) },
        { PieceType.Soldier,   new Vector2(0.5f, 0.0f) },
    };
    
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT];

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

    private void CreatePieceObject(Piece piece, Vector2Int position)
    {
        // This method now contains all the logic from the old RenderBoard's loop
        Vector3 localPosition = GetLocalPosition(position.x, position.y);
        GameObject pieceGO = Instantiate(gamePiecePrefab, this.transform);
        pieceGO.transform.localPosition = localPosition;
        pieceGO.name = $"{piece.Color}_{piece.Type}_{position.x}_{position.y}";

        PieceComponent pc = pieceGO.GetComponent<PieceComponent>();
        if (pc != null) pc.BoardPosition = position;

        if (piece.Color == PlayerColor.Red) pieceGO.transform.Rotate(0, 95, 0, Space.World);
        else if (piece.Color == PlayerColor.Black) pieceGO.transform.Rotate(0, -85, 0, Space.World);

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

    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        GameObject pieceToMove = pieceObjects[from.x, from.y];
        if (pieceToMove != null)
        {
            pieceToMove.transform.localPosition = GetLocalPosition(to.x, to.y);

            pieceObjects[to.x, to.y] = pieceToMove;
            pieceObjects[from.x, from.y] = null;

            PieceComponent pc = pieceToMove.GetComponent<PieceComponent>();
            if (pc != null) pc.BoardPosition = to;
        }
    }

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
    /// 将棋盘格子坐标 (x,y) 转换为相对于此对象(BoardVisual)的本地坐标。
    /// 这个版本是基于设计尺寸，与模型本身大小解耦，最稳定。
    /// 棋盘固定尺寸45X45cm，棋子固定尺寸35mm
    /// </summary>
    private Vector3 GetLocalPosition(int x, int y)
    {
        // --- 设计常量 ---
        // 我们在代码中定义棋盘的逻辑尺寸，而不是依赖模型。
        // 棋盘总宽度 (X轴, 8个间隔)
        const float boardLogicalWidth = 0.45f; 
        // 棋盘总高度 (Z轴, 9个间隔)
        const float boardLogicalHeight = 0.45f * (10f / 9f); // 按比例计算，中国象棋棋盘是长方形的
        
        // --- 计算 ---
        // 计算每个格子的间距
        float cellWidth = boardLogicalWidth / (BoardState.BOARD_WIDTH - 1);
        float cellHeight = boardLogicalHeight / (BoardState.BOARD_HEIGHT - 1);
        
        // 计算偏移量，使得棋盘中心在 (0,0)
        float xOffset = boardLogicalWidth / 2f;
        float zOffset = boardLogicalHeight / 2f;
        
        // 计算最终本地坐标
        float xPos = x * cellWidth - xOffset;
        float zPos = y * cellHeight - zOffset;

        // 获取棋子的高度，使其刚好浮在棋盘上
        float pieceHeight = 0.0175f; // 对应棋子世界高度

        return new Vector3(xPos, pieceHeight / 2f, zPos);
    }

}