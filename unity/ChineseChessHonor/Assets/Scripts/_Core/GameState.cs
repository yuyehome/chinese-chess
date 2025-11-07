// 文件路径: Assets/Scripts/_Core/GameState/GameState.cs

using System.Collections.Generic;

// 游戏当前阶段
public enum GamePhase
{
    Setup,     // 准备阶段
    Playing,   // 游戏中
    Paused,    // 暂停
    GameOver   // 游戏结束
}

public class GameState
{
    // 游戏逻辑帧或回合数。每次状态更新时自增。
    public int tick;

    // 核心数据: 存储棋盘上所有棋子的当前数据。Key是棋子的uniqueId。
    public Dictionary<int, PieceData> pieces;

    // 玩家行动点 [0] = Red, [1] = Black
    public float[] actionPoints;

    // 技能冷却计时器。Key是技能唯一ID, Value是剩余tick数或秒数。
    // 为了简化，我们先定义结构，具体生成Key的逻辑在技能系统实现。
    public Dictionary<int, float> skillCooldowns;

    // 游戏当前阶段
    public GamePhase phase;

    // 游戏结束时的胜利方
    public PlayerTeam winner;

    // 仅在回合制模式下有意义
    public PlayerTeam currentTurn;

    // 构造函数，用于创建一个干净的初始状态
    public GameState()
    {
        tick = 0;
        pieces = new Dictionary<int, PieceData>();
        actionPoints = new float[2];
        skillCooldowns = new Dictionary<int, float>();
        phase = GamePhase.Setup;
        // winner和currentTurn在游戏开始时根据需要设置
    }

    // TODO: 后续网络阶段实现 FishNet 的 INetworkSerializable 接口
}
