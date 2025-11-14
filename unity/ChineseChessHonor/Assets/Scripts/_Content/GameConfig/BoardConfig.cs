// 文件路径: Assets/Scripts/_Content/GameConfig/BoardConfig.cs

using UnityEngine;

[CreateAssetMenu(fileName = "BoardConfig", menuName = "ChessHonor/Configs/Board Config")]
public class BoardConfig : ScriptableObject
{
    [Header("棋盘逻辑尺寸")]
    public Vector2Int boardSize = new Vector2Int(9, 10);

    [Header("棋盘物理尺寸 (单位: 米)")]
    [Tooltip("每个格子之间的中心点距离")]
    public float gridSpacing = 0.05f; // 50mm

    [Tooltip("棋盘的起始点在世界坐标中的偏移量。通常我们希望(0,0)逻辑点对应世界的(0,0,0)")]
    public Vector3 originOffset = Vector3.zero;

    /// <summary>
    /// 将逻辑格子坐标转换为世界坐标。
    /// </summary>
    public Vector3 GridToWorld(Vector2Int gridPos)
    {
        return new Vector3(gridPos.x * gridSpacing, 0, gridPos.y * gridSpacing) + originOffset;
    }

    /// <summary>
    /// 将世界坐标转换为最近的逻辑格子坐标。
    /// </summary>
    public Vector2Int WorldToGrid(Vector3 worldPos)
    {
        Vector3 relativePos = worldPos - originOffset;
        int x = Mathf.RoundToInt(relativePos.x / gridSpacing);
        int y = Mathf.RoundToInt(relativePos.z / gridSpacing);
        return new Vector2Int(x, y);
    }
}