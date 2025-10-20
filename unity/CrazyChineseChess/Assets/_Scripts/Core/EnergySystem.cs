// File: _Scripts/Core/EnergySystem.cs

using UnityEngine;

/// <summary>
/// �������˫������ж��㣨��������ϵͳ��
/// </summary>
public class EnergySystem
{
    // --- ���ò��� ---
    private const float MAX_ENERGY = 4.0f;        // ��������
    private const float ENERGY_RECOVERY_RATE = 0.3f; // �����ָ��ٶ�
    private const int MOVE_COST = 1;                 // �ƶ�һ������1������

    // --- �ڲ�״̬ ---
    private float redPlayerEnergy;
    private float blackPlayerEnergy;

    /// <summary>
    /// ���캯������ʼ��˫��������
    /// </summary>
    public EnergySystem()
    {
        // ��Ϸ��ʼʱ��˫������Ϊ0
        redPlayerEnergy = 0.0f;
        blackPlayerEnergy = 0.0f;
    }

    /// <summary>
    /// ���������Ҫ��һ��MonoBehaviour��Update()����ÿ֡���á�
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
    /// ���ָ������Ƿ����㹻������ִ�в�����
    /// </summary>
    public bool CanSpendEnergy(PlayerColor player)
    {
        float currentEnergy = GetEnergy(player);
        return currentEnergy >= MOVE_COST;
    }

    /// <summary>
    /// ����ָ����ҵ�������
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
    /// ��ȡָ����ҵ�ǰ������ֵ���������֣���
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