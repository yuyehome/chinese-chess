// File: _Scripts/Controllers/AI/IAIStrategy.cs

using System.Collections.Generic;
using UnityEngine;

public interface IAIStrategy
{
    // --- 方法签名修改：不再传递GameManager ---
    /// <summary>
    /// AI决策的核心方法。
    /// </summary>
    /// <param name="assignedColor">AI所控制的颜色</param>
    /// <param name="logicalBoard">当前的逻辑棋盘状态</param>
    /// <param name="myPieces">AI所拥有的所有棋子(模拟信息)</param>
    /// <param name="opponentPieces">对手拥有的所有棋子(模拟信息)</param>
    /// <returns>一个经过评估的最佳移动方案</returns>
    AIController.MovePlan FindBestMove(PlayerColor assignedColor, BoardState logicalBoard,
                                        List<GameManager.SimulatedPiece> myPieces,
                                        List<GameManager.SimulatedPiece> opponentPieces);

    Vector2 DecisionTimeRange { get; }
}