// File: _Scripts/PieceComponent.cs

using UnityEngine;
using FishNet.Object;
using FishNet.Object.Synchronizing;

/// <summary>
/// 挂载在棋子Prefab上的“身份证”组件。
/// 它作为桥梁，连接了棋子的视觉表现(GameObject)和其背后的多种逻辑数据。
/// [网络改造]: 继承自NetworkBehaviour，使其成为一个网络对象。
/// </summary>
public class PieceComponent : NetworkBehaviour // 继承 NetworkBehaviour 而不是 MonoBehaviour
{
    /// <summary>
    /// [SyncVar] 棋子的纯数据定义（类型、颜色）。
    /// SyncVar确保当这个变量在服务器上改变时，会自动同步到所有客户端。
    /// </summary>
    [SyncVar]
    public Piece PieceData;

    /// <summary>
    /// [SyncVar] 棋子在棋盘逻辑坐标系中的位置。
    /// </summary>
    [SyncVar]
    public Vector2Int BoardPosition;

    /// <summary>
    /// 仅在实时模式下使用的棋子动态状态对象。
    /// 这个状态是运行时的，不需要网络同步，由各自的控制器进行管理。
    /// </summary>
    public RealTimePieceState RTState { get; set; }


    /// <summary>
    /// FishNet回调: 当该网络对象在客户端上被生成并激活时调用。
    /// 这是客户端棋子“出生”的地方。
    /// </summary>
    public override void OnStartClient()
    {
        base.OnStartClient();

        // OnStartClient 只会在网络模式下被调用，因此不会影响单机模式
        // 在客户端，棋子需要主动向 BoardRenderer 注册，以便能被点击和管理
        BoardRenderer renderer = FindObjectOfType<BoardRenderer>();
        if (renderer != null)
        {
            // 使用从服务器同步过来的 BoardPosition 进行注册
            renderer.RegisterNetworkedPiece(this.gameObject, this.BoardPosition);
            Debug.Log($"[Client] 棋子 {name} 在位置 {BoardPosition} 成功向BoardRenderer注册。");
        }
        else
        {
            Debug.LogError($"[PieceComponent] 客户端未能找到BoardRenderer实例来注册棋子 {name}!");
        }
    }
}