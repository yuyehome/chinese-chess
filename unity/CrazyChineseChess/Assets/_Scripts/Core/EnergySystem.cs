// File: _Scripts/Core/EnergySystem.cs

using UnityEngine;

/// <summary>
/// �������˫������ж��㣨�������Ļָ������ĺͲ�ѯ��
/// </summary>
public class EnergySystem
{
    // --- ���ó��� ---
    private const float MAX_ENERGY = 4.0f;
    private const float ENERGY_RECOVERY_RATE = 0.3f;
    private const int MOVE_COST = 1;

    // --- �ڲ�״̬ ---
    private float redPlayerEnergy;
    private float blackPlayerEnergy;

    public EnergySystem()
    {
        // ��Ϸ��ʼʱ��˫��ӵ�г�ʼ����
        redPlayerEnergy = 2.0f;
        blackPlayerEnergy = 2.0f;
    }

    /// <summary>
    /// ��MonoBehaviour��Update()����ÿ֡���ã���ƽ���ػָ�������
    /// </summary>
    public void Tick()
    {
        // �ָ��췽����
        if (redPlayerEnergy < MAX_ENERGY)
        {
            redPlayerEnergy += ENERGY_RECOVERY_RATE * Time.deltaTime;
            redPlayerEnergy = Mathf.Min(redPlayerEnergy, MAX_ENERGY); // ȷ������������
        }

        // �ָ��ڷ�����
        if (blackPlayerEnergy < MAX_ENERGY)
        {
            blackPlayerEnergy += ENERGY_RECOVERY_RATE * Time.deltaTime;
            blackPlayerEnergy = Mathf.Min(blackPlayerEnergy, MAX_ENERGY); // ȷ������������
        }
    }

    /// <summary>
    /// ���ָ������Ƿ����㹻������ִ��һ�β�����
    /// </summary>
    public bool CanSpendEnergy(PlayerColor player)
    {
        return GetEnergy(player) >= MOVE_COST;
    }

    /// <summary>
    /// ����ָ�����һ�β��������������
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