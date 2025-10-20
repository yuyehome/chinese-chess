// File: _Scripts/MovementStrategies/HorseStrategy.cs

using UnityEngine; 

public class HorseStrategy : IPieceMovementStrategy
{
    public void UpdateStateOnMoveStart(PieceStateController state)
    {
        // 移动开始时，是非攻击、可被攻击的
        state.SetStates(isVulnerable: true, isAttacking: false);
    }

    public void UpdateStateOnMoveUpdate(PieceStateController state, float moveProgress)
    {
        // 0.2 ~ 0.8 阶段，进入空中闪避状态
        if (moveProgress >= 0.2f && moveProgress <= 0.8f)
        {
            state.SetStates(isVulnerable: false, isAttacking: false);
        }
        // 0.6 ~ 1.0 阶段，进入向下压的攻击状态
        else if (moveProgress > 0.6f)
        {
            // 注意：这里会覆盖上面的状态，在 0.6-0.8 区间，vulnerable会变回true
            state.SetStates(isVulnerable: true, isAttacking: true);
        }
        // 0.0 ~ 0.2 阶段，保持初始状态 (isVulnerable: true, isAttacking: false)
        else
        {
            state.SetStates(isVulnerable: true, isAttacking: false);
        }
    }

    // 【新增】马的跳跃动画，使用 Sin 函数模拟抛物线
    public float GetJumpHeight(float moveProgress, float baseJumpHeight)
    {
        // Mathf.Sin(progress * Mathf.PI) 会在 0->1 的过程中产生一个 0 -> 1 -> 0 的平滑曲线
        return Mathf.Sin(moveProgress * Mathf.PI) * baseJumpHeight;
    }
}