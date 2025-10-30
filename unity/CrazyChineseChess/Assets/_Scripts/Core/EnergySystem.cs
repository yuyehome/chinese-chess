using UnityEngine;

/// <summary>
/// 负责管理行动点（能量）的恢复、消耗和查询逻辑。
/// 这是一个无状态的工具类，所有计算都基于传入的当前能量值。
/// </summary>
public class EnergySystem
{
    private readonly float maxEnergy;
    private readonly float energyRecoveryRate;
    private readonly int moveCost;

    /// <summary>
    /// 构造函数，注入配置参数。
    /// </summary>
    public EnergySystem(float maxEnergy, float recoveryRate, int moveCost)
    {
        this.maxEnergy = maxEnergy;
        this.energyRecoveryRate = recoveryRate;
        this.moveCost = moveCost;
    }

    /// <summary>
    /// 计算并返回一帧之后的新能量值。
    /// </summary>
    /// <param name="currentEnergy">当前的能量值</param>
    /// <returns>更新后的能量值</returns>
    public float Tick(float currentEnergy)
    {
        if (currentEnergy < maxEnergy)
        {
            currentEnergy += energyRecoveryRate * Time.deltaTime;
            return Mathf.Min(currentEnergy, maxEnergy);
        }
        return currentEnergy;
    }

    /// <summary>
    /// 检查是否有足够的能量执行一次操作。
    /// </summary>
    /// <param name="currentEnergy">当前的能量值</param>
    public bool CanSpendEnergy(float currentEnergy)
    {
        return currentEnergy >= moveCost;
    }

    /// <summary>
    /// 计算并返回消耗一次操作后的新能量值。
    /// </summary>
    /// <param name="currentEnergy">当前的能量值</param>
    /// <returns>消耗后的能量值</returns>
    public float SpendEnergy(float currentEnergy)
    {
        return currentEnergy - moveCost;
    }

    /// <summary>
    /// 获取能量值的整数部分（用于UI显示）。
    /// </summary>
    public int GetEnergyInt(float currentEnergy)
    {
        return Mathf.FloorToInt(currentEnergy);
    }
}