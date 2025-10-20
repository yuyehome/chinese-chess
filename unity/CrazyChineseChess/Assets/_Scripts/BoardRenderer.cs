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
    public GameObject selectionMarkerPrefab; // ����������Inspector��ָ��ѡ���ǵ�Ԥ�Ƽ�
    public GameObject moveMarkerPrefab; // ���ƶ�λ�õ���ʾ��� (С��Ƭ)
    public Color attackHighlightColor = new Color(1f, 0.2f, 0.2f); // �ɹ������ӵĸ�����ɫ (��Ϊ��ɫ��ֱ��)

    [Header("Animation Settings")]
    public float moveSpeed = 0.5f; // �����ƶ��ٶ� (��λ/��)
    public float jumpHeight = 0.1f;  // ������Ծ�߶�

    // --- �ڲ�״̬���� ---
    private GameObject activeSelectionMarker = null; // �����������ڴ洢��ǰ��ѡ����ʵ��
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

        // �����������ѡ����
        if (activeSelectionMarker != null)
        {
            Destroy(activeSelectionMarker);
            activeSelectionMarker = null;
        }

    }

    /// <summary>
    /// ������������ʾѡ�����ӵı�ǡ�
    /// </summary>
    public void ShowSelectionMarker(Vector2Int position)
    {
        // 1. ����κο��ܴ��ڵľɱ��
        if (activeSelectionMarker != null)
        {
            Destroy(activeSelectionMarker);
        }

        // 2. ���Ԥ�Ƽ��Ƿ����
        if (selectionMarkerPrefab == null)
        {
            Debug.LogWarning("SelectionMarkerPrefab δ�� BoardRenderer ��ָ����");
            return;
        }

        // 3. ��ȡ��ѡ�е����ӵ�GameObject
        GameObject pieceGO = GetPieceObjectAt(position);
        if (pieceGO != null)
        {
            // 4. �����������������ʵ����Ϊ pieceGO ���Ӷ���
            //    ������������ϵ������������ӵģ����һ��Զ����������ƶ���
            activeSelectionMarker = Instantiate(selectionMarkerPrefab, pieceGO.transform);

            // 5. ���������������ñ�ǵľֲ�λ�� (localPosition)���������������Ϸ�
            //    ��Ϊ������������ӱ�����������ֻ��Ҫһ���򵥵�����ƫ�ơ�
            activeSelectionMarker.transform.localPosition = new Vector3(0, 0.03f, 0);

            // 6. ����ѡ�������ϣ����ǲ���������ת�������������ľֲ���ת
            activeSelectionMarker.transform.localRotation = Quaternion.identity;
        }
    }

    /// <summary>
    /// ������������������ͨ������������ٻ�ȡ���ӵ�GameObject��
    /// </summary>
    public GameObject GetPieceObjectAt(Vector2Int position)
    {
        // �ȼ�������Ƿ������̵���Ч��Χ��
        if (position.x >= 0 && position.x < BoardState.BOARD_WIDTH &&
            position.y >= 0 && position.y < BoardState.BOARD_HEIGHT)
        {
            // �����Ч���򷵻ش洢�ڶ�ά�����е�GameObject
            return pieceObjects[position.x, position.y];
        }

        // ���������Ч���򷵻�null
        return null;
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
        if (pc != null)
        {
            pc.BoardPosition = position;
            pc.PieceData = piece; // �������������ӵ��߼����ݴ������
        }

        //if (piece.Color == PlayerColor.Red) pieceGO.transform.Rotate(0, 95, 0, Space.World);
        //else if (piece.Color == PlayerColor.Black) pieceGO.transform.Rotate(0, -85, 0, Space.World);

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
    /// �������޸ġ����Ӿ����ƶ�һ�����ӡ�
    /// ����������ڻ�����һ��Э����ִ��ƽ�����ƶ�������
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to, BoardState boardState, bool isCapture)
    {
        GameObject pieceToMove = pieceObjects[from.x, from.y];
        if (pieceToMove != null)
        {
            Vector3 startPos = GetLocalPosition(from.x, from.y);
            Vector3 endPos = GetLocalPosition(to.x, to.y);
            Piece pieceData = boardState.GetPieceAt(to);

            // ���޸ġ�ֱ��ʹ�ô���� isCapture ����
            bool isJump = IsJumpingPiece(pieceData.Type, isCapture);

            pieceObjects[to.x, to.y] = pieceToMove;
            pieceObjects[from.x, from.y] = null;
            PieceComponent pc = pieceToMove.GetComponent<PieceComponent>();
            if (pc != null) pc.BoardPosition = to;

            StartCoroutine(MovePieceCoroutine(pieceToMove, startPos, endPos, isJump));
        }
    }

    /// <summary>
    /// �ƶ������ĺ���Э�̡�
    /// </summary>
    private System.Collections.IEnumerator MovePieceCoroutine(GameObject piece, Vector3 startPos, Vector3 endPos, bool isJump)
    {
        //GameManager.Instance.SetAnimating(true);
        float journeyDuration = Vector3.Distance(startPos, endPos) / moveSpeed;
        if (journeyDuration <= 0) journeyDuration = 0.1f; // ��ֹ�������
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
            if (piece != null) piece.transform.localPosition = currentPos;
            yield return null;
        }
        if (piece != null) piece.transform.localPosition = endPos;
        //GameManager.Instance.SetAnimating(false);
    }

    /// <summary>
    /// �������������������������������ͺ��ƶ�����ж��Ƿ�Ӧ��ִ����Ծ������
    /// ʹ�� pieceObjects ��������ȷ�ж����Ƿ��ڳ��ӡ�
    /// </summary>
    private bool IsJumpingPiece(PieceType type, bool isCapture)
    {
        switch (type)
        {
            case PieceType.Horse:
            case PieceType.Elephant:
                return true;

            case PieceType.Cannon:
                // ��ֻ���ڳ���ʱ����Ծ
                return isCapture;

            default:
                return false;
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