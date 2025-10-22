// File: _Scripts/Controllers/IPlayerController.cs
// (建议新建一个 _Scripts/Controllers 文件夹来存放)

/// <summary>
/// 控制器接口，定义了控制一方棋子的基本行为。
/// 无论是玩家输入、AI决策还是网络同步，都通过实现此接口来与GameManager交互。
/// </summary>
public interface IPlayerController
{
    /// <summary>
    /// 初始化控制器。
    /// </summary>
    /// <param name="color">该控制器被分配的颜色</param>
    /// <param name="gameManager">游戏总管理器的引用</param>
    void Initialize(PlayerColor color, GameManager gameManager);
}