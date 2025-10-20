// File: _Scripts/MovementStrategies/CannonStrategy.cs

using UnityEngine;

public class CannonStrategy : IPieceMovementStrategy
{
    // ����������̬���������ڴ��ⲿ��֪��һ���ƶ��Ƿ�Ϊ����
    public static bool isNextMoveCapture = false;

    public void UpdateStateOnMoveStart(PieceStateController state)
    {
        state.SetStates(isVulnerable: true, isAttacking: false);
    }

    public void UpdateStateOnMoveUpdate(PieceStateController state, float moveProgress)
    {
        // ֻ�����ǳ����ƶ���������ĩ��ʱ���Ž��빥��״̬
        if (isNextMoveCapture && moveProgress >= 0.9f)
        {
            state.SetStates(isVulnerable: true, isAttacking: true);
        }
        else
        {
            state.SetStates(isVulnerable: true, isAttacking: false);
        }
    }


    // ����������ֻ���ڹ���ʱ����Ծ
    public float GetJumpHeight(float moveProgress, float baseJumpHeight)
    {
        if (isNextMoveCapture)
        {
            return Mathf.Sin(moveProgress * Mathf.PI) * baseJumpHeight;
        }
        else
        {
            return 0f; // ƽ��ʱ����Ծ
        }
    }


}