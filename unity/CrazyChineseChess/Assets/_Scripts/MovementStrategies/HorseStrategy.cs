// File: _Scripts/MovementStrategies/HorseStrategy.cs

using UnityEngine; 

public class HorseStrategy : IPieceMovementStrategy
{
    public void UpdateStateOnMoveStart(PieceStateController state)
    {
        // �ƶ���ʼʱ���Ƿǹ������ɱ�������
        state.SetStates(isVulnerable: true, isAttacking: false);
    }

    public void UpdateStateOnMoveUpdate(PieceStateController state, float moveProgress)
    {
        // 0.2 ~ 0.8 �׶Σ������������״̬
        if (moveProgress >= 0.2f && moveProgress <= 0.8f)
        {
            state.SetStates(isVulnerable: false, isAttacking: false);
        }
        // 0.6 ~ 1.0 �׶Σ���������ѹ�Ĺ���״̬
        else if (moveProgress > 0.6f)
        {
            // ע�⣺����Ḳ�������״̬���� 0.6-0.8 ���䣬vulnerable����true
            state.SetStates(isVulnerable: true, isAttacking: true);
        }
        // 0.0 ~ 0.2 �׶Σ����ֳ�ʼ״̬ (isVulnerable: true, isAttacking: false)
        else
        {
            state.SetStates(isVulnerable: true, isAttacking: false);
        }
    }

    // �������������Ծ������ʹ�� Sin ����ģ��������
    public float GetJumpHeight(float moveProgress, float baseJumpHeight)
    {
        // Mathf.Sin(progress * Mathf.PI) ���� 0->1 �Ĺ����в���һ�� 0 -> 1 -> 0 ��ƽ������
        return Mathf.Sin(moveProgress * Mathf.PI) * baseJumpHeight;
    }
}