// File: _Scripts/BoardRenderer.cs
using UnityEngine;
using System.Collections.Generic; // 需要使用字典

public class BoardRenderer : MonoBehaviour
{
    // --- 资源引用 ---
    [Header("Prefabs & Materials")]
    public GameObject gamePiecePrefab; // 新的棋子Prefab
    public Material redPieceMaterial;  // 红棋材质
    public Material blackPieceMaterial;// 黑棋材质

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

        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Piece piece = boardState.GetPieceAt(new Vector2Int(x, y));
                if (piece.Type != PieceType.None)
                {
                    Vector3 localPosition = GetLocalPosition(x, y);
                    GameObject pieceGO = Instantiate(gamePiecePrefab, this.transform);
                    pieceGO.transform.localPosition = localPosition;
                    pieceGO.name = $"{piece.Color}_{piece.Type}_{x}_{y}";
                    
                    // 1. 获取渲染器组件
                    MeshRenderer renderer = pieceGO.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;

                    // 2. 动态创建材质实例，避免修改共享材质
                    MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(propBlock);

                    // 3. 根据颜色选择基础材质
                    Material baseMaterial = (piece.Color == PlayerColor.Red) ? redPieceMaterial : blackPieceMaterial;
                    renderer.material = baseMaterial;

                    // 4. 设置UV偏移以显示正确的文字
                    if (uvOffsets.ContainsKey(piece.Type))
                    {
                        Vector2 offset = uvOffsets[piece.Type];
                        // "_MainTex_ST" 是Unity标准着色器中控制贴图Tiling和Offset的属性名
                        // Vector4(Tiling.x, Tiling.y, Offset.x, Offset.y)
                        propBlock.SetVector("_MainTex_ST", new Vector4(0.25f, 0.5f, offset.x, offset.y));
                    }
                    
                    renderer.SetPropertyBlock(propBlock);

                    pieceObjects[x, y] = pieceGO;
                }
            }
        }
    }

    // GetLocalPosition 方法保持不变
    private Vector3 GetLocalPosition(int x, int y)
    {
        const float TOTAL_BOARD_WIDTH = 0.45f;
        const float TOTAL_BOARD_HEIGHT = TOTAL_BOARD_WIDTH * (10f / 9f); 
        const float MARGIN_X = 0.025f; 
        const float MARGIN_Y = 0.025f; 
        float playingAreaWidth = TOTAL_BOARD_WIDTH - 2 * MARGIN_X;
        float playingAreaHeight = TOTAL_BOARD_HEIGHT - 2 * MARGIN_Y;
        float cellWidth = playingAreaWidth / (BoardState.BOARD_WIDTH - 1);
        float cellHeight = playingAreaHeight / (BoardState.BOARD_HEIGHT - 1);
        float startX = -playingAreaWidth / 2f;
        float startZ = -playingAreaHeight / 2f;
        float xPos = startX + x * cellWidth;
        float zPos = startZ + y * cellHeight;
        float pieceHeight = 0.0175f; 
        return new Vector3(xPos, pieceHeight / 2f, zPos);
    }
}