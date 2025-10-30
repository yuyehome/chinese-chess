using UnityEngine;

/// <summary>
/// ��������ж��㣨�������Ļָ������ĺͲ�ѯ�߼���
/// ����һ����״̬�Ĺ����࣬���м��㶼���ڴ���ĵ�ǰ����ֵ��
/// </summary>
public class EnergySystem
{
    private readonly float maxEnergy;
    private readonly float energyRecoveryRate;
    private readonly int moveCost;

    /// <summary>
    /// ���캯����ע�����ò�����
    /// </summary>
    public EnergySystem(float maxEnergy, float recoveryRate, int moveCost)
    {
        this.maxEnergy = maxEnergy;
        this.energyRecoveryRate = recoveryRate;
        this.moveCost = moveCost;
    }

    /// <summary>
    /// ���㲢����һ֮֡���������ֵ��
    /// </summary>
    /// <param name="currentEnergy">��ǰ������ֵ</param>
    /// <returns>���º������ֵ</returns>
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
    /// ����Ƿ����㹻������ִ��һ�β�����
    /// </summary>
    /// <param name="currentEnergy">��ǰ������ֵ</param>
    public bool CanSpendEnergy(float currentEnergy)
    {
        return currentEnergy >= moveCost;
    }

    /// <summary>
    /// ���㲢��������һ�β������������ֵ��
    /// </summary>
    /// <param name="currentEnergy">��ǰ������ֵ</param>
    /// <returns>���ĺ������ֵ</returns>
    public float SpendEnergy(float currentEnergy)
    {
        return currentEnergy - moveCost;
    }

    /// <summary>
    /// ��ȡ����ֵ���������֣�����UI��ʾ����
    /// </summary>
    public int GetEnergyInt(float currentEnergy)
    {
        return Mathf.FloorToInt(currentEnergy);
    }
}