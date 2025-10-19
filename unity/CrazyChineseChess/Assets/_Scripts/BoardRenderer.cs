// File: _Scripts/BoardRenderer.cs
using UnityEngine;
using System.Collections.Generic;

public class BoardRenderer : MonoBehaviour
{
    // --- ��Դ���� ---
    [Header("Prefabs & Materials")]
    public GameObject gamePiecePrefab;
    public Material redPieceMaterial;
    public Material blackPieceMaterial;
    
    [Header("UI & Effects")]
    public GameObject moveMarkerPrefab;
    public Color attackHighlightColor = new Color(1f, 0.2f, 0.2f, 1f); // ���������ø���Ŀ�ĺ�ɫ
    
    private List<GameObject> activeMarkers = new List<GameObject>();
    private List<PieceComponent> highlightedPieces = new List<PieceComponent>();
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT];

    // ... GetLocalPosition �� UV �ֵ�Ȳ��ֱ��ֲ��䣬Ϊȷ��������ȫ������ ...

    //region Public Methods for Rendering & Highlighting

    /// <summary>
    /// ������Ⱦ�������̣�ͨ��ֻ����Ϸ��ʼʱ����һ��
    /// </summary>
    public void RenderBoard(BoardState boardState)
    {
        // ����ɶ���
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
    /// [����] ���Ӿ����ƶ�һ������
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        GameObject pieceToMove = pieceObjects[from.x, from.y];
        if (pieceToMove != null)
        {
            // TODO: δ���������������ƽ�����ƶ����� (e.g., using LeanTween or DoTween)
            pieceToMove.transform.localPosition = GetLocalPosition(to.x, to.y);
            
            // �������������е�����
            pieceObjects[to.x, to.y] = pieceToMove;
            pieceObjects[from.x, from.y] = null;
            
            // �����������������Ϣ
            PieceComponent pc = pieceToMove.GetComponent<PieceComponent>();
            if (pc != null)
            {
                pc.BoardPosition = to;
            }
        }
    }

    /// <summary>
    /// [����] ���Ӿ����Ƴ�һ������
    /// </summary>
    public void RemovePieceAt(Vector2Int position)
    {
        GameObject pieceToRemove = pieceObjects[position.x, position.y];
        if (pieceToRemove != null)
        {
            // TODO: δ���������������������Ч
            Destroy(pieceToRemove);
            pieceObjects[position.x, position.y] = null;
        }
    }
    
    public void ShowValidMoves(List<Vector2Int> moves, PlayerColor movingPieceColor, BoardState boardState)
    {
        ClearAllHighlights();
        
        foreach (var move in moves)
        {
            Piece targetPiece = boardState.GetPieceAt(move);
            if (targetPiece.Type != PieceType.None)
            {
                // ȷ���ǵз�����
                if (targetPiece.Color != movingPieceColor)
                {
                    PieceComponent pc = GetPieceComponentAt(move);
                    if (pc != null) HighlightPiece(pc, attackHighlightColor);
                }
            }
            else
            {
                Vector3 markerPos = GetLocalPosition(move.x, move.y);
                // ��΢̧��һ�㣬��ֹ�����̴�ģ
                markerPos.y += 0.001f; 
                GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
                marker.transform.localPosition = markerPos;
                activeMarkers.Add(marker);
            }
        }
    }

    public void ClearAllHighlights()
    {
        foreach (var marker in activeMarkers) Destroy(marker);
        activeMarkers.Clear();
        
        foreach (var pc in highlightedPieces)
        {
            if (pc != null)
            {
                var renderer = pc.GetComponent<MeshRenderer>();
                if(renderer == null) continue;
                var propBlock = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(propBlock);
                propBlock.SetColor("_EmissionColor", Color.black);
                renderer.SetPropertyBlock(propBlock);
            }
        }
        highlightedPieces.Clear();
    }
    
    //endregion
    
    #region Private Helper Methods

    /// <summary>
    /// [����] ��װ�������������Ӿ�������߼�
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

        // ��������GameObject�������ǵĶ�ά�����Ա���ٷ���
        pieceObjects[position.x, position.y] = pieceGO;
    }

    private void HighlightPiece(PieceComponent piece, Color color)
    {
        var renderer = piece.GetComponent<MeshRenderer>();
        if(renderer == null) return;
        var propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        
        propBlock.SetColor("_EmissionColor", color * 2.0f); 
        renderer.SetPropertyBlock(propBlock);

        highlightedPieces.Add(piece);
    }
    
    public PieceComponent GetPieceComponentAt(Vector2Int position)
    {
        if (boardState.IsWithinBounds(position))
        {
            GameObject pieceGO = pieceObjects[position.x, position.y];
            if (pieceGO != null) return pieceGO.GetComponent<PieceComponent>();
        }
        return null;
    }
    
    private Vector3 GetLocalPosition(int x, int y)
    {
        const float boardLogicalWidth = 0.45f; 
        const float boardLogicalHeight = 0.45f * (10f / 9f);
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
    
    // (С�Ż�) BoardState��IsWithinBounds����������Ҳ������
    private BoardState boardState = new BoardState();

    #endregion
}