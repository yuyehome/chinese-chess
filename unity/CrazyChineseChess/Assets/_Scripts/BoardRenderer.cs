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
    
    [Header("UI & Effects")]
    public GameObject moveMarkerPrefab; // ������ʾ��Prefab
    private List<GameObject> activeMarkers = new List<GameObject>();
    
    public void ShowValidMoves(List<Vector2Int> moves)
    {
        ClearValidMoves(); // ������ɵ�
        foreach (var move in moves)
        {
            Vector3 markerPos = GetLocalPosition(move.x, move.y);
            GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
            marker.transform.localPosition = markerPos;
            activeMarkers.Add(marker);
        }
    }

    public void ClearValidMoves()
    {
        foreach (var marker in activeMarkers)
        {
            Destroy(marker);
        }
        activeMarkers.Clear();
    }

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
                    
                    // ��������Ϸ���������������ϵ��������
                    PieceComponent pc = pieceGO.GetComponent<PieceComponent>();
                    if (pc != null)
                    {
                        pc.BoardPosition = new Vector2Int(x, y);
                    }

                    // 1. ������λ�ã���Ӧ����ת
                    pieceGO.transform.localPosition = localPosition;

                    // 2. ʹ�� Rotate() �ڵ�ǰ��ת������׷����ת
                    if (piece.Color == PlayerColor.Red)
                    {
                        // ��ɫ���ӣ��ڵ�ǰ�����ϣ�������Y����ת90��
                        pieceGO.transform.Rotate(0, 95, 0, Space.World);
                    }
                    else if (piece.Color == PlayerColor.Black)
                    {
                        // ��ɫ���ӣ��ڵ�ǰ�����ϣ�������Y����ת-90��
                        pieceGO.transform.Rotate(0, -85, 0, Space.World);
                    }

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
    
    /// <summary>
    /// �����̸������� (x,y) ת��Ϊ����ڴ˶���(BoardVisual)�ı������ꡣ
    /// ����汾�ǻ�����Ƴߴ磬��ģ�ͱ����С������ȶ���
    /// ���̶̹��ߴ�45X45cm�����ӹ̶��ߴ�35mm
    /// </summary>
    private Vector3 GetLocalPosition(int x, int y)
    {
        // --- ��Ƴ��� ---
        // �����ڴ����ж������̵��߼��ߴ磬����������ģ�͡�
        // �����ܿ�� (X��, 8�����)
        const float boardLogicalWidth = 0.45f; 
        // �����ܸ߶� (Z��, 9�����)
        const float boardLogicalHeight = 0.45f * (10f / 9f); // ���������㣬�й����������ǳ����ε�
        
        // --- ���� ---
        // ����ÿ�����ӵļ��
        float cellWidth = boardLogicalWidth / (BoardState.BOARD_WIDTH - 1);
        float cellHeight = boardLogicalHeight / (BoardState.BOARD_HEIGHT - 1);
        
        // ����ƫ������ʹ������������ (0,0)
        float xOffset = boardLogicalWidth / 2f;
        float zOffset = boardLogicalHeight / 2f;
        
        // �������ձ�������
        float xPos = x * cellWidth - xOffset;
        float zPos = y * cellHeight - zOffset;

        // ��ȡ���ӵĸ߶ȣ�ʹ��պø���������
        float pieceHeight = 0.0175f; // ��Ӧ��������߶�

        return new Vector3(xPos, pieceHeight / 2f, zPos);
    }

}