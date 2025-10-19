// File: _Scripts/BoardRenderer.cs
using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// ����BoardState�е��߼����ݡ���Ⱦ��Ϊ�����е�3D����
/// �����������Ӻ�UI��ǵĴ��������١��ƶ��͸�����
/// </summary>
public class BoardRenderer : MonoBehaviour
{
    // --- ��Unity�༭����ָ������Դ ---
    [Header("Prefabs & Materials")]
    public GameObject gamePiecePrefab;  // ���ӵ�3Dģ��Ԥ�Ƽ�
    public Material redPieceMaterial;   // ����Ĳ���
    public Material blackPieceMaterial; // ����Ĳ���

    [Header("UI & Effects")]
    public GameObject moveMarkerPrefab; // ���ƶ�λ�õ���ʾ��� (С��Ƭ)
    public Color attackHighlightColor = new Color(1f, 0.2f, 0.2f); // �ɹ������ӵĸ�����ɫ (��Ϊ��ɫ��ֱ��)

    // --- �ڲ�״̬���� ---
    private List<GameObject> activeMarkers = new List<GameObject>(); // �洢��ǰ��ʾ�������ƶ����
    private List<PieceComponent> highlightedPieces = new List<PieceComponent>(); // �洢��ǰ������������
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT]; // ��ά���飬���ڿ���ͨ�������������GameObject

    /// <summary>
    /// ���ݴ���ĺϷ��ƶ��б�����������ʾ������ʾ��
    /// </summary>
    /// <param name="moves">���кϷ��ƶ��������б�</param>
    /// <param name="movingPieceColor">�����ƶ������ӵ���ɫ</param>
    /// <param name="boardState">��ǰ������״̬</param>
    public void ShowValidMoves(List<Vector2Int> moves, PlayerColor movingPieceColor, BoardState boardState)
    {
        ClearAllHighlights(); // ����ʾ�±��ǰ��������оɵ�

        foreach (var move in moves)
        {
            Piece targetPiece = boardState.GetPieceAt(move);
            if (targetPiece.Type != PieceType.None) // ���Ŀ���������
            {
                if (targetPiece.Color != movingPieceColor) // �����ǵз�����
                {
                    PieceComponent pc = GetPieceComponentAt(move);
                    if (pc != null) HighlightPiece(pc, attackHighlightColor); // �����õз�����
                }
            }
            else // ���Ŀ����ǿո�
            {
                Vector3 markerPos = GetLocalPosition(move.x, move.y);
                markerPos.y += 0.001f; // ��΢̧�ߣ���ֹ������ƽ�洩ģ
                GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
                marker.transform.localPosition = markerPos;

                // ��������Collider��Component���Ա㱻���߼�⵽
                var collider = marker.GetComponent<SphereCollider>();
                if (collider == null) collider = marker.AddComponent<SphereCollider>();
                collider.radius = 0.0175f; // ���õ���뾶Ϊ���Ӱ뾶

                var markerComp = marker.GetComponent<MoveMarkerComponent>();
                if (markerComp == null) markerComp = marker.AddComponent<MoveMarkerComponent>();
                markerComp.BoardPosition = move; // ��¼�ñ�Ƕ�Ӧ����������

                activeMarkers.Add(marker);
            }
        }
    }

    /// <summary>
    /// ������������еĸ���Ч�����ƶ���ǡ�
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
                propBlock.SetColor("_EmissionColor", Color.black); // ���Է�����ɫ����Ϊ��
                renderer.SetPropertyBlock(propBlock);
            }
        }
        highlightedPieces.Clear();
    }

    /// <summary>
    /// �����������ӣ�ͨ�����ò��ʵ��Է�����ɫʵ�֡�
    /// </summary>
    private void HighlightPiece(PieceComponent piece, Color color)
    {
        var renderer = piece.GetComponent<MeshRenderer>();
        var propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);

        propBlock.SetColor("_EmissionColor", color * 2.0f); // ����һ��ϵ����������
        renderer.SetPropertyBlock(propBlock);
        highlightedPieces.Add(piece);
    }

    /// <summary>
    /// ����������ͨ������������ٻ�ȡ���Ӷ����PieceComponent��
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

    // UV����ӳ���ֵ䣬���ڴ���ͼ����ѡ����ȷ����������
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
    /// ����BoardState���ݣ���ȫ���»����������̡�ͨ��ֻ����Ϸ��ʼʱ���á�
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
    /// �����������ӵ�GameObject�������������ϡ�
    /// </summary>
    private void CreatePieceObject(Piece piece, Vector2Int position)
    {
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

    /// <summary>
    /// ���Ӿ����ƶ�һ�����ӣ�GameObject����
    /// </summary>
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

    /// <summary>
    /// ���Ӿ����Ƴ�һ�����ӣ�GameObject����
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
    /// ���������������̸�������ת��Ϊ����ڴ˶���ı���3D���ꡣ
    /// </summary>
    private Vector3 GetLocalPosition(int x, int y)
    {
        // --- ��Ƴ��� ---
        const float boardLogicalWidth = 0.45f;
        const float boardLogicalHeight = 0.45f * (10f / 9f);

        // --- ���� ---
        float cellWidth = boardLogicalWidth / (BoardState.BOARD_WIDTH - 1);
        float cellHeight = boardLogicalHeight / (BoardState.BOARD_HEIGHT - 1);

        float xOffset = boardLogicalWidth / 2f;
        float zOffset = boardLogicalHeight / 2f;

        float xPos = x * cellWidth - xOffset;
        float zPos = y * cellHeight - zOffset;

        float pieceHeight = 0.0175f;

        // ������һ������ֵ���������� CS0161 ����
        return new Vector3(xPos, pieceHeight / 2f, zPos);
    }
}