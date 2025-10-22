// File: _Scripts/Controllers/AI/IAIStrategy.cs

using System.Collections.Generic;

/// <summary>
/// AI���߲��Խӿڡ�
/// ��ͬ��AI�ѶȽ�ͨ��ʵ�ִ˽ӿ����ṩ��ͬ�ľ����߼���
/// </summary>
public interface IAIStrategy
{
    /// <summary>
    /// AI���ߵĺ��ķ�����
    /// </summary>
    /// <param name="assignedColor">AI�����Ƶ���ɫ</param>
    /// <param name="logicalBoard">��ǰ���߼�����״̬�������ƶ��е����ӣ�</param>
    /// <param name="myPieces">AI��ӵ�е����������б�</param>
    /// <returns>һ����������������ƶ�����</returns>
    AIController.MovePlan FindBestMove(PlayerColor assignedColor, BoardState logicalBoard, List<PieceComponent> myPieces);
}