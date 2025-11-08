// 文件路径: Assets/Scripts/_Core/Networking/NetworkEvents.cs (最终修正版 2)

using Mirror;
using UnityEngine;

public class NetworkEvents : NetworkBehaviour
{
    public static NetworkEvents Instance { get; private set; }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (Instance == null) Instance = this; else Destroy(gameObject);
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (Instance == null) Instance = this; else if (Instance != this) Destroy(gameObject);
    }

    // --- 新增的 TargetRpc ---
    [TargetRpc]
    public void TargetRpcSyncInitialState(NetworkConnection target, PieceData[] initialPieces)
    {
        Debug.Log("接收到来自服务器的初始状态同步...");
        GameLoopController.Instance.InitializeAsClient(initialPieces);
    }

    // --- 原有的 ClientRpc ---
    [ClientRpc]
    public void RpcOnPieceCreated(PieceData[] newPieces)
    {
        if (isClientOnly)
        {
            // 未来用于召唤单位等
        }
    }

    [ClientRpc]
    public void RpcOnPieceUpdated(PieceData updatedPiece)
    {
        if (isClientOnly)
        {
            GameLoopController.Instance.HandlePieceUpdated_FromNet(updatedPiece);
        }
    }

    [ClientRpc]
    public void RpcOnPieceRemoved(int pieceId)
    {
        if (isClientOnly)
        {
            GameLoopController.Instance.HandlePieceRemoved_FromNet(pieceId);
        }
    }

    [ClientRpc]
    public void RpcOnActionPointsUpdated(PlayerTeam team, float newAmount)
    {
        if (isClientOnly)
        {
            GameLoopController.Instance.HandleActionPointsUpdated_FromNet(team, newAmount);
        }
    }
}