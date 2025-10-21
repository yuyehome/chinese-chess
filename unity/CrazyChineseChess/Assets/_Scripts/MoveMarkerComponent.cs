// File: _Scripts/MoveMarkerComponent.cs

using UnityEngine;

/// <summary>
/// 挂载在“移动标记”预制件上的数据组件。
/// 它的唯一作用就是存储该标记所代表的棋盘格子坐标。
/// 当PlayerInput的射线检测到它时，就能立刻知道玩家意图移动的目标位置。
/// </summary>
public class MoveMarkerComponent : MonoBehaviour
{
    /// <summary>
    /// 该移动标记所代表的棋盘逻辑坐标。
    /// </summary>
    public Vector2Int BoardPosition;
}