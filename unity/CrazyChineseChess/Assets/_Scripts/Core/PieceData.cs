// File: _Scripts/Core/PieceData.cs

// ʹ��ö�٣����������Ҳ��׳���
public enum PlayerColor { None, Red, Black }
public enum PieceType { None, Chariot, Horse, Elephant, Advisor, General, Cannon, Soldier }

// ����һ�������ݽṹ�����̳��κ�Unity�Ķ��� (like MonoBehaviour)
// ��������һ�����ӵ��߼�״̬�������������Ӿ�����
public struct Piece
{
    public PieceType Type;
    public PlayerColor Color;

    // ���캯�������㴴��
    public Piece(PieceType type, PlayerColor color)
    {
        this.Type = type;
        this.Color = color;
    }
}


#region Game State Enums

/// <summary>
/// ö�٣����ڱ�ʾ��Ϸ��ǰ�ļ���״̬��
/// </summary>
public enum GameStatus
{
    Ongoing,    // ��Ϸ��
    RedWin,     // �췽ʤ
    BlackWin,   // �ڷ�ʤ
    Stalemate   // ����
}

#endregion