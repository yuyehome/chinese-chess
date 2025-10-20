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
    // ���޸ġ�������PieceStateController�Լ������������BoardRenderer��Ҫ��¼��Щ�������ˣ��Ա����
    private List<PieceStateController> highlightedControllers = new List<PieceStateController>(); 
    private List<GameObject> activeMarkers = new List<GameObject>(); // �洢��ǰ��ʾ�������ƶ����
    private GameObject[,] pieceObjects = new GameObject[BoardState.BOARD_WIDTH, BoardState.BOARD_HEIGHT]; // ��ά���飬���ڿ���ͨ�������������GameObject

    /// <summary>
    /// �����ع������ݺϷ��ƶ��б���ʾ���ƶ��ı�ǺͿɹ����ĵ��˸�����
    /// </summary>
    public void ShowValidMoves(List<Vector2Int> moves, PlayerColor movingPieceColor, BoardState boardState)
    {
        ClearAllHighlights(); // ��������оɵķ���

        foreach (var move in moves)
        {
            Piece targetPiece = boardState.GetPieceAt(move);

            // ���Ŀ����ǵз�����
            if (targetPiece.Type != PieceType.None && targetPiece.Color != movingPieceColor)
            {
                GameObject targetPieceGO = GetPieceObjectAt(move);
                if (targetPieceGO != null)
                {
                    var psc = targetPieceGO.GetComponent<PieceStateController>();
                    if (psc != null)
                    {
                        psc.Highlight(attackHighlightColor); // ���ø�������
                        highlightedControllers.Add(psc);   // ��¼�����Ա�֮�����
                    }
                }
            }
            // ���Ŀ����ǿո�
            else if (targetPiece.Type == PieceType.None)
            {
                // --- �����ƶ���ǵ��߼����ֲ��� ---
                Vector3 markerPos = GetLocalPosition(move.x, move.y);
                markerPos.y += 0.001f;
                GameObject marker = Instantiate(moveMarkerPrefab, this.transform);
                marker.transform.localPosition = markerPos;

                var collider = marker.GetComponent<SphereCollider>();
                if (collider == null) collider = marker.AddComponent<SphereCollider>();
                collider.radius = 0.0175f;

                var markerComp = marker.GetComponent<MoveMarkerComponent>();
                if (markerComp == null) markerComp = marker.AddComponent<MoveMarkerComponent>();
                markerComp.BoardPosition = move;

                activeMarkers.Add(marker);
            }
        }
    }


    /// <summary>
    /// �����ع���������������еĸ���Ч�����ƶ���Ǻ�ѡ���ǡ�
    /// </summary>
    public void ClearAllHighlights()
    {
        // ����ƶ����
        foreach (var marker in activeMarkers) Destroy(marker);
        activeMarkers.Clear();

        // ���������������
        foreach (var psc in highlightedControllers)
        {
            psc?.ClearHighlight(); // ������������ķ���
        }
        highlightedControllers.Clear();

        // ���ѡ���ǣ���ͷ��
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

        // ����������ʼ��PieceStateController
        PieceStateController psc = pieceGO.GetComponent<PieceStateController>();
        if (psc != null)
        {
            psc.Initialize(piece);
        }
        else
        {
            Debug.LogError("����Prefab��û��PieceStateController��");
        }

        pieceObjects[position.x, position.y] = pieceGO;
    }


    /// <summary>
    /// �������޸ġ�����һ���������ƶ�Э�̣�������isCapture��Ϣ
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to, bool isCapture)
    {
        GameObject pieceToMove = GetPieceObjectAt(from);
        if (pieceToMove != null)
        {
            Vector3 startPos = GetLocalPosition(from.x, from.y);
            Vector3 endPos = GetLocalPosition(to.x, to.y);

            // ���� pieceObjects ����
            pieceObjects[to.x, to.y] = pieceToMove;
            pieceObjects[from.x, from.y] = null;
            PieceComponent pc = pieceToMove.GetComponent<PieceComponent>();
            if (pc != null) pc.BoardPosition = to;

            // ����Э�̣�����isCapture��Ϣ����ȥ
            StartCoroutine(MovePieceCoroutine(pieceToMove, startPos, endPos, isCapture));
        }
    }

    /// <summary>
    /// �ƶ������ĺ���Э�̣����ڸ�������PieceStateController��״̬����
    /// </summary>
    private System.Collections.IEnumerator MovePieceCoroutine(GameObject piece, Vector3 startPos, Vector3 endPos, bool isCapture)
    {
        var stateController = piece.GetComponent<PieceStateController>();
        // ����������ȡ�����ӵ��ƶ�����
        var movementStrategy = PieceStrategyFactory.GetStrategy(stateController.pieceComponent.PieceData.Type);

        if (stateController == null || movementStrategy == null)
        {
            Debug.LogError("�ƶ�������û��PieceStateController��MovementStrategy��");
            yield break;
        }

        // --- ״̬���� ---
        stateController.OnMoveStart();
        // ���ڵ������߼�������ⲽ�ǳ��ӣ�������Ҫ֪ͨ�ڵĲ���
        if (stateController.pieceComponent.PieceData.Type == PieceType.Cannon)
        {
            // ����һ���򻯵�֪ͨ��ʽ�����Ͻ��ķ����ǲ���ģʽ����һ��SetContext����
            // ������ʱͨ���޸�CannonStrategy������
            CannonStrategy.isNextMoveCapture = isCapture;
        }

        float journeyDuration = Vector3.Distance(startPos, endPos) / moveSpeed;
        if (journeyDuration <= 0) journeyDuration = 0.1f;
        float elapsedTime = 0f;

        while (elapsedTime < journeyDuration)
        {
            elapsedTime += Time.deltaTime;
            float percent = Mathf.Clamp01(elapsedTime / journeyDuration);

            // ״̬����
            stateController.OnMoveUpdate(percent);


            // --- �������޸ġ�λ�ø����߼� ---
            Vector3 currentPos = Vector3.Lerp(startPos, endPos, percent);

            // ���������Ӳ��Ի�ȡY��߶Ȳ�Ӧ��
            float currentJumpHeight = movementStrategy.GetJumpHeight(percent, this.jumpHeight);
            currentPos.y = startPos.y + currentJumpHeight; // �ڻ����߶���������Ծƫ��

            if (piece != null)
            {
                // ʹ��Rigidbody.MovePosition���ƶ����Ի����ȷ��������
                piece.GetComponent<Rigidbody>().MovePosition(this.transform.TransformPoint(currentPos));
            }
            else
            {
                yield break;
            }
            yield return null;
        }

        if (piece != null)
        {
            // ȷ������ʱY��ص�ԭλ
            piece.GetComponent<Rigidbody>().MovePosition(this.transform.TransformPoint(endPos));
            stateController.OnMoveEnd();
        }
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
        /// �������������������󣬲���ȫ���������ݺͶ���
        /// ���Ǵ��������Ӿ����ٵ�Ψһ��ڡ�
        /// </summary>
        public void RequestDestroyPiece(GameObject pieceToDestroy)
        {
            if (pieceToDestroy == null) return;

            var pc = pieceToDestroy.GetComponent<PieceComponent>();
            if (pc != null)
            {
                Vector2Int pos = pc.BoardPosition;

                // ȷ���������ٵ��Ǽ�¼�������е�ͬһ������
                if (pieceObjects[pos.x, pos.y] == pieceToDestroy)
                {
                    // ���������Ƴ�����
                    pieceObjects[pos.x, pos.y] = null;
                }
            }

            // ������Ϸ����
            Destroy(pieceToDestroy);
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
