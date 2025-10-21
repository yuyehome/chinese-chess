using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ʵʱģʽ�ĺ��Ŀ�������
/// �������޻غ������£������ж����ѡ���ƶ��������Լ�����״̬��ʵʱ���¡�
/// </summary>
public class RealTimeModeController : GameModeController
{
    private readonly EnergySystem energySystem;

    private readonly CombatManager combatManager;

    // �洢���������ƶ��е����ӣ�����ÿ֡���и������ǵ�״̬
    private readonly List<PieceComponent> movingPieces = new List<PieceComponent>();

    // �����ϴμ���ĺϷ��ƶ������ڼ��仯
    private List<Vector2Int> lastCalculatedValidMoves = new List<Vector2Int>();

    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
        this.combatManager = new CombatManager(state, renderer);
    }

    /// <summary>
    /// ��ʼ���������������ӵ�ʵʱ״̬���ݡ��˷���������BoardRenderer�����Ⱦ����á�
    /// </summary>
    public void InitializeRealTimeStates()
    {
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (boardState.GetPieceAt(pos).Type != PieceType.None)
                {
                    PieceComponent pc = boardRenderer.GetPieceComponentAt(pos);
                    if (pc != null)
                    {
                        pc.RTState = new RealTimePieceState();
                        // ��ʼ�����ӵ��߼�λ��
                        pc.RTState.LogicalPosition = pos;
                    }
                }
            }
        }
        Debug.Log("[System] ʵʱģʽ����������Ϊ�������ӳ�ʼ��ʵʱ״̬(RTState)��");
    }

    /// <summary>
    /// ��GameManagerÿ֡���ã���Ϊʵʱ�߼�����������
    /// </summary>
    public void Tick()
    {
        UpdateAllPieceStates();
        combatManager.ProcessCombat(GetAllActivePieces()); // ���޸ġ����������б�
        UpdateSelectionHighlights();
    }

    /// <summary>
    /// ���������ƶ��е����ӣ������������ͺ��ƶ����ȣ�ʵʱ�����乥��/����״̬��
    /// </summary>
    private void UpdateAllPieceStates()
    {
        // �Ӻ���ǰ�������԰�ȫ����ѭ�����Ƴ�Ԫ��
        for (int i = movingPieces.Count - 1; i >= 0; i--)
        {
            PieceComponent pc = movingPieces[i];
            if (pc == null || pc.RTState == null || pc.RTState.IsDead)
            {
                movingPieces.RemoveAt(i);
                continue;
            }

            RealTimePieceState state = pc.RTState;
            PieceType type = pc.PieceData.Type;
            float progress = state.MoveProgress;

            // ÿ֡��ʼʱ��������Ϊ�ƶ��еĻ���״̬
            state.IsAttacking = false;
            state.IsVulnerable = true;

            // �����ƶ������ӵ��߼�λ��
            UpdatePieceLogicalPosition(pc);

            // ���ݹ���Ӧ�ò�ͬ��״̬�仯
            switch (type)
            {
                case PieceType.Chariot:
                case PieceType.Soldier:
                case PieceType.General:
                case PieceType.Advisor:
                    // ʵ���ƶ����ӣ�ȫ�̱��ֹ����ԺͿɱ�����
                    state.IsAttacking = true;
                    state.IsVulnerable = true;
                    break;
                case PieceType.Cannon:
                    // ������Ծ���������׶βž��й�����
                    if (progress > 0.9f) { state.IsAttacking = true; }
                    state.IsVulnerable = true;
                    break;
                case PieceType.Horse:
                case PieceType.Elephant:
                    // ��������ƶ����ξ��й�����
                    if (progress > 0.8f) { state.IsAttacking = true; }
                    // ���ƶ��м�׶δ����޵�״̬
                    if (progress > 0.1f && progress < 0.8f) { state.IsVulnerable = false; }
                    break;
            }

            if (Time.frameCount % 60 == 0)
            {
                Debug.Log($"[State-Update] �ƶ�������: {pc.name}, Progress: {progress:F2}, LogicalPos: {pc.RTState.LogicalPosition}, Attacking: {pc.RTState.IsAttacking}, Vulnerable: {pc.RTState.IsVulnerable}");
            }

        }
    }

    /// <summary>
    /// ������ҵ�����ӵ��¼���
    /// </summary>
    public override void OnPieceClicked(PieceComponent clickedPiece)
    {
        Debug.Log($"[Input] ��ҵ��������: {clickedPiece.name}");
        Piece clickedPieceData = boardState.GetPieceAt(clickedPiece.BoardPosition);

        // ��֧1: ����ѡ�����ӣ��ұ��ε�����ǵз����ӣ���ͼ���ӣ�
        if (selectedPiece != null && clickedPieceData.Color != selectedPiece.PieceData.Color)
        {
            if (currentValidMoves.Contains(clickedPiece.BoardPosition))
            {
                // �����Ա�̣�ȷ��ѡ�е�����״̬����
                if (selectedPiece.RTState == null)
                {
                    Debug.LogError($"[Error] ���ش���ѡ�е����� {selectedPiece.name} û��ʵʱ״̬(RTState)��");
                    ClearSelection();
                    return;
                }

                if (selectedPiece.RTState.IsMoving)
                {
                    Debug.Log($"[Action] ����ʧ��: {selectedPiece.name} �����ƶ��У����ɲ�����");
                    ClearSelection();
                    return;
                }

                if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
                {
                    PerformMove(selectedPiece, clickedPiece.BoardPosition, true);
                }
                else
                {
                    Debug.Log($"[Action] ����ʧ��: {selectedPiece.PieceData.Color}���ж��㲻�㣬�޷����ӡ�");
                    ClearSelection();
                }
            }
            else
            {
                // ����˷Ƿ��ĵз�Ŀ�꣬��Ϊ�л�ѡ��
                TrySelectPiece(clickedPiece);
            }
        }
        // ��֧2: ����������ӣ���δѡ���κ����ӣ���ͼѡ��
        else
        {
            TrySelectPiece(clickedPiece);
        }
    }

    /// <summary>
    /// ������ҵ���ƶ���ǵ��¼���
    /// </summary>
    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        Debug.Log($"[Input] ��ҵ�����ƶ���ǣ�Ŀ������: {marker.BoardPosition}");
        if (selectedPiece == null) return;

        // �����Ա�̣�ȷ��ѡ�е�����״̬����
        if (selectedPiece.RTState == null)
        {
            Debug.LogError($"[Error] ���ش���ѡ�е����� {selectedPiece.name} û��ʵʱ״̬(RTState)��");
            ClearSelection();
            return;
        }

        if (selectedPiece.RTState.IsMoving)
        {
            Debug.Log($"[Action] ����ʧ��: {selectedPiece.name} �����ƶ��У����ɲ�����");
            ClearSelection();
            return;
        }

        if (energySystem.CanSpendEnergy(selectedPiece.PieceData.Color))
        {
            PerformMove(selectedPiece, marker.BoardPosition, false);
        }
        else
        {
            Debug.Log($"[Action] ����ʧ��: {selectedPiece.PieceData.Color}���ж��㲻�㣬�޷��ƶ���");
            ClearSelection();
        }
    }

    /// <summary>
    /// ������ҵ�����̿հ�������¼���
    /// </summary>
    public override void OnBoardClicked(RaycastHit hit)
    {
        Debug.Log("[Input] ��ҵ�������̿հ�����ȡ��ѡ��");
        ClearSelection();
    }

    /// <summary>
    /// ����ѡ��һ�����ӣ������ж����Ƿ��㹻��
    /// </summary>
    private void TrySelectPiece(PieceComponent pieceToSelect)
    {
        Piece pieceData = boardState.GetPieceAt(pieceToSelect.BoardPosition);
        if (energySystem.CanSpendEnergy(pieceData.Color))
        {
            SelectPiece(pieceToSelect);
            Debug.Log($"[Action] �ɹ�ѡ������ {pieceToSelect.name}��");
        }
        else
        {
            Debug.Log($"[Action] ѡ��ʧ��: {pieceData.Color}���ж��㲻�㡣");
            ClearSelection();
        }
    }

    /// <summary>
    /// ��װ��ִ���ƶ��ĺ����߼�����OnPieceClicked��OnMarkerClicked���á�
    /// </summary>
    private void PerformMove(PieceComponent pieceToMove, Vector2Int targetPosition, bool isCapture)
    {
        PlayerColor movingColor = pieceToMove.PieceData.Color;
        string moveType = isCapture ? "����" : "�ƶ�";
        Debug.Log($"[Action] {movingColor}�� {pieceToMove.name} ��ʼ {moveType} �� {targetPosition}��");

        // ����Ҫ���������ƶ�ʱ���������Ƿ�Ӧ�ò�����Ծ����
        bool isCannonJump = pieceToMove.PieceData.Type == PieceType.Cannon && isCapture;

        // ����1: ���������ڲ�״̬�������뵽�ƶ��б���
        pieceToMove.RTState.IsMoving = true;

        pieceToMove.RTState.MoveStartPos = pieceToMove.BoardPosition;
        pieceToMove.RTState.MoveEndPos = targetPosition;

        movingPieces.Add(pieceToMove);

        // ����2: ����GameManagerִ���ƶ���������ص���������״̬����
        gameManager.ExecuteMove(
            pieceToMove.BoardPosition,
            targetPosition,
            // OnProgress: ���������еĻص�
            (pc, progress) => {
                if (pc != null && pc.RTState != null) pc.RTState.MoveProgress = progress;
            },
            // OnComplete: �������ʱ�Ļص�
            (pc) => {
                if (pc != null && pc.RTState != null)
                {

                    // ����������ʽ��������
                    boardState.SetPieceAt(pc.RTState.MoveEndPos, pc.PieceData);
                    pc.BoardPosition = pc.RTState.MoveEndPos;
                    pc.RTState.ResetToDefault(pc.RTState.MoveEndPos);
                    movingPieces.Remove(pc);
                    Debug.Log($"[State] {pc.name} �ƶ���ɣ�״̬�������� {pc.RTState.MoveEndPos}��");

                }
            }
        );

        // ����3: ��������
        energySystem.SpendEnergy(movingColor);

        // ����4: ����ѡ��״̬
        ClearSelection();
    }


    /// <summary>
    /// �������ӱ�ѡ��ʱ��ÿ֡��鲢������Ϸ��ƶ�����Ӿ�������
    /// </summary>
    private void UpdateSelectionHighlights()
    {
        if (selectedPiece == null) return;

        // 1. ��ȡ��ǰ�ġ����⡱����״̬
        BoardState logicalBoard = GetLogicalBoardState();

        // 2. ���¼���Ϸ��ƶ�
        // ע�⣺����������ʱ����ʹ�þɵ�RuleEngine��������ʶʵʱ״̬�����ܶ�ȡ��������
        // �����Ҫ�����ӵ�ʵʱ�����紩�ˣ�������Ҫ���� RealTimeRuleEngine
        List<Vector2Int> newValidMoves = RuleEngine.GetValidMoves(selectedPiece.PieceData, selectedPiece.RTState.LogicalPosition, logicalBoard);

        // 3. ����б��Ƿ��б仯
        if (!newValidMoves.SequenceEqual(lastCalculatedValidMoves))
        {
            // 4. ����б仯������µ�ǰ�Ϸ��ƶ��б��ػ����
            currentValidMoves = newValidMoves;
            lastCalculatedValidMoves = new List<Vector2Int>(newValidMoves); // ���봴�����б�

            // ����ɸ�������ʾ�¸���
            boardRenderer.ClearAllHighlights();
            boardRenderer.ShowValidMoves(currentValidMoves, selectedPiece.PieceData.Color, logicalBoard);
            // ������ʾѡ���ǣ���Ϊ�����ܱ�ClearAllHighlights�����
            boardRenderer.ShowSelectionMarker(selectedPiece.RTState.LogicalPosition);
        }
    }

    /// <summary>
    /// ��̬����һ����ӳ��ǰ֡���������߼�λ�õġ����⡱����״̬��
    /// </summary>
    private BoardState GetLogicalBoardState()
    {
        BoardState logicalBoard = boardState.Clone(); // �������о�ֹ����
        foreach (var piece in movingPieces)
        {
            if (piece.RTState.IsDead) continue;

            // TODO: ������Ը������Ĺ�����չ�����硰���ޡ�״̬�����Ӳ������赲
            // Ŀǰ�������ƶ��е����Ӷ������߼�λ���ϲ����赲
            logicalBoard.SetPieceAt(piece.RTState.LogicalPosition, piece.PieceData);
        }
        return logicalBoard;
    }

    /// <summary>
    /// �������ӵ�3D�������꣬���������䵱ǰ���ڵ��߼��������ꡣ
    /// </summary>
    private void UpdatePieceLogicalPosition(PieceComponent piece)
    {
        // ����������Ƚϸ��ӣ���������һ���򻯵����Բ�ֵ��ģ��
        // �ڵ�֡���¿��ܲ���ȷ����������֤�߼�
        float progress = piece.RTState.MoveProgress;
        Vector2 start = piece.RTState.MoveStartPos;
        Vector2 end = piece.RTState.MoveEndPos;

        // ���Բ�ֵ��������λ��
        float logicalX = Mathf.Lerp(start.x, end.x, progress);
        float logicalY = Mathf.Lerp(start.y, end.y, progress);

        // �������뵽����ĸ���
        piece.RTState.LogicalPosition = new Vector2Int(Mathf.RoundToInt(logicalX), Mathf.RoundToInt(logicalY));
    }

    /// <summary>
    /// ��ȡ���������д�����ӵ��б�
    /// </summary>
    // ���޸ġ�GetAllActivePieces ������ʹ�䲻������ pieceObjects ����
    private List<PieceComponent> GetAllActivePieces()
    {
        List<PieceComponent> allPieces = new List<PieceComponent>();

        // 1. ��ȡ�����ƶ��е�����
        allPieces.AddRange(movingPieces.Where(p => p != null && !p.RTState.IsDead));

        // 2. ���� BoardState ��ȡ���о�ֹ������
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (boardState.GetPieceAt(pos).Type != PieceType.None)
                {
                    // ͨ�� BoardRenderer ��ȡ GameObject��������ȡ Component
                    // ��һ�������ǰ�ȫ�ģ���Ϊ��ֹ���ӵ� pieceObjects ��������ȷ��
                    PieceComponent pc = boardRenderer.GetPieceComponentAt(pos);
                    if (pc != null && pc.RTState != null && !pc.RTState.IsDead)
                    {
                        allPieces.Add(pc);
                    }
                }
            }
        }

        return allPieces.Distinct().ToList(); // ȥ�ز�����
    }

}