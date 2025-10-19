// File: _Scripts/GameModes/RealTimeModeController.cs
using UnityEngine;

/// <summary>
/// 【新增】实时模式的控制器 (未来实现)。
/// 将负责处理行动点、同步操作等逻辑。
/// </summary>
public class RealTimeModeController : GameModeController
{
    public RealTimeModeController(GameManager manager, BoardState state, BoardRenderer renderer)
        : base(manager, state, renderer) { }

    // TODO: 实现实时模式下的点击逻辑
    // 例如：检查行动点是否足够，不区分回合等

    public override void OnPieceClicked(PieceComponent piece)
    {
        Debug.LogWarning("实时模式的OnPieceClicked逻辑尚未实现！");
    }

    public override void OnMarkerClicked(MoveMarkerComponent marker)
    {
        Debug.LogWarning("实时模式的OnMarkerClicked逻辑尚未实现！");
    }

    public override void OnBoardClicked(RaycastHit hit)
    {
        Debug.LogWarning("实时模式的OnBoardClicked逻辑尚未实现！");
    }
}