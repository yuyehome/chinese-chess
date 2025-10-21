// File: _Scripts/Core/PieceValue.cs

/// <summary>
/// �ṩ�����Ӽ�ֵ��صľ�̬���ݺͷ�����
/// </summary>
public static class PieceValue
{
    /// <summary>
    /// ��ȡһ���������͵ļ�ֵ����ֵԽ�ߣ���ֵԽ��
    /// </summary>
    public static int GetValue(PieceType type)
    {
        switch (type)
        {
            case PieceType.General: return 100; // ��/˧ 
            case PieceType.Chariot: return 90;   // ��
            case PieceType.Cannon: return 70;   // ��
            case PieceType.Horse: return 50;   // ��
            case PieceType.Elephant: return 30;   // ��
            case PieceType.Advisor: return 20;   // ʿ
            case PieceType.Soldier: return 10;   // ��
            default: return 0;   // ��
        }
    }
}