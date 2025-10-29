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
}