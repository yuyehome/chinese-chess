// 文件路径: Assets/Scripts/_Core/Networking/NetworkEvents.cs

using Mirror;
using UnityEngine;

public class NetworkEvents : NetworkBehaviour
{
    public static NetworkEvents Instance { get; private set; }

    private void Awake()
    {
        Debug.Log($"[NetworkEvents] Awake: 一个NetworkEvents实例被创建。 IsServer: {isServer}, IsClient: {isClient}");
    }

    public override void OnStartServer()
    {
        base.OnStartServer();
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("[NetworkEvents] OnStartServer: Instance 已在服务器上设置。");
        }
        else
        {
            Debug.LogWarning("[NetworkEvents] OnStartServer: 发现重复的NetworkEvents实例，即将销毁。");
            Destroy(gameObject);
        }
    }

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (Instance == null)
        {
            Instance = this;
            Debug.Log($"[NetworkEvents] OnStartClient: Instance 在客户端上成功设置! (netId: {netId})");
        }
        else if (Instance != this)
        {
            Debug.LogWarning("[NetworkEvents] OnStartClient: 发现重复的NetworkEvents实例，即将销毁。");
            Destroy(gameObject);
        }
    }

    [TargetRpc]
    public void TargetRpcSyncInitialState(NetworkConnection target, PieceData[] initialPieces)
    {
        Debug.Log($"[NetworkEvents] TargetRpcSyncInitialState: 客户端收到来自服务器的初始状态同步! 棋子数量: {initialPieces.Length}");
        if (GameLoopController.Instance != null)
        {
            GameLoopController.Instance.InitializeAsClient(initialPieces);
        }
        else
        {
            Debug.LogError("[NetworkEvents] TargetRpcSyncInitialState: GameLoopController.Instance 为 null! 无法初始化客户端状态。");
        }
    }

    [ClientRpc]
    public void RpcOnPieceUpdated(PieceData updatedPiece)
    {
        Debug.Log($"[NetworkEvents] RpcOnPieceUpdated: 收到棋子更新, ID: {updatedPiece.uniqueId}");
        if (isClientOnly)
        {
            GameLoopController.Instance.HandlePieceUpdated_FromNet(updatedPiece);
        }
    }

    [ClientRpc]
    public void RpcOnPieceRemoved(int pieceId)
    {
        Debug.Log($"[NetworkEvents] RpcOnPieceRemoved: 收到棋子移除, ID: {pieceId}");
        if (isClientOnly)
        {
            GameLoopController.Instance.HandlePieceRemoved_FromNet(pieceId);
        }
    }

    [ClientRpc]
    public void RpcOnActionPointsUpdated(PlayerTeam team, float newAmount)
    {
        Debug.Log($"[NetworkEvents] RpcOnActionPointsUpdated: 收到行动点更新, Team: {team}, Amount: {newAmount}");
        if (isClientOnly)
        {
            GameLoopController.Instance.HandleActionPointsUpdated_FromNet(team, newAmount);
        }
    }

    //  由Host调用，通知所有客户端开始备战阶段
    [ClientRpc]
    public void RpcStartPreBattlePhase()
    {
        Debug.Log("[NetworkEvents] 收到 RpcStartPreBattlePhase 指令。");
        // 假设RoomPanel是一个单例或易于访问的
        var roomPanel = FindObjectOfType<RoomPanel>(); // 临时写法，后续可优化
        if (roomPanel != null && roomPanel.IsVisible)
        {
            roomPanel.StartPreBattlePhase();
        }
    }

}