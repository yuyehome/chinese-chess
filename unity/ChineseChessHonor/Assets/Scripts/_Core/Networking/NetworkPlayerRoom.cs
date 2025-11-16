// 文件路径: Assets/Scripts/_Core/Networking/NetworkPlayerRoom.cs
using Mirror;
using Steamworks;

public class NetworkPlayerRoom : NetworkBehaviour
{
    [SyncVar]
    public CSteamID SteamId;

    public override void OnStartClient()
    {
        base.OnStartClient();
        if (isLocalPlayer)
        {
            // 当这个对象在本地客户端上生成时，告诉服务器“我已准备好”
            CmdSetReady();
        }
    }

    [Command]
    private void CmdSetReady()
    {
        // 这个指令在服务器上执行
        // 服务器端的MirrorService会监听这个事件
        (NetworkManager.singleton as MirrorService)?.PlayerIsReady(this);
    }
}