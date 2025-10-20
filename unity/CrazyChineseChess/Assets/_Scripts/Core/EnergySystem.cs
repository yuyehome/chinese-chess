// File: _Scripts/Core/EnergySystem.cs

using UnityEngine;

/// <summary>
/// 负责管理双方玩家行动点（能量）的系统。
/// </summary>
public class EnergySystem
{
    // --- 配置参数 ---
    private const float MAX_ENERGY = 4.0f;        // 能量上限
    private const float ENERGY_RECOVERY_RATE = 0.3f; // 能量恢复速度
    private const int MOVE_COST = 1;                 // 移动一次消耗1点能量

    // --- 内部状态 ---
    private float redPlayerEnergy;
    private float blackPlayerEnergy;

    /// <summary>
    /// 构造函数，初始化双方能量。
    /// </summary>
    public EnergySystem()
    {
        // 游戏开始时，双方能量为0
        redPlayerEnergy = 0.0f;
        blackPlayerEnergy = 0.0f;
    }

    /// <summary>
    /// 这个方法需要被一个MonoBehaviour的Update()方法每帧调用。
    /// </summary>
    public void Tick()
    {
        // 恢复红方能量
        if (redPlayerEnergy < MAX_ENERGY)
        {
            redPlayerEnergy += ENERGY_RECOVERY_RATE * Time.deltaTime;
            redPlayerEnergy = Mathf.Min(redPlayerEnergy, MAX_ENERGY); // 确保不超过上限
        }

        // 恢复黑方能量
        if (blackPlayerEnergy < MAX_ENERGY)
        {
            blackPlayerEnergy += ENERGY_RECOVERY_RATE * Time.deltaTime;
            blackPlayerEnergy = Mathf.Min(blackPlayerEnergy, MAX_ENERGY); // 确保不超过上限
        }
    }

    /// <summary>
    /// 检查指定玩家是否有足够的能量执行操作。
    /// </summary>
    public bool CanSpendEnergy(PlayerColor player)
    {
        float currentEnergy = GetEnergy(player);
        return currentEnergy >= MOVE_COST;
    }

    /// <summary>
    /// 消耗指定玩家的能量。
    /// </summary>
    public void SpendEnergy(PlayerColor player)
    {
        if (player == PlayerColor.Red)
        {
            redPlayerEnergy -= MOVE_COST;
        }
        else if (player == PlayerColor.Black)
        {
            blackPlayerEnergy -= MOVE_COST;
        }
    }

    /// <summary>
    /// 获取指定玩家当前的能量值（整数部分）。
    /// </summary>
    public int GetEnergyInt(PlayerColor player)
    {
        return Mathf.FloorToInt(GetEnergy(player));
    }

    /// <summary>
    /// 获取指定玩家当前的精确能量值。
    /// </summary>
    public float GetEnergy(PlayerColor player)
    {
        if (player == PlayerColor.Red)
        {
            return redPlayerEnergy;
        }
        if (player == PlayerColor.Black)
        {
            return blackPlayerEnergy;
        }
        return 0;
    }
}