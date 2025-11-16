// 文件路径: Assets/Scripts/_Core/Networking/NetworkPlayerRoom.cs
using Mirror;
using Steamworks;

public class NetworkPlayerRoom : NetworkBehaviour
{
    [SyncVar]
    public CSteamID SteamId;

    // 我们不再需要客户端主动报告准备状态，
    // 因为MirrorService会在OnServerAddPlayer时直接将我们添加到列表中。
    // OnStartClient 和 CmdSetReady 都可以移除了。
}