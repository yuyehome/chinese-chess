// File: _Scripts/Core/EnergySystem.cs

using UnityEngine;

/// <summary>
/// �������˫������ж��㣨�������Ļָ������ĺͲ�ѯ��
/// </summary>
public class EnergySystem
{
    // --- ���ò��� (���ⲿע��) ---
    private readonly float maxEnergy;
    private readonly float energyRecoveryRate;
    private readonly int moveCost;

    // --- �ڲ�״̬ ---
    private float redPlayerEnergy;
    private float blackPlayerEnergy;

    /// <summary>
    /// ���캯�������ڳ�ʼ������ϵͳ��ע�����ò�����
    /// </summary>
    /// <param name="maxEnergy">�����������</param>
    /// <param name="recoveryRate">ÿ��ָ�����</param>
    /// <param name="moveCost">ÿ���ƶ�����</param>
    /// <param name="startEnergy">��ʼ����</param>
    public EnergySystem(float maxEnergy, float recoveryRate, int moveCost, float startEnergy)
    {
        this.maxEnergy = maxEnergy;
        this.energyRecoveryRate = recoveryRate;
        this.moveCost = moveCost;

        // ��Ϸ��ʼʱ��˫��ӵ�г�ʼ����
        this.redPlayerEnergy = startEnergy;
        this.blackPlayerEnergy = startEnergy;
    }

    /// <summary>
    /// ��MonoBehaviour��Update()����ÿ֡���ã���ƽ���ػָ�������
    /// </summary>
    public void Tick()
    {
        // �ָ��췽����
        if (redPlayerEnergy < maxEnergy)
        {
            redPlayerEnergy += energyRecoveryRate * Time.deltaTime;
            redPlayerEnergy = Mathf.Min(redPlayerEnergy, maxEnergy); // ȷ������������
        }

        // �ָ��ڷ�����
        if (blackPlayerEnergy < maxEnergy)
        {
            blackPlayerEnergy += energyRecoveryRate * Time.deltaTime;
            blackPlayerEnergy = Mathf.Min(blackPlayerEnergy, maxEnergy); // ȷ������������
        }
    }

    /// <summary>
    /// ���ָ������Ƿ����㹻������ִ��һ�β�����
    /// </summary>
    public bool CanSpendEnergy(PlayerColor player)
    {
        return GetEnergy(player) >= moveCost;
    }

    /// <summary>
    /// ����ָ�����һ�β��������������
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
    /// ��ȡָ����ҵ�ǰ����ֵ���������֣�����UI��ʾ����
    /// </summary>
    public int GetEnergyInt(PlayerColor player)
    {
        return Mathf.FloorToInt(GetEnergy(player));
    }

    /// <summary>
    /// ��ȡָ����ҵ�ǰ�ľ�ȷ����ֵ��
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