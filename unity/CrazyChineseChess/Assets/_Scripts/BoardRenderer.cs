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
    /// </summary>
    private Vector3 GetLocalPosition(int x, int y)
    {
        // 这个计算逻辑本身是正确的，它计算的是以(0,0,0)为中心的偏移量，
        // 这正是本地坐标所需要的。
        float boardWidthUnits = 9.0f;
        float boardHeightUnits = 10.0f;
        
        // 假设棋盘模型的尺寸与我们的单位尺寸匹配。
        // Plane的默认大小是10x10，我们之前把它Scale成了(1, 1, 1.2)。
        // 所以它的实际宽度是10个单位，高度是12个单位。
        // 为了精确匹配，我们需要根据实际模型尺寸调整。
        // 让我们用一个更健壮的方法：
        Renderer boardRenderer = GetComponentInChildren<Renderer>(); // 获取子对象(BoardPlane)的渲染器
        if (boardRenderer == null) {
             Debug.LogError("BoardVisual 下找不到带Renderer的棋盘平面！");
             return Vector3.zero;
        }

        Vector3 boardSize = boardRenderer.bounds.size;

        // X轴：从 -boardSize.x / 2 到 +boardSize.x / 2
        float xPos = (float)x / (BoardState.BOARD_WIDTH - 1) * boardSize.x - (boardSize.x / 2f);
        
        // Z轴：从 -boardSize.z / 2 到 +boardSize.z / 2
        // 注意：Unity Plane的"高度"是在Z轴上
        float zPos = (float)y / (BoardState.BOARD_HEIGHT - 1) * boardSize.z - (boardSize.z / 2f);

        // 返回本地坐标。Y值是棋子的高度。
        return new Vector3(xPos, 0.1f, zPos);
    }


}