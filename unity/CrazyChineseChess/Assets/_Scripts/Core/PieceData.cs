// File: _Scripts/Core/PieceData.cs

/// <summary>
/// �����ɫö�١�
/// </summary>
public enum PlayerColor { None, Red, Black }

/// <summary>
/// ��������ö�١�
/// </summary>
public enum PieceType { None, Chariot, Horse, Elephant, Advisor, General, Cannon, Soldier }

/// <summary>
/// ����һ�������߼�״̬�Ĵ����ݽṹ�塣
/// ��ֻ���������ӵġ���ݡ������ͺ���ɫ�����������κ���Ϊ��Unity�����
/// </summary>
public struct Piece
{
    public PieceType Type;
    public PlayerColor Color;

    /// <summary>
    /// ����һ���µ�����ʵ����
    /// </summary>
    public Piece(PieceType type, PlayerColor color)
    {
        this.Type = type;
        this.Color = color;
    }
}


/// <summary>
/// ö�٣����ڱ�ʾ��Ϸ��ǰ�ļ��ֽ���״̬��
/// </summary>
public enum GameStatus
{
    Ongoing,    // ��Ϸ��
    RedWin,     // �췽ʤ
    BlackWin,   // �ڷ�ʤ
    Stalemate   // ����
}