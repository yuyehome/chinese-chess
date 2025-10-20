using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;

/// <summary>
/// ����BoardState�е��߼�������ȾΪ�����е�3D����
/// �����������Ӻ��Ӿ�Ԫ�صĴ��������١��ƶ��͸�����֧�ֶ������ͬʱ�����ƶ���
/// </summary>
public class BoardRenderer : MonoBehaviour
{
    [Header("Prefabs & Materials")]
    [Tooltip("���ӵ�3Dģ��Ԥ�Ƽ�")]
    public GameObject gamePiecePrefab;
    public Material redPieceMaterial;
    public Material blackPieceMaterial;

    [Header("UI & Effects")]
    [Tooltip("ѡ������ʱ��ʾ�ı��Ԥ�Ƽ�")]
    public GameObject selectionMarkerPrefab;
    [Tooltip("���ƶ�λ�õ���ʾ���Ԥ�Ƽ�")]
    public GameObject moveMarkerPrefab;
    [Tooltip("�ɹ������ӵĸ�����ɫ")]
    public Color attackHighlightColor = new Color(1f, 0.2f, 0.2f);

    [Header("Animation Settings")]
    [Tooltip("�����ƶ��ٶ� (��λ/��)")]
    public float moveSpeed = 0.5f;
    [Tooltip("������Ծ�߶�")]
    public float jumpHeight = 0.1f;

    // �ڲ�״̬����
    private List<GameObject> activeMarkers = new List<GameObject>();
    private List<PieceComponent> highlightedPieces = new List<PieceComponent>();
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT];
    private GameObject activeSelectionMarker = null;

    // UV����ӳ���ֵ䣬���ڴ���ͼ����ѡ����ȷ����������
    private readonly Dictionary<PieceType, Vector2> uvOffsets = new Dictionary<PieceType, Vector2>()
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
    /// ���Ӿ����ƶ�һ�����ӣ���Ϊ��ʵʱģʽ�ṩ״̬���»ص���
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to, BoardState boardState, bool isCapture, Action<PieceComponent, float> onProgressUpdate = null, Action<PieceComponent> onComplete = null)
    {
        GameObject pieceToMoveGO = GetPieceObjectAt(from);
        if (pieceToMoveGO != null)
        {
            PieceComponent pc = pieceToMoveGO.GetComponent<PieceComponent>();

            // ���� pieceObjects ���飬�����Ӿ��ƶ��ġ��߼�˲�䡱
            pieceObjects[to.x, to.y] = pieceToMoveGO;
            pieceObjects[from.x, from.y] = null;
            if (pc != null) pc.BoardPosition = to;

            // ���㶯������
            Vector3 startPos = GetLocalPosition(from.x, from.y);
            Vector3 endPos = GetLocalPosition(to.x, to.y);
            Piece pieceData = boardState.GetPieceAt(to);
            bool isJump = IsJumpingPiece(pieceData.Type, isCapture);

            // Ϊ����ƶ�����һ��ȫ�µġ�������Э��
            StartCoroutine(MovePieceCoroutine(pc, startPos, endPos, isJump, onProgressUpdate, onComplete));
        }
    }

    /// <summary>
    /// �ƶ������ĺ���Э�̣�������������ƽ���ƶ���
    /// </summary>
    private IEnumerator MovePieceCoroutine(PieceComponent piece, Vector3 startPos, Vector3 endPos, bool isJump, Action<PieceComponent, float> onProgressUpdate, Action<PieceComponent> onComplete)
    {
        // ������Ӷ����ڶ�����ʼǰ����ʧЧ����ֱ���˳�
        if (piece == null) yield break;

        float journeyDuration = Vector3.Distance(startPos, endPos) / moveSpeed;
        if (journeyDuration <= 0) journeyDuration = 0.01f; // ��ֹ�������
        float elapsedTime = 0f;

        while (elapsedTime < journeyDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsedTime / journeyDuration);
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);

            if (isJump)
            {
                currentPos.y += Mathf.Sin(percent * Mathf.PI) * jumpHeight;
            }

            // ���ûص�������ǰ���ȴ��ݸ��߼��㣨��RealTimeModeController��
            onProgressUpdate?.Invoke(piece, percent);

            // ����������ƶ������б����٣���ȫ�˳�Э��
            if (piece == null || piece.gameObject == null)
            {
                yield break;
            }
            piece.transform.localPosition = currentPos;

            yield return null;
        }

        // ȷ����������ʱ���Ӿ�ȷ���յ�λ��
        if (piece != null && piece.gameObject != null)
        {
            piece.transform.localPosition = endPos;
        }

        // �������ʱ��������ɻص�
        onComplete?.Invoke(piece);
    }

    #region Public Utility Methods

    /// <summary>
    /// ����BoardState���ݣ���ȫ���»����������̡�ͨ��ֻ����Ϸ��ʼʱ���á�
    /// </summary>
    public void RenderBoard(BoardState boardState)
    {
        // ����ɵ����̶���
        foreach (Transform child in transform) Destroy(child.gameObject);
        System.Array.Clear(pieceObjects, 0, pieceObjects.Length);

        // �����µ����Ӷ���
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
    /// ���ݴ���ĺϷ��ƶ��б�����������ʾ������ʾ��
    /// </summary>
    public void ShowValidMoves(List<Vector2Int> moves, PlayerColor movingPieceColor, BoardState boardState)
    {
        ClearAllHighlights();
        foreach (var move in moves)
        {
            Piece targetPiece = boardState.GetPieceAt(move);
            if (targetPiece.Type != PieceType.None) // ���Ŀ���������
            {
                if (targetPiece.Color != movingPieceColor) // �����ǵз�����
                {
                    PieceComponent pc = GetPieceComponentAt(move);
                    if (pc != null) HighlightPiece(pc, attackHighlightColor);
                }
            }
            else // ���Ŀ����ǿո�
            {
                Vector3 markerPos = GetLocalPosition(move.x, move.y);
                markerPos.y += 0.001f; // ��΢̧�ߣ���ֹ������ƽ�洩ģ
                GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
                marker.transform.localPosition = markerPos;
                var collider = marker.GetComponent<SphereCollider>() ?? marker.AddComponent<SphereCollider>();
                collider.radius = 0.0175f;
                var markerComp = marker.GetComponent<MoveMarkerComponent>() ?? marker.AddComponent<MoveMarkerComponent>();
                markerComp.BoardPosition = move;
                activeMarkers.Add(marker);
            }
        }
    }

    /// <summary>
    /// ������������еĸ���Ч�����ƶ���Ǻ�ѡ���ǡ�
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
                propBlock.SetColor("_EmissionColor", Color.black);
                renderer.SetPropertyBlock(propBlock);
            }
        }
        highlightedPieces.Clear();

        if (activeSelectionMarker != null)
        {
            Destroy(activeSelectionMarker);
            activeSelectionMarker = null;
        }
    }

    /// <summary>
    /// ��ָ�������Ϸ���ʾѡ���ǡ�
    /// </summary>
    public void ShowSelectionMarker(Vector2Int position)
    {
        if (activeSelectionMarker != null) Destroy(activeSelectionMarker);
        if (selectionMarkerPrefab == null) return;

        GameObject pieceGO = GetPieceObjectAt(position);
        if (pieceGO != null)
        {
            activeSelectionMarker = Instantiate(selectionMarkerPrefab, this.transform);
            Vector3 markerPosition = pieceGO.transform.localPosition + new Vector3(0, 0.03f, 0); // ���������Ϸ�
            activeSelectionMarker.transform.localPosition = markerPosition;
        }
    }

    /// <summary>
    /// ���Ӿ����Ƴ�һ�����ӣ�GameObject����
    /// </summary>
    public void RemovePieceAt(Vector2Int position)
    {
        GameObject pieceToRemove = GetPieceObjectAt(position);
        if (pieceToRemove != null)
        {
            Destroy(pieceToRemove);
            pieceObjects[position.x, position.y] = null;
        }
    }

    /// <summary>
    /// ͨ������������ٻ�ȡ���Ӷ����PieceComponent��
    /// </summary>
    public PieceComponent GetPieceComponentAt(Vector2Int position)
    {
        GameObject pieceGO = GetPieceObjectAt(position);
        return pieceGO != null ? pieceGO.GetComponent<PieceComponent>() : null;
    }

    /// <summary>
    /// ͨ������������ٻ�ȡ����GameObject��
    /// </summary>
    public GameObject GetPieceObjectAt(Vector2Int position)
    {
        if (position.x >= 0 && position.x < BoardState.BOARD_WIDTH && position.y >= 0 && position.y < BoardState.BOARD_HEIGHT)
        {
            return pieceObjects[position.x, position.y];
        }
        return null;
    }

    #endregion

    #region Private Helper Methods

    /// <summary>
    /// �����������ӵ�GameObject�������������ϡ�
    /// </summary>
    private void CreatePieceObject(Piece piece, Vector2Int position)
    {
        Vector3 localPosition = GetLocalPosition(position.x, position.y);
        GameObject pieceGO = Instantiate(gamePiecePrefab, this.transform);
        pieceGO.transform.localPosition = localPosition;
        pieceGO.name = $"{piece.Color}_{piece.Type}_{position.x}_{position.y}";

        // �������������
        PieceComponent pc = pieceGO.GetComponent<PieceComponent>();
        if (pc != null)
        {
            pc.BoardPosition = position;
            pc.PieceData = piece;
        }

        // ���ò��ʺ���ͼ
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

        // �洢��GameObject������
        pieceObjects[position.x, position.y] = pieceGO;
    }

    /// <summary>
    /// �����������ӣ�ͨ�����ò��ʵ��Է�����ɫʵ�֡�
    /// </summary>
    private void HighlightPiece(PieceComponent piece, Color color)
    {
        var renderer = piece.GetComponent<MeshRenderer>();
        var propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_EmissionColor", color * 2.0f); // ����ϵ����������
        renderer.SetPropertyBlock(propBlock);
        highlightedPieces.Add(piece);
    }

    /// <summary>
    /// �����������ͺ��ƶ�����ж��Ƿ�Ӧ��ִ����Ծ������
    /// </summary>
    private bool IsJumpingPiece(PieceType type, bool isCapture)
    {
        switch (type)
        {
            case PieceType.Horse:
            case PieceType.Elephant: return true;
            case PieceType.Cannon: return isCapture;
            default: return false;
        }
    }

    /// <summary>
    /// �����̸�������ת��Ϊ����ڴ˶���ı���3D���ꡣ
    /// </summary>
    private Vector3 GetLocalPosition(int x, int y)
    {
        const float boardLogicalWidth = 0.45f;
        const float boardLogicalHeight = 0.45f * (10f / 9f);
        float cellWidth = boardLogicalWidth / (BoardState.BOARD_WIDTH - 1);
        float cellHeight = boardLogicalHeight / (BoardState.BOARD_HEIGHT - 1);
        float xOffset = boardLogicalWidth / 2f;
        float zOffset = boardLogicalHeight / 2f;
        float xPos = x * cellWidth - xOffset;
        float zPos = y * cellHeight - zOffset;
        float pieceHeight = 0.0175f;
        return new Vector3(xPos, pieceHeight / 2f, zPos);
    }

    #endregion
}