// File: _Scripts/MovementStrategies/IPieceMovementStrategy.cs

public interface IPieceMovementStrategy
{
    /// <summary>
    /// ���ƶ���ʼʱ��������״̬��
    /// </summary>
    void UpdateStateOnMoveStart(PieceStateController state);

    /// <summary>
    /// ���ƶ������и��ݽ��ȸ�������״̬��
    /// </summary>
    void UpdateStateOnMoveUpdate(PieceStateController state, float moveProgress);


    /// <summary>
    /// �������������ƶ����ȣ����㵱ǰ֡��Y��ƫ��������Ծ�߶ȣ���
    /// </summary>
    /// <param name="moveProgress">�ƶ����� (0.0 to 1.0)</param>
    /// <param "baseJumpHeight">BoardRenderer�ж���Ļ�����Ծ�߶�</param>
    /// <returns>Y��ĸ߶�ƫ��</returns>
    float GetJumpHeight(float moveProgress, float baseJumpHeight);

}