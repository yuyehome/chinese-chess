// File: _Scripts/MovementStrategies/PhysicalMoverStrategy.cs

// ����������˧��ʿ�����ƶ���ȫ�̱��ֹ����ԺͿɱ�����״̬�Ĳ���
public class PhysicalMoverStrategy : IPieceMovementStrategy
{
    public void UpdateStateOnMoveStart(PieceStateController state)
    {
        state.SetStates(isVulnerable: true, isAttacking: true);
    }

    public void UpdateStateOnMoveUpdate(PieceStateController state, float moveProgress)
    {
        // �������ƶ������У�״̬���ֲ���
    }

    // �����������浥λ����Ծ
    public float GetJumpHeight(float moveProgress, float baseJumpHeight)
    {
        return 0f;
    }
}