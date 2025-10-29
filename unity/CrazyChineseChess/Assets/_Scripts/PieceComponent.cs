// File: _Scripts/PieceComponent.cs
using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// 挂载在棋子Prefab上的网络化组件。
/// 这是适用于最新版FishNet的最终正确实现。
/// </summary>
public class PieceComponent : NetworkBehaviour
{
    // 【核心改动】将 readonly 关键字加回来！
    // 这满足了 FishNet ILPP 的要求。
    public readonly SyncVar<PieceType> Type = new SyncVar<PieceType>();
    public readonly SyncVar<PlayerColor> Color = new SyncVar<PlayerColor>();

    public Piece PieceData => new Piece(Type.Value, Color.Value);

    public Vector2Int BoardPosition { get; set; }
    public RealTimePieceState RTState { get; set; }

    private bool _visualsInitialized = false;

    /// <summary>
    /// [Server-Side Logic]
    /// 初始化 [SyncVar] 的值。
    /// 我们操作的是 .Value，而不是字段本身，所以这与 readonly 规则不冲突。
    /// </summary>
    public void Initialize(Piece piece, Vector2Int position)
    {
        Type.Value = piece.Type;
        Color.Value = piece.Color;
        BoardPosition = position;
        gameObject.name = $"{piece.Color}_{piece.Type}_{position.x}_{position.y}";
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (_visualsInitialized) return;
        SetupVisuals();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        SetupVisuals();
    }

    private void SetupVisuals()
    {
        if (_visualsInitialized) return;

        if (!IsServer)
        {
            this.BoardPosition = BoardRenderer.Instance.GetBoardPosition(this.transform.localPosition);
        }

        if (BoardRenderer.Instance != null)
        {
            Debug.Log($"[{(IsServer ? "Server" : "Client")}] 棋子 {gameObject.name} (Type: {Type.Value}, Color: {Color.Value}) 已生成，正在设置视觉效果。坐标: {BoardPosition}");
            BoardRenderer.Instance.SetupPieceVisuals(this);
            _visualsInitialized = true;
        }
        else
        {
            Debug.LogError($"[{(IsServer ? "Server" : "Client")}] 棋子 {gameObject.name} 生成时，BoardRenderer.Instance 为空！视觉设置失败。");
        }
    }

    /// <summary>
    /// [Observers Rpc] 由服务器调用，命令所有客户端播放此棋子的移动动画。
    /// </summary>
    [ObserversRpc]
    public void Observer_PlayMoveAnimation(Vector2Int from, Vector2Int to)
    {
        Debug.Log($"[{(IsServer ? "Server" : "Client")}] 收到移动指令，棋子 {this.name} 将从 {from} 移动到 {to}");

        // 每个客户端都调用自己的BoardRenderer来播放视觉动画
        // 注意：这里的逻辑回调（onProgressUpdate, onComplete）只在服务器上有意义，
        // 因为只有服务器才需要根据动画进度更新权威的游戏状态。
        if (BoardRenderer.Instance != null)
        {
            BoardRenderer.Instance.MovePiece(
                from, to,
                onProgressUpdate: (pc, progress) => {
                    // 这个回调只在服务器上执行
                    if (!IsServer) return;
                    if (pc != null && pc.RTState != null) pc.RTState.MoveProgress = progress;
                },
                onComplete: (pc) => {
                    // 这个回调也只在服务器上执行
                    if (!IsServer) return;

                    if (pc != null && pc.RTState != null && !pc.RTState.IsDead)
                    {
                        // 服务器在动画完成后更新最终的逻辑状态
                        GameManager.Instance.CurrentBoardState.SetPieceAt(pc.RTState.MoveEndPos, pc.PieceData);
                        pc.BoardPosition = pc.RTState.MoveEndPos;
                        pc.RTState.ResetToDefault(pc.RTState.MoveEndPos);

                        // 从服务器的movingPieces列表中移除
                        var rtController = GameManager.Instance.GetCurrentGameMode() as RealTimeModeController;
                        rtController?.Server_OnMoveAnimationComplete(pc);

                        Debug.Log($"[Server-State] {pc.name} 移动完成，服务器状态已重置于 {pc.RTState.MoveEndPos}。");
                    }
                    else if (pc != null)
                    {
                        // 从服务器的movingPieces列表中移除
                        var rtController = GameManager.Instance.GetCurrentGameMode() as RealTimeModeController;
                        rtController?.Server_OnMoveAnimationComplete(pc);
                        Debug.Log($"[Server-State] 已死亡的棋子 {pc.name} 动画结束，不执行落子逻辑。");
                    }
                }
            );
        }
    }

}