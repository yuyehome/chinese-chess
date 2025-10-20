// File: _Scripts/MovementStrategies/PhysicalMoverStrategy.cs

// 代表车、兵、帅、士等在移动中全程保持攻击性和可被攻击状态的策略
public class PhysicalMoverStrategy : IPieceMovementStrategy
{
    public void UpdateStateOnMoveStart(PieceStateController state)
    {
        state.SetStates(isVulnerable: true, isAttacking: true);
    }

    public void UpdateStateOnMoveUpdate(PieceStateController state, float moveProgress)
    {
        // 在整个移动过程中，状态保持不变
    }

    // 【新增】地面单位不跳跃
    public float GetJumpHeight(float moveProgress, float baseJumpHeight)
    {
        return 0f;
    }
}