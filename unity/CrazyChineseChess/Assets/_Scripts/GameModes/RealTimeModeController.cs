// File: _Scripts/GameModes/RealTimeModeController.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using FishNet.Object;

/// <summary>
/// 实时模式的核心逻辑执行器。
/// 负责驱动棋子的状态更新、战斗判定和移动动画。
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
        Debug.Log("[System] 实时模式控制器：已为所有棋子初始化实时状态(RTState)。");
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
    /// [Server Only] 当服务器端的棋子移动动画完成时，由PieceComponent的RPC回调调用。
    /// </summary>
    public void Server_OnMoveAnimationComplete(PieceComponent pc)
    {
        if (!movingPieces.Contains(pc)) return;

        movingPieces.Remove(pc);
    }

    /// <summary>
    /// [Server-Side Logic] 服务器执行移动指令。
    /// 这个版本不再直接调用RPC，而是返回需要播放动画的棋子组件。
    /// </summary>
    /// <returns>返回被移动的PieceComponent，以便上层调用者（GameManager）可以对其发起RPC。</returns>
    public PieceComponent ExecuteMoveCommand(Vector2Int from, Vector2Int to)
    {
        PieceComponent pieceToMove = boardRenderer.GetPieceComponentAt(from);
        if (pieceToMove == null)
        {
            Debug.LogError($"[Server-RTController] 尝试移动一个不存在的棋子，位置: {from}");
            return null;
        }

        // --- 服务器权威逻辑 ---
        PlayerColor movingColor = pieceToMove.Color.Value;
        Debug.Log($"[Server-Action] {movingColor}方 {pieceToMove.name} 开始移动到 {to}。");

        // 1. 更新服务器的逻辑棋盘状态
        boardState.RemovePieceAt(from);

        // 2. 更新服务器上棋子的实时状态
        pieceToMove.RTState.IsMoving = true;
        pieceToMove.RTState.MoveStartPos = from;
        pieceToMove.RTState.MoveEndPos = to;
        movingPieces.Add(pieceToMove);

        // 3. 返回这个棋子，让GameManager来处理网络同步
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