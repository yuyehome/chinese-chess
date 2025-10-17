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
                    // 计算棋子在3D世界中的位置
                    Vector3 worldPosition = GetWorldPosition(x, y);
                    
                    // 实例化一个棋子Prefab
                    GameObject pieceGO = Instantiate(piecePrefab, worldPosition, Quaternion.identity, this.transform);
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
    /// 将棋盘格子坐标 (0,0) 转换为世界坐标。
    /// 这需要根据你的棋盘模型大小进行调整。
    /// </summary>
    private Vector3 GetWorldPosition(int x, int y)
    {
        // Plane 的大小是 10x10 units。我们的棋盘是 9x10 格。
        // 我们需要一个映射关系。假设棋盘模型的左下角对应世界坐标的 (-4.5, 0, -5)。
        float boardWidthUnits = 9.0f;
        float boardHeightUnits = 10.0f;

        float xPos = (x - (BoardState.BOARD_WIDTH - 1) / 2.0f) * (boardWidthUnits / (BoardState.BOARD_WIDTH -1));
        float zPos = (y - (BoardState.BOARD_HEIGHT - 1) / 2.0f) * (boardHeightUnits / (BoardState.BOARD_HEIGHT - 1));

        // 我们将棋盘的Y轴对齐到世界的Z轴
        return new Vector3(xPos, 0.1f, zPos); // 0.1f 是为了让棋子稍微浮在棋盘上
    }
}