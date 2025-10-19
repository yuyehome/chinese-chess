// File: _Scripts/BoardRenderer.cs
using UnityEngine;
using System.Collections.Generic; // ��Ҫʹ���ֵ�
using System.Linq; // ��Ҫʹ��Linq

public class BoardRenderer : MonoBehaviour
{
    // --- ��Դ���� ---
    [Header("Prefabs & Materials")]
    public GameObject gamePiecePrefab; // �µ�����Prefab
    public Material redPieceMaterial;  // �������
    public Material blackPieceMaterial;// �������
    
    [Header("UI & Effects")]
    public GameObject moveMarkerPrefab; // ������ʾ��Prefab
    public Color attackHighlightColor = Color.green; // <-- [����] �����������ɫ����
    private List<GameObject> activeMarkers = new List<GameObject>();
    
    private List<PieceComponent> highlightedPieces = new List<PieceComponent>(); // ����׷�ٱ�����������

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

    // ������и����ͱ��
    public void ClearAllHighlights()
    {
        // ����ƶ����
        foreach (var marker in activeMarkers) Destroy(marker);
        activeMarkers.Clear();
        
        // ������Ӹ߹�
        foreach (var pc in highlightedPieces)
        {
            if (pc != null) // ���ӿ����Ѿ����Ե�
            {
                // �����Է�����ɫ
                var renderer = pc.GetComponent<MeshRenderer>();
                var propBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_EmissionColor", Color.black);
                renderer.SetPropertyBlock(propBlock);
            }
        }
        highlightedPieces.Clear();
    }
    
    // ������������
    private void HighlightPiece(PieceComponent piece, Color color)
    {
        var renderer = piece.GetComponent<MeshRenderer>();
        var propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        
        // �����Է�����ɫ������һ��ϵ����������
        propBlock.SetColor("_EmissionColor", color * 2.0f); 
        renderer.SetPropertyBlock(propBlock);

        highlightedPieces.Add(piece);
    }

    /// ����������ͨ�������ȡ���ӵ�Component (O(1)����)
    public PieceComponent GetPieceComponentAt(Vector2Int position)
    {
        // �޸���ֱ�ӽ�����ѧ�߽��飬������������� boardState ʵ��
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