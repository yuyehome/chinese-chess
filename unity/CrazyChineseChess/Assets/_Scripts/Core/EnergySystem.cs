// File: _Scripts/Core/EnergySystem.cs

using UnityEngine;

/// <summary>
/// 负责管理双方玩家行动点（能量）的恢复、消耗和查询。
/// </summary>
public class EnergySystem
{
    // --- 配置参数 (由外部注入) ---
    private readonly float maxEnergy;
    private readonly float energyRecoveryRate;
    private readonly int moveCost;

    // --- 内部状态 ---
    private float redPlayerEnergy;
    private float blackPlayerEnergy;

    /// <summary>
    /// 构造函数，用于初始化能量系统并注入配置参数。
    /// </summary>
    /// <param name="maxEnergy">最大能量上限</param>
    /// <param name="recoveryRate">每秒恢复速率</param>
    /// <param name="moveCost">每次移动消耗</param>
    /// <param name="startEnergy">初始能量</param>
    public EnergySystem(float maxEnergy, float recoveryRate, int moveCost, float startEnergy)
    {
        this.maxEnergy = maxEnergy;
        this.energyRecoveryRate = recoveryRate;
        this.moveCost = moveCost;

        // 游戏开始时，双方拥有初始能量
        this.redPlayerEnergy = startEnergy;
        this.blackPlayerEnergy = startEnergy;
    }

    /// <summary>
    /// 由MonoBehaviour的Update()方法每帧调用，以平滑地恢复能量。
    /// </summary>
    public void Tick()
    {
        // 恢复红方能量
        if (redPlayerEnergy < maxEnergy)
        {
            redPlayerEnergy += energyRecoveryRate * Time.deltaTime;
            redPlayerEnergy = Mathf.Min(redPlayerEnergy, maxEnergy); // 确保不超过上限
        }

        // 恢复黑方能量
        if (blackPlayerEnergy < maxEnergy)
        {
            blackPlayerEnergy += energyRecoveryRate * Time.deltaTime;
            blackPlayerEnergy = Mathf.Min(blackPlayerEnergy, maxEnergy); // 确保不超过上限
        }
    }

    /// <summary>
    /// 检查指定玩家是否有足够的能量执行一次操作。
    /// </summary>
    public bool CanSpendEnergy(PlayerColor player)
    {
        return GetEnergy(player) >= moveCost;
    }

    /// <summary>
    /// 消耗指定玩家一次操作所需的能量。
    /// </summary>
    public void SpendEnergy(PlayerColor player)
    {
        if (player == PlayerColor.Red)
        {
            redPlayerEnergy -= moveCost;
        }
        else if (player == PlayerColor.Black)
        {
            blackPlayerEnergy -= moveCost;
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