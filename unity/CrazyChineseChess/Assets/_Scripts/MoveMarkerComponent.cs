// File: _Scripts/MoveMarkerComponent.cs
using UnityEngine;

/// <summary>
/// 这是一个挂在“移动标记”预制件上的数据组件。
/// 它的唯一作用就是存储该标记所代表的棋盘格子坐标。
/// 这样，当PlayerInput的射线检测到它时，就能立刻知道玩家想移动到哪里。
/// </summary>
public class MoveMarkerComponent : MonoBehaviour
{
    // 公开的变量，用于存储棋盘坐标
    public Vector2Int BoardPosition;
}