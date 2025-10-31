// File: _Scripts/BoardRenderer.cs

using UnityEngine;
using System.Collections.Generic;
using System;
using System.Collections;
using FishNet;

/// <summary>
/// �Ӿ���Ⱦ����ģ�����BoardState�е��߼�������ȾΪ�����е�3D����
/// �����������Ӻ��Ӿ�Ԫ�صĴ��������١��ƶ��͸�����֧�ֶ������ͬʱ�����ƶ���
/// </summary>
public class BoardRenderer : MonoBehaviour
{
    public static BoardRenderer Instance { get; private set; }

    /// <summary>
    /// ��BoardRenderer�ĵ���Instance׼����ʱ������
    /// </summary>
    public static event Action OnInstanceReady;


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
    [Tooltip("�����ƶ��ٶ� (��/��)")]
    [SerializeField] private float moveSpeed = 0.2f;
    [Tooltip("������Ծ�����ĸ߶�")]
    [SerializeField] private float jumpHeight = 0.1f;

    private int defaultLayer;
    private int etherealLayer;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        OnInstanceReady?.Invoke(); // ֪ͨ���ж����ߣ����Ѿ�׼������

        // �ڿ�ʼʱ����Layer������ֵ����ÿ�����ַ������Ҹ���Ч
        defaultLayer = LayerMask.NameToLayer("Default");
        etherealLayer = LayerMask.NameToLayer("EtherealPieces");
    }

    // --- �ڲ�״̬ ---
    // �洢��ǰ��ʾ�������ƶ����
    private List<GameObject> activeMarkers = new List<GameObject>();
    // �洢��ǰ������������
    private List<PieceComponent> highlightedPieces = new List<PieceComponent>();
    // ��ά���飬����ͨ��������ٲ������ӵ�GameObject�����Ӿ����ֵ�Ψһ��ʵ��Դ
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT];
    // ��ǰ�����ѡ����ʵ��
    private GameObject activeSelectionMarker = null;

    // UV����ӳ���ֵ䣬���ڴ���ͼ����ѡ����ȷ����������
    public readonly Dictionary<PieceType, Vector2> uvOffsets = new Dictionary<PieceType, Vector2>()
    {
        { PieceType.General,   new Vector2(0.0f, 0.5f) },
        { PieceType.Advisor,   new Vector2(0.25f, 0.5f) },
        { PieceType.Elephant,  new Vector2(0.5f, 0.5f) },
        { PieceType.Chariot,   new Vector2(0.75f, 0.5f) },
        { PieceType.Horse,     new Vector2(0.0f, 0.0f) },
        { PieceType.Cannon,    new Vector2(0.25f, 0.0f) },
        { PieceType.Soldier,   new Vector2(0.5f, 0.0f) },
    };

    #region Public Action Methods

    /// <summary>
    /// ���Ӿ�������һ�����ӵ��ƶ�������
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to, Action<PieceComponent, float> onProgressUpdate = null, Action<PieceComponent> onComplete = null)
    {
        GameObject pieceToMoveGO = GetPieceObjectAt(from);
        if (pieceToMoveGO == null)
        {
            Debug.LogWarning($"[Renderer] �����ƶ�һ�������� {from} �ϲ����ڵ����ӡ�");
            return;
        }

        PieceComponent pc = pieceToMoveGO.GetComponent<PieceComponent>();
        if (pc == null || pc.PieceData.Type == PieceType.None)
        {
            Debug.LogError($"[Error] �����ƶ������� {pieceToMoveGO.name} û����Ч�� PieceComponent �� PieceData��");
            return;
        }

        // 1. ���Ӿ������н����Ӵ���㡰���𡱣�ʹ�����ƶ��в�ռ����ʼ��
        pieceObjects[from.x, from.y] = null;
        pc.BoardPosition = to; // ��������ڵ�Ŀ��λ��

        // 2. ���㶯������
        Vector3 startPos = GetLocalPosition(from.x, from.y);
        Vector3 endPos = GetLocalPosition(to.x, to.y);
        bool isJump = IsJumpingPiece(pc.PieceData.Type);

        // �����ƶ������л�Layer
        SetLayerRecursively(pieceToMoveGO, etherealLayer);

        // 3. ����Э�̣�����װ onComplete �ص����ڶ������������ pieceObjects ����


        StartCoroutine(MovePieceCoroutine(pc, startPos, endPos, isJump, onProgressUpdate,
            (completedPiece) => {
                // ������ɺ���Ŀ��λ�ü�¼ GameObject
                if (completedPiece != null && completedPiece.RTState != null && !completedPiece.RTState.IsDead)
                {
                    SetLayerRecursively(completedPiece.gameObject, defaultLayer);
                    pieceObjects[to.x, to.y] = completedPiece.gameObject;
                }
                // ����ԭʼ�� onComplete �ص������磬������������״̬��
                onComplete?.Invoke(completedPiece);
            }
        ));
    }

    /// <summary>
    /// �ݹ������һ��GameObject���������Ӷ����Layer��
    /// </summary>
    private void SetLayerRecursively(GameObject obj, int layer)
    {
        if (obj == null) return;
        obj.layer = layer;
        foreach (Transform child in obj.transform)
        {
            if (child == null) continue;
            SetLayerRecursively(child.gameObject, layer);
        }
    }

    /// <summary>
    /// �ƶ������ĺ���Э�̣�������������ƽ���ƶ���
    /// </summary>
    private IEnumerator MovePieceCoroutine(PieceComponent piece, Vector3 startPos, Vector3 endPos, bool isJump, Action<PieceComponent, float> onProgressUpdate, Action<PieceComponent> onComplete)
    {
        if (piece == null) yield break;

        float journeyDuration = Vector3.Distance(startPos, endPos) / moveSpeed;
        if (journeyDuration <= 0) journeyDuration = 0.01f;
        float elapsedTime = 0f;

        while (elapsedTime < journeyDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsedTime / journeyDuration);

            // ����������ƶ������б����٣���ȫ�˳�Э��
            if (piece == null || piece.gameObject == null)
            {
                yield break;
            }

            // ���㵱ǰ֡��λ��
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);
            if (isJump)
            {
                currentPos.y += Mathf.Sin(percent * Mathf.PI) * jumpHeight;
            }
            piece.transform.localPosition = currentPos;

            // ���ûص�������ǰ���ȴ��ݸ��߼���
            onProgressUpdate?.Invoke(piece, percent);

            yield return null;
        }

        // ֻ�е�Э��������ɣ�δ����;���٣�ʱ����ִ����β�߼�
        if (piece != null && piece.gameObject != null)
        {
            piece.transform.localPosition = endPos;
            onComplete?.Invoke(piece);
        }
    }

    #endregion

    #region Public Utility & Setup Methods

    /// <summary>
    /// �����������������õ������ӵ��Ӿ����֣����ʺ�UV����
    /// </summary>
    public void SetupPieceVisuals(PieceComponent pc)
    {
        MeshRenderer renderer = pc.GetComponent<MeshRenderer>();
        if (renderer == null) return;

        MaterialPropertyBlock propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);

        // 1. ���ò��ʣ�ͨ�� .Value ����ͬ������
        renderer.material = (pc.Color.Value == PlayerColor.Red) ? redPieceMaterial : blackPieceMaterial;

        // 2. ����UVƫ�ƣ�ͨ�� .Value ����ͬ������
        if (uvOffsets.ContainsKey(pc.Type.Value))
        {
            Vector2 offset = uvOffsets[pc.Type.Value];
            propBlock.SetVector("_MainTex_ST", new Vector4(0.25f, 0.5f, offset.x, offset.y));
        }

        //�ڷ���� ���ӵ�����
        //if (InstanceFinder.IsClient)
        if (pc.Color.Value == PlayerColor.Black)
        {
            pc.transform.rotation = Quaternion.Euler(-90, 0, 180);
        }

        // 3. ���ø߹�
        propBlock.SetColor("_EmissionColor", Color.black);
        renderer.SetPropertyBlock(propBlock);

        // 4. (��Ҫ) ���Ӿ�������ע���������
        pieceObjects[pc.BoardPosition.x, pc.BoardPosition.y] = pc.gameObject;
    }

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
    /// ���ݴ���ĺϷ��ƶ��б�����������ʾ������ʾ��
    /// </summary>
    public void ShowValidMoves(List<Vector2Int> moves, PlayerColor movingPieceColor, BoardState boardState)
    {
        ClearAllHighlights();
        foreach (var move in moves)
        {
            Piece targetPiece = boardState.GetPieceAt(move);
            if (targetPiece.Type != PieceType.None) // Ŀ���������
            {
                if (targetPiece.Color != movingPieceColor) // �����ǵз�����
                {
                    PieceComponent pc = GetPieceComponentAt(move);
                    if (pc != null) HighlightPiece(pc, attackHighlightColor);
                }
            }
            else // Ŀ����ǿո�
            {
                InstantiateMoveMarker(move);
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
            Vector3 markerPosition = pieceGO.transform.localPosition + new Vector3(0, 0.03f, 0);
            activeSelectionMarker.transform.localPosition = markerPosition;
        }
    }

    /// <summary>
    /// ֱ�Ӹ���PieceComponent�Ƴ����ӵ��Ӿ�����
    /// ����������ڴ����ƶ��б���ɱ�����ӣ���Ϊ����������pieceObjects���顣
    /// </summary>
    /// <param name="pieceToRemove">Ҫ�Ƴ������������</param>
    public void RemovePiece(PieceComponent pieceToRemove)
    {
        if (pieceToRemove != null && pieceToRemove.gameObject != null)
        {
            Destroy(pieceToRemove.gameObject);
        }
        else
        {
            Debug.LogWarning($"[Renderer] ����ֱ���Ƴ�һ���յ�PieceComponent����GameObject��");
        }
    }

    /// <summary>
    /// ���Ӿ����Ƴ�һ�����ӣ�������GameObject����
    /// </summary>
    public void RemovePieceAt(Vector2Int position)
    {
        Debug.Log($"[Renderer] ���Դ����� {position} ���Ҳ��Ƴ�һ����ֹ�����ӡ�");

        GameObject pieceToRemove = GetPieceObjectAt(position);
        if (pieceToRemove != null)
        {
            Debug.Log($"[Renderer] ���ڴ����� {position} �Ƴ�GameObject: {pieceToRemove.name}��");
            Destroy(pieceToRemove);
            pieceObjects[position.x, position.y] = null;
        }
        else
        {
            Debug.LogWarning($"[Renderer] �����Ƴ����� {position} �����ӣ���δ�ҵ�GameObject��");
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
    /// ����������ڡ������ڵ���ģʽ����RenderBoard���á�
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
            // ���ǵ���ģʽ������ֱ������PieceComponent�ı��ر�����
            // ���� SyncVar<T> ���͵��ֶΣ�������Ҫ���� .Value ���Ը�ֵ��
            // ��Ȼ�ڵ���ģʽ������ͬ���������ã���Ϊ�˱������״̬��һ���ԣ�
            // ����������ȷ�ġ�
            pc.Type.Value = piece.Type;
            pc.Color.Value = piece.Color;
            pc.BoardPosition = position;
        }

        // ���ù��������������Ӿ�Ч��
        if (pc != null)
        {
            // ע�⣺��Ϊ���ǵ���ģʽ�����ǲ���Ҫ������ģʽ�����ӳ�һ֡��
            // ֱ�ӵ��ü��ɡ�
            SetupPieceVisuals(pc);
        }

        // �洢��GameObject������ (��һ��������SetupPieceVisuals��)
    }

    /// <summary>
    /// ��ָ������ʵ����һ���ƶ���ǡ�
    /// </summary>
    private void InstantiateMoveMarker(Vector2Int position)
    {
        Vector3 markerPos = GetLocalPosition(position.x, position.y);
        markerPos.y += 0.001f; // ��΢̧�ߣ���ֹ������ƽ�洩ģ
        GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
        marker.transform.localPosition = markerPos;
        var collider = marker.GetComponent<SphereCollider>() ?? marker.AddComponent<SphereCollider>();
        collider.radius = 0.0175f;
        var markerComp = marker.GetComponent<MoveMarkerComponent>() ?? marker.AddComponent<MoveMarkerComponent>();
        markerComp.BoardPosition = position;
        activeMarkers.Add(marker);
    }

    /// <summary>
    /// �����������ӣ�ͨ�����ò��ʵ��Է�����ɫʵ�֡�
    /// </summary>
    private void HighlightPiece(PieceComponent piece, Color color)
    {
        var renderer = piece.GetComponent<MeshRenderer>();
        var propBlock = new MaterialPropertyBlock();
        renderer.GetPropertyBlock(propBlock);
        propBlock.SetColor("_EmissionColor", color * 2.0f);
        renderer.SetPropertyBlock(propBlock);
        highlightedPieces.Add(piece);
    }

    /// <summary>
    /// �������������ж��Ƿ�Ӧ��ִ����Ծ������
    /// </summary>
    private bool IsJumpingPiece(PieceType type)
    {
        switch (type)
        {
            case PieceType.Horse:
            case PieceType.Elephant:
            case PieceType.Cannon:
                return true;
            default:
                return false;
        }
    }

    /// <summary>
    /// �����̸�������ת��Ϊ����ڴ˶���ı���3D���ꡣ
    /// </summary>
    public Vector3 GetLocalPosition(int x, int y)
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

    /// <summary>
    /// ������3D���귴��ת��Ϊ���̸������ꡣ
    /// ��Ҫ���ͻ�������������ʱȷ���Լ����߼�λ�á�
    /// </summary>
    public Vector2Int GetBoardPosition(Vector3 localPosition)
    {
        const float boardLogicalWidth = 0.45f;
        const float boardLogicalHeight = 0.45f * (10f / 9f);
        float cellWidth = boardLogicalWidth / (BoardState.BOARD_WIDTH - 1);
        float cellHeight = boardLogicalHeight / (BoardState.BOARD_HEIGHT - 1);
        float xOffset = boardLogicalWidth / 2f;
        float zOffset = boardLogicalHeight / 2f;

        int x = Mathf.RoundToInt((localPosition.x + xOffset) / cellWidth);
        int y = Mathf.RoundToInt((localPosition.z + zOffset) / cellHeight);

        return new Vector2Int(x, y);
    }

    #endregion
}