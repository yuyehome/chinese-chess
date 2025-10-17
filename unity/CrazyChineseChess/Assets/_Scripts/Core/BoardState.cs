
using UnityEngine; // ������ҪVector2Int

public class BoardState
{
    // C#�еĶ�ά���顣���ǽ��� Piece �ṹ�������
    // [x, y] -> x�Ǻ����꣬y��������
    // �й������� 9x10 ������
    public const int BOARD_WIDTH = 9;
    public const int BOARD_HEIGHT = 10;
    
    private Piece[,] board = new Piece[BOARD_WIDTH, BOARD_HEIGHT];

    // ��ʼ�����̵���׼����״̬
    public void InitializeDefaultSetup()
    {
        // �������
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                board[x, y] = new Piece(PieceType.None, PlayerColor.None);
            }
        }

        // �����Ƿ������ӵ��߼�...
        // �ٸ�����:
        // �췽��
        board[0, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);
        board[8, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);
        // �ڷ���
        board[0, 9] = new Piece(PieceType.Chariot, PlayerColor.Black);
        board[8, 9] = new Piece(PieceType.Chariot, PlayerColor.Black);

        // ... ������й������������������ӵĳ�ʼ����
    }
    
    // ��ȡָ��λ�õ�����
    public Piece GetPieceAt(Vector2Int position)
    {
        if (IsWithinBounds(position))
        {
            return board[position.x, position.y];
        }
        return new Piece(PieceType.None, PlayerColor.None);
    }

    // �ƶ����� (��ֻ��һ�����ݲ���������������У��)
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        if (IsWithinBounds(from) && IsWithinBounds(to))
        {
            Piece movingPiece = board[from.x, from.y];
            board[to.x, to.y] = movingPiece;
            board[from.x, from.y] = new Piece(PieceType.None, PlayerColor.None);
        }
    }
    
    // ��������Ƿ���������
    public bool IsWithinBounds(Vector2Int position)
    {
        return position.x >= 0 && position.x < BOARD_WIDTH && 
               position.y >= 0 && position.y < BOARD_HEIGHT;
    }
}