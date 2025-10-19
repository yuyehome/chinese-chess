// File: _Scripts/GameModes/RealTimeModeController.cs
using UnityEngine;

/// <summary>
/// ��������ʵʱģʽ�Ŀ����� (δ��ʵ��)��
/// ���������ж��㡢ͬ���������߼���
/// </summary>
public class RealTimeModeController : GameModeController
{
    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    // TODO: ʵ��ʵʱģʽ�µĵ���߼�
    // ���磺����ж����Ƿ��㹻�������ֻغϵ�

    public override void OnPieceClicked(PieceComponent piece)
    {
        Debug.LogWarning("ʵʱģʽ��OnPieceClicked�߼���δʵ�֣�");
    }

    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        Debug.LogWarning("ʵʱģʽ��OnMarkerClicked�߼���δʵ�֣�");
    }

    public override void OnBoardClicked(RaycastHit hit)
    {
        Debug.LogWarning("ʵʱģʽ��OnBoardClicked�߼���δʵ�֣�");
    }
}