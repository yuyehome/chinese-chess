// File: _Scripts/GameModes/RealTimeModeController.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;

/// <summary>
/// ʵʱģʽ�ĺ����߼�ִ������
/// �����������ӵ�״̬���¡�ս���ж����ƶ�������
/// </summary>
public class RealTimeModeController : GameModeController
{
    private readonly EnergySystem energySystem;
    public CombatManager CombatManager { get; private set; }

    private readonly List<PieceComponent> movingPieces = new List<PieceComponent>();

    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer, EnergySystem energySystem, float collisionDistanceSquared)
        : base(manager, state, renderer)
    {
        this.energySystem = energySystem;
        this.CombatManager = new CombatManager(state, renderer, collisionDistanceSquared);
    }

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
                        pc.RTState.LogicalPosition = pos;
                    }
                }
            }
        }
        Debug.Log("[System] ʵʱģʽ����������Ϊ�������ӳ�ʼ��ʵʱ״̬(RTState)��");
    }

    #region Main Logic Loop

    public void Tick()
    {
        UpdateAllPieceStates();
        CombatManager.ProcessCombat(GetAllActivePieces());
    }

    private void UpdateAllPieceStates()
    {
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

            UpdatePieceLogicalPosition(pc);

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
        }
    }
    #endregion

    #region Action Execution

    /// <summary>
    /// [Server Only] ���������˵������ƶ��������ʱ����PieceComponent��RPC�ص����á�
    /// </summary>
    public void Server_OnMoveAnimationComplete(PieceComponent pc)
    {
        if (!movingPieces.Contains(pc)) return;

        movingPieces.Remove(pc);
    }

    /// <summary>
    /// [Server-Side Logic] ������ִ���ƶ�ָ�
    /// ����汾����ֱ�ӵ���RPC�����Ƿ�����Ҫ���Ŷ��������������
    /// </summary>
    /// <returns>���ر��ƶ���PieceComponent���Ա��ϲ�����ߣ�GameManager�����Զ��䷢��RPC��</returns>
    public PieceComponent ExecuteMoveCommand(Vector2Int from, Vector2Int to)
    {
        PieceComponent pieceToMove = boardRenderer.GetPieceComponentAt(from);
        if (pieceToMove == null)
        {
            Debug.LogError($"[Server-RTController] �����ƶ�һ�������ڵ����ӣ�λ��: {from}");
            return null;
        }

        // --- ������Ȩ���߼� ---
        PlayerColor movingColor = pieceToMove.Color.Value;
        Debug.Log($"[Server-Action] {movingColor}�� {pieceToMove.name} ��ʼ�ƶ��� {to}��");

        // 1. ���·��������߼�����״̬
        boardState.RemovePieceAt(from);

        // 2. ���·����������ӵ�ʵʱ״̬
        pieceToMove.RTState.IsMoving = true;
        pieceToMove.RTState.MoveStartPos = from;
        pieceToMove.RTState.MoveEndPos = to;
        movingPieces.Add(pieceToMove);

        // 3. ����������ӣ���GameManager����������ͬ��
        return pieceToMove;
    }

    public BoardState GetLogicalBoardState()
    {
        BoardState logicalBoard = boardState.Clone();
        foreach (var piece in movingPieces)
        {
            if (piece.RTState.IsDead) continue;

            switch (piece.PieceData.Type)
            {
                case PieceType.Horse:
                case PieceType.Elephant:
                case PieceType.Cannon:
                    break;
                default:
                    logicalBoard.SetPieceAt(piece.RTState.LogicalPosition, piece.PieceData);
                    break;
            }
        }
        return logicalBoard;
    }

    private void UpdatePieceLogicalPosition(PieceComponent piece)
    {
        float progress = piece.RTState.MoveProgress;
        Vector2 start = piece.RTState.MoveStartPos;
        Vector2 end = piece.RTState.MoveEndPos;

        float logicalX = Mathf.Lerp(start.x, end.x, progress);
        float logicalY = Mathf.Lerp(start.y, end.y, progress);

        piece.RTState.LogicalPosition = new Vector2Int(Mathf.RoundToInt(logicalX), Mathf.RoundToInt(logicalY));
    }

    public List<PieceComponent> GetAllActivePieces()
    {
        List<PieceComponent> allPieces = new List<PieceComponent>();
        allPieces.AddRange(movingPieces.Where(p => p != null && !p.RTState.IsDead));

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
        return allPieces.Distinct().ToList();
    }
    #endregion
}