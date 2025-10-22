// File: _Scripts/Controllers/AI/IAIStrategy.cs

using System.Collections.Generic;

/// <summary>
/// AI���߲��Խӿڡ�
/// ��ͬ��AI�ѶȽ�ͨ��ʵ�ִ˽ӿ����ṩ��ͬ�ľ����߼���
/// </summary>
public interface IAIStrategy
{

    AIController.MovePlan FindBestMove(GameManager gameManager, PlayerColor assignedColor);

}