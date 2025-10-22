// File: _Scripts/Controllers/AI/IAIStrategy.cs

using System.Collections.Generic;

/// <summary>
/// AI决策策略接口。
/// 不同的AI难度将通过实现此接口来提供不同的决策逻辑。
/// </summary>
public interface IAIStrategy
{

    AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor);

}