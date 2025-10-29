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
        // OnStartClient 可能在 BoardRenderer.Awake 之前被调用
        // 所以我们不能在这里直接调用 SetupVisuals

        // 尝试立即设置，如果 BoardRenderer 已经就绪，就直接完成
        TrySetupVisuals();
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        // 服务器端通常不存在这个问题，但为了代码统一和健壮性，也使用同样逻辑
        TrySetupVisuals();
    }

    private void OnEnable()
    {
        // 订阅事件，以防我们在 BoardRenderer 就绪前就被激活
        BoardRenderer.OnInstanceReady += TrySetupVisuals;
    }

    private void OnDisable()
    {
        // 良好习惯：在对象被禁用或销毁时取消订阅，防止内存泄漏
        BoardRenderer.OnInstanceReady -= TrySetupVisuals;
    }

    /// <summary>
    /// 尝试执行视觉设置。只有在 BoardRenderer 就绪且尚未初始化时才会执行。
    /// </summary>
    private void TrySetupVisuals()
    {
        // 如果已经初始化过，或者 BoardRenderer 还未准备好，则直接返回
        if (_visualsInitialized || BoardRenderer.Instance == null)
        {
            return;
        }

        // --- 以下是原 SetupVisuals 的逻辑 ---
        if (!IsServer)
        {
            this.BoardPosition = BoardRenderer.Instance.GetBoardPosition(this.transform.localPosition);
        }

        Debug.Log($"[{(IsServer ? "Server" : "Client")}] 棋子 {gameObject.name} (Type: {Type.Value}, Color: {Color.Value}) 已生成，正在设置视觉效果。坐标: {BoardPosition}");
        BoardRenderer.Instance.SetupPieceVisuals(this);

        // 标记为已初始化，并取消订阅，因为我们只需要执行一次
        _visualsInitialized = true;
        BoardRenderer.OnInstanceReady -= TrySetupVisuals;
    }

    public override void OnStartNetwork()
    {
        // 如果你的 FishNet 版本较新，有时 OnStartNetwork 会比 OnEnable 早，
        // 我们可以在这里也进行一次尝试，确保万无一失。
        base.OnStartNetwork();
        TrySetupVisuals();
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