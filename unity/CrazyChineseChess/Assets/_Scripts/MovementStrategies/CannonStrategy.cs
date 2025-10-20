// File: _Scripts/MovementStrategies/CannonStrategy.cs

using UnityEngine;

public class CannonStrategy : IPieceMovementStrategy
{
    // 【新增】静态变量，用于从外部告知下一次移动是否为吃子
    public static bool isNextMoveCapture = false;

    public void UpdateStateOnMoveStart(PieceStateController state)
    {
        state.SetStates(isVulnerable: true, isAttacking: false);
    }

    public void UpdateStateOnMoveUpdate(PieceStateController state, float moveProgress)
    {
        // 只有在是吃子移动，并且在末端时，才进入攻击状态
        if (isNextMoveCapture && moveProgress >= 0.9f)
        {
            state.SetStates(isVulnerable: true, isAttacking: true);
        }
        else
        {
            state.SetStates(isVulnerable: true, isAttacking: false);
        }
    }


    // 【新增】炮只有在攻击时才跳跃
    public float GetJumpHeight(float moveProgress, float baseJumpHeight)
    {
        if (isNextMoveCapture)
        {
            return Mathf.Sin(moveProgress * Mathf.PI) * baseJumpHeight;
        }
        else
        {
            return 0f; // 平移时不跳跃
        }
    }


}