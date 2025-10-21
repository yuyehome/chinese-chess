// File: _Scripts/GameModes/RealTimeModeController.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// ʵʱģʽ�ĺ��Ŀ�������
/// �������޻غ������£������ж����ѡ���ƶ��������Լ�����״̬��ʵʱ���¡�
/// </summary>
public class RealTimeModeController : GameModeController
{
    // --- ����ģ�� ---
    private readonly EnergySystem energySystem;

    public CombatManager CombatManager { get; private set; }

    // --- �ڲ�״̬ ---
    // �洢���������ƶ��е����ӣ�����ÿ֡���и������ǵ�״̬
    private readonly List<PieceComponent> movingPieces = new List<PieceComponent>();
    // �����ϴ�Ϊѡ�����Ӽ���ĺϷ��ƶ��б����ڼ��仯�Ծ����Ƿ��ػ����
    private List<Vector2Int> lastCalculatedValidMoves = new List<Vector2Int>();

    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem, float collisionDistanceSquared)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
        // ������ײ����������ʵ����CombatManager
        this.CombatManager = new CombatManager(state, renderer, collisionDistanceSquared);
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
                        pc.RTState.LogicalPosition = pos; // ��ʼ�����ӵ��߼�λ��
                    }
                }
            }
        }
        Debug.Log("[System] ʵʱģʽ����������Ϊ�������ӳ�ʼ��ʵʱ״̬(RTState)��");
    }

    #region Main Logic Loop

    /// <summary>
    /// ��GameManagerÿ֡���ã���Ϊʵʱ�߼�����������
    /// </summary>
    public void Tick()
    {
        UpdateAllPieceStates();
        CombatManager.ProcessCombat(GetAllActivePieces());
        UpdateSelectionHighlights();
    }

    /// <summary>
    /// ���������ƶ��е����ӣ������������ͺ��ƶ����ȣ�ʵʱ�����乥��/����״̬���߼�λ�á�
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

            // 1. �����ƶ������ӵ��߼�λ��
            UpdatePieceLogicalPosition(pc);

            // 2. ���ݹ���Ӧ�ò�ͬ�Ĺ���״̬�仯
            state.IsAttacking = false;
            state.IsVulnerable = true;

            switch (type)
            {
                case PieceType.Chariot:
                case PieceType.Soldier:
                case PieceType.General:
                case PieceType.Advisor:
                    state.IsAttacking = true;
                    state.IsVulnerable = true;
                    break;
                case PieceType.Cannon:
                    if (progress > 0.9f) { state.IsAttacking = true; }
                    state.IsVulnerable = true;
                    break;
                case PieceType.Horse:
                case PieceType.Elephant:
                    if (progress > 0.8f) { state.IsAttacking = true; }
                    if (progress > 0.1f && progress < 0.8f) { state.IsVulnerable = false; }
                    break;
            }

            // ������־�����ڴ�ӡ�ƶ������ӵ�״̬
            if (Time.frameCount % 300 == 0)
            {
                Debug.Log($"[State-Update] �ƶ�������: {pc.name}, type:{type}, Progress: {progress:F2}, LogicalPos: {pc.RTState.LogicalPosition}, Attacking: {pc.RTState.IsAttacking}, Vulnerable: {pc.RTState.IsVulnerable}");
            }
        }
    }

    /// <summary>
    /// �������ӱ�ѡ��ʱ��ÿ֡��鲢������Ϸ��ƶ�����Ӿ�������
    /// ����ʵ�֡���̬�ڼܡ���ʵʱս���Ĺؼ���
    /// </summary>
    private void UpdateSelectionHighlights()
    {
        if (selectedPiece == null) return;

        BoardState logicalBoard = GetLogicalBoardState();
        List<Vector2Int> newValidMoves = RuleEngine.GetValidMoves(selectedPiece.PieceData, selectedPiece.RTState.LogicalPosition, logicalBoard);

        // �����Ϸ��ƶ��б����仯ʱ���ػ棬���Ż�����
        if (!newValidMoves.SequenceEqual(lastCalculatedValidMoves))
        {
            currentValidMoves = newValidMoves;
            lastCalculatedValidMoves = new List<Vector2Int>(newValidMoves);

            boardRenderer.ClearAllHighlights();
            boardRenderer.ShowValidMoves(currentValidMoves, selectedPiece.PieceData.Color, logicalBoard);
            boardRenderer.ShowSelectionMarker(selectedPiece.RTState.LogicalPosition);
        }
    }

    #endregion

    #region Player Input Handling

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
                    PerformMove(selectedPiece, clickedPiece.BoardPosition);
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
            PerformMove(selectedPiece, marker.BoardPosition);
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

    #endregion

    #region Helper Methods

    /// <summary>
    /// ����ѡ��һ�����ӣ��˲��������ж����Ƿ��㹻��
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
    /// ��װ��ִ��һ���ƶ��������߼����衣
    /// </summary>
    private void PerformMove(PieceComponent pieceToMove, Vector2Int targetPosition)
    {
        PlayerColor movingColor = pieceToMove.PieceData.Color;
        Debug.Log($"[Action] {movingColor}�� {pieceToMove.name} ��ʼ�ƶ��� {targetPosition}��");

        // 1. ���������ڲ�״̬�����Ϊ���ƶ��С�����¼·��
        pieceToMove.RTState.IsMoving = true;
        pieceToMove.RTState.MoveStartPos = pieceToMove.BoardPosition;
        pieceToMove.RTState.MoveEndPos = targetPosition;
        movingPieces.Add(pieceToMove);

        // 2. ����GameManagerִ���ƶ���������ص���������״̬����
        gameManager.ExecuteMove(
            pieceToMove.BoardPosition,
            targetPosition,
            // OnProgress: �������Ź����еĻص������ڸ��½���
            (pc, progress) => {
                if (pc != null && pc.RTState != null) pc.RTState.MoveProgress = progress;
            },
            // OnComplete: �������ʱ�Ļص�
            (pc) => {
                // ��ִ���κ������߼�ǰ�������������Ƿ�����;����ɱ
                if (pc != null && pc.RTState != null && !pc.RTState.IsDead)
                {
                    // ֻ�д������Ӳ���ִ�����Ӻ�״̬����
                    boardState.SetPieceAt(pc.RTState.MoveEndPos, pc.PieceData);
                    pc.BoardPosition = pc.RTState.MoveEndPos;
                    pc.RTState.ResetToDefault(pc.RTState.MoveEndPos);
                    movingPieces.Remove(pc);
                    Debug.Log($"[State] {pc.name} �ƶ���ɣ�״̬�������� {pc.RTState.MoveEndPos}��");
                }
                else if (pc != null)
                {
                    // �����������;������ֻ��ȷ�������ƶ��б����Ƴ�
                    movingPieces.Remove(pc);
                    Debug.Log($"[State] ������������ {pc.name} ������������ִ�������߼���");
                }
            }
        );

        // 3. ��������
        energySystem.SpendEnergy(movingColor);

        // 4. ����ǰ��ѡ��״̬����������ǵȣ�
        ClearSelection();
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

            // �������������ж������ƶ����Ƿ�Ϊ�߼��谭��
            switch (piece.PieceData.Type)
            {
                // ��Ծ��λ�ڿ��в������谭
                case PieceType.Horse:
                case PieceType.Elephant:
                case PieceType.Cannon:
                    break;
                // ʵ�嵥λ���ƶ��л�ʵʱ�����谭
                case PieceType.Chariot:
                case PieceType.Soldier:
                case PieceType.General:
                case PieceType.Advisor:
                default:
                    logicalBoard.SetPieceAt(piece.RTState.LogicalPosition, piece.PieceData);
                    break;
            }
        }
        return logicalBoard;
    }

    /// <summary>
    /// ͨ�����Բ�ֵ�������ƶ������ӵ�ǰ���ڵ��߼��������ꡣ
    /// </summary>
    private void UpdatePieceLogicalPosition(PieceComponent piece)
    {
        float progress = piece.RTState.MoveProgress;
        Vector2 start = piece.RTState.MoveStartPos;
        Vector2 end = piece.RTState.MoveEndPos;

        float logicalX = Mathf.Lerp(start.x, end.x, progress);
        float logicalY = Mathf.Lerp(start.y, end.y, progress);

        // �������뵽����ĸ���
        piece.RTState.LogicalPosition = new Vector2Int(Mathf.RoundToInt(logicalX), Mathf.RoundToInt(logicalY));
    }

    /// <summary>
    /// ��ȡ���������д�����ӵ��б�����ս����⡣
    /// </summary>
    private List<PieceComponent> GetAllActivePieces()
    {
        List<PieceComponent> allPieces = new List<PieceComponent>();

        // 1. ��������ƶ��е�����
        allPieces.AddRange(movingPieces.Where(p => p != null && !p.RTState.IsDead));

        // 2. ���� BoardState ������о�ֹ������
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Vector2Int pos = new Vector2Int(x, y);
                if (boardState.GetPieceAt(pos).Type != PieceType.None)
                {
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

    #endregion
}