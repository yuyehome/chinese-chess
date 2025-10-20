// File: _Scripts/MovementStrategies/IPieceMovementStrategy.cs

public interface IPieceMovementStrategy
{
    /// <summary>
    /// 在移动开始时更新棋子状态。
    /// </summary>
    void UpdateStateOnMoveStart(PieceStateController state);

    /// <summary>
    /// 在移动过程中根据进度更新棋子状态。
    /// </summary>
    void UpdateStateOnMoveUpdate(PieceStateController state, float moveProgress);


    /// <summary>
    /// 【新增】根据移动进度，计算当前帧的Y轴偏移量（跳跃高度）。
    /// </summary>
    /// <param name="moveProgress">移动进度 (0.0 to 1.0)</param>
    /// <param "baseJumpHeight">BoardRenderer中定义的基础跳跃高度</param>
    /// <returns>Y轴的高度偏移</returns>
    float GetJumpHeight(float moveProgress, float baseJumpHeight);

}