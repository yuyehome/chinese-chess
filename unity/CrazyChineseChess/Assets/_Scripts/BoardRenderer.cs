// File: _Scripts/BoardRenderer.cs
using UnityEngine;
using System.Collections.Generic; // ��Ҫʹ���ֵ�

public class BoardRenderer : MonoBehaviour
{
    // --- ��Դ���� ---
    [Header("Prefabs & Materials")]
    public GameObject gamePiecePrefab; // �µ�����Prefab
    public Material redPieceMaterial;  // �������
    public Material blackPieceMaterial;// �������

    // --- UV����ӳ�� ---
    // ����ֵ䶨����ÿ�����ӵ���������ͼ��(Atlas)�ϵ�UVƫ����
    // Key: PieceType
    // Value: Vector2(x_offset, y_offset)
    // �������ǵ���ͼ���� 4x2 ������
    private Dictionary<PieceType, Vector2> uvOffsets = new Dictionary<PieceType, Vector2>()
    {
        // �����һ��: ˧, ��, ��, ��
        { PieceType.General,   new Vector2(0.0f, 0.5f) },
        { PieceType.Advisor,   new Vector2(0.25f, 0.5f) },
        { PieceType.Elephant,  new Vector2(0.5f, 0.5f) },
        { PieceType.Chariot,   new Vector2(0.75f, 0.5f) },
        // ����ڶ���: ��, ��, ��
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
                    
                    // 1. ��ȡ��Ⱦ�����
                    MeshRenderer renderer = pieceGO.GetComponent<MeshRenderer>();
                    if (renderer == null) continue;

                    // 2. ��̬��������ʵ���������޸Ĺ������
                    MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
                    renderer.GetPropertyBlock(propBlock);

                    // 3. ������ɫѡ���������
                    Material baseMaterial = (piece.Color == PlayerColor.Red) ? redPieceMaterial : blackPieceMaterial;
                    renderer.material = baseMaterial;

                    // 4. ����UVƫ������ʾ��ȷ������
                    if (uvOffsets.ContainsKey(piece.Type))
                    {
                        Vector2 offset = uvOffsets[piece.Type];
                        // "_MainTex_ST" ��Unity��׼��ɫ���п�����ͼTiling��Offset��������
                        // Vector4(Tiling.x, Tiling.y, Offset.x, Offset.y)
                        propBlock.SetVector("_MainTex_ST", new Vector4(0.25f, 0.5f, offset.x, offset.y));
                    }
                    
                    renderer.SetPropertyBlock(propBlock);

                    pieceObjects[x, y] = pieceGO;
                }
            }
        }
    }

    // GetLocalPosition �������ֲ���
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