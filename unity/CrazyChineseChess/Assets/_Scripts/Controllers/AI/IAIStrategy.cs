// File: _Scripts/Controllers/AI/IAIStrategy.cs

using System.Collections.Generic;

/// <summary>
/// AI决策策略接口。
/// 不同的AI难度将通过实现此接口来提供不同的决策逻辑。
/// </summary>
public interface IAIStrategy
{
    /// <summary>
    /// AI决策的核心方法。
    /// </summary>
    /// <param name="assignedColor">AI所控制的颜色</param>
    /// <param name="logicalBoard">当前的逻辑棋盘状态（包含移动中的棋子）</param>
    /// <param name="myPieces">AI所拥有的所有棋子列表</param>
    /// <returns>一个经过评估的最佳移动方案</returns>
    AIController.MovePlan FindBestMove(PlayerColor assignedColor, BoardState logicalBoard, List<PieceComponent> myPieces);
}