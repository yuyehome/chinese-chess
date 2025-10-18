// File: _Scripts/BoardRenderer.cs
using UnityEngine;

public class BoardRenderer : MonoBehaviour
{
    // 在Unity编辑器里，把我们的 Prefab 和 Materials 拖到这里
    public GameObject piecePrefab;
    public Material redMaterial;
    public Material blackMaterial;
    
    // 用于存储当前场景中所有棋子的GameObject，方便后续管理
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT];

    /// <summary>
    /// 根据传入的 BoardState 数据，渲染整个棋盘
    /// </summary>
    public void RenderBoard(BoardState boardState)
    {
        // 遍历棋盘的每一个格子
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Piece piece = boardState.GetPieceAt(new Vector2Int(x, y));
                
                // 如果这个格子有棋子
                if (piece.Type != PieceType.None)
                {
                    // 1. 计算棋子相对于 BoardVisual 的本地坐标
                    Vector3 localPosition = GetLocalPosition(x, y);
                    
                    // 2. 实例化棋子，并指定父对象
                    //    Instantiate 会自动将 localPosition 设置为相对于父对象的坐标
                    GameObject pieceGO = Instantiate(piecePrefab, this.transform);
                    pieceGO.transform.localPosition = localPosition; // 直接设置本地坐标

                    pieceGO.name = $"{piece.Color}_{piece.Type}_{x}_{y}";

                    // 根据棋子颜色设置材质
                    MeshRenderer renderer = pieceGO.GetComponent<MeshRenderer>();
                    if (piece.Color == PlayerColor.Red)
                    {
                        renderer.material = redMaterial;
                    }
                    else if (piece.Color == PlayerColor.Black)
                    {
                        renderer.material = blackMaterial;
                    }
                    
                    // 存储对这个GameObject的引用
                    pieceObjects[x, y] = pieceGO;
                }
            }
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