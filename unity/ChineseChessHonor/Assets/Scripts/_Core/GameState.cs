// 文件路径: Assets/Scripts/_Core/GameState/GameState.cs
// (全量更新)

using System.Collections.Generic;
using UnityEngine;

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

    // 新增: 棋盘的逻辑尺寸 (例如: 9x10, 18x10)
    public Vector2Int boardSize;

    // 玩家行动点。长度为4以支持最多4个阵营。
    public float[] actionPoints;

    // 技能冷却计时器。Key是技能唯一ID, Value是剩余tick数或秒数。
    public Dictionary<int, float> skillCooldowns;

    // 游戏当前阶段
    public GamePhase phase;

    // 游戏结束时的胜利方
    public PlayerTeam winner;

    // 仅在回合制模式下有意义
    public PlayerTeam currentTurn;

    // 新增: 记录当前是谁发起的求和请求
    public PlayerTeam drawRequestFrom = PlayerTeam.None;

    // 构造函数，用于创建一个干净的初始状态
    public GameState()
    {
        tick = 0;
        pieces = new Dictionary<int, PieceData>();
        boardSize = new Vector2Int(9, 10); // 默认为标准棋盘
        actionPoints = new float[4]; // 扩展以支持4个队伍
        skillCooldowns = new Dictionary<int, float>();
        phase = GamePhase.Setup;
        winner = PlayerTeam.None;
        currentTurn = PlayerTeam.None;
        drawRequestFrom = PlayerTeam.None;
    }
}