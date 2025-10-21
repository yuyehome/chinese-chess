// File: _Scripts/Core/EnergySystem.cs

using UnityEngine;

/// <summary>
/// 负责管理双方玩家行动点（能量）的恢复、消耗和查询。
/// </summary>
public class EnergySystem
{
    // --- 配置常量 ---
    private const float MAX_ENERGY = 4.0f;
    private const float ENERGY_RECOVERY_RATE = 0.3f;
    private const int MOVE_COST = 1;

    // --- 内部状态 ---
    private float redPlayerEnergy;
    private float blackPlayerEnergy;

    public EnergySystem()
    {
        // 游戏开始时，双方拥有初始能量
        redPlayerEnergy = 2.0f;
        blackPlayerEnergy = 2.0f;
    }

    /// <summary>
    /// 由MonoBehaviour的Update()方法每帧调用，以平滑地恢复能量。
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
    /// 检查指定玩家是否有足够的能量执行一次操作。
    /// </summary>
    public bool CanSpendEnergy(PlayerColor player)
    {
        return GetEnergy(player) >= MOVE_COST;
    }

    /// <summary>
    /// 消耗指定玩家一次操作所需的能量。
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
    /// 获取指定玩家当前能量值的整数部分（用于UI显示）。
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