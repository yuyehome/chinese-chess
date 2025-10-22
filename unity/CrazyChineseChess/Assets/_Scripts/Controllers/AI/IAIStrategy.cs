// File: _Scripts/Controllers/AI/IAIStrategy.cs

using System.Collections.Generic;
using UnityEngine;

public interface IAIStrategy
{
    // --- ����ǩ���޸ģ����ٴ���GameManager ---
    /// <summary>
    /// AI���ߵĺ��ķ�����
    /// </summary>
    /// <param name="assignedColor">AI�����Ƶ���ɫ</param>
    /// <param name="logicalBoard">��ǰ���߼�����״̬</param>
    /// <param name="myPieces">AI��ӵ�е���������(ģ����Ϣ)</param>
    /// <param name="opponentPieces">����ӵ�е���������(ģ����Ϣ)</param>
    /// <returns>һ����������������ƶ�����</returns>
    AIController.MovePlan FindBestMove(PlayerColor assignedColor, BoardState logicalBoard,
                                        List<GameManager.SimulatedPiece> myPieces,
                                        List<GameManager.SimulatedPiece> opponentPieces);

    Vector2 DecisionTimeRange { get; }
}