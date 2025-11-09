// 文件路径: Assets/Scripts/_Core/View/PieceView.cs

using UnityEngine;

public class PieceView : MonoBehaviour
{
    public int pieceId;
    public PlayerTeam team;

    private Vector3 _targetWorldPosition;
    private float _moveSpeed = 10f; // 视觉移动速度，可以调快一点让动画更跟手

    public void Initialize(PieceData pieceData)
    {
        this.pieceId = pieceData.uniqueId;
        this.team = pieceData.team;

        // 根据逻辑坐标设置初始世界坐标
        _targetWorldPosition = GridToWorld(pieceData.position);
        transform.position = _targetWorldPosition;
    }

    // 更新目标位置，由BoardView在状态更新时调用
    public void UpdateTargetPosition(Vector2Int gridPosition)
    {
        _targetWorldPosition = GridToWorld(gridPosition);

        // 在移动开始时播放音效
        AudioManager.Instance.PlaySFX("sfx_piece_click");
    }

    // 在Update中平滑移动到目标位置
    private void Update()
    {
        if (Vector3.Distance(transform.position, _targetWorldPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, _targetWorldPosition, Time.deltaTime * _moveSpeed);
        }
        else
        {
            transform.position = _targetWorldPosition;
        }
    }

    // 辅助函数：将棋盘格点坐标转换为Unity世界坐标
    public static Vector3 GridToWorld(Vector2Int gridPos)
    {
        // 假设棋盘中心在(4, 4.5)，每个格子大小为1x1
        return new Vector3(gridPos.x - 4f, 0, gridPos.y - 4.5f);
    }

    // 辅助函数：将Unity世界坐标转换为棋盘格点坐标
    public static Vector2Int WorldToGrid(Vector3 worldPos)
    {
        int x = Mathf.RoundToInt(worldPos.x + 4f);
        int y = Mathf.RoundToInt(worldPos.z + 4.5f);
        return new Vector2Int(x, y);
    }
}