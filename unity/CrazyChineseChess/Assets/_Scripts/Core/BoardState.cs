// File: _Scripts/Core/BoardState.cs
using UnityEngine;

public class BoardState
{
    // C#�еĶ�ά���顣���ǽ��� Piece �ṹ�������
    // [x, y] -> x�Ǻ����꣬y��������
    // �й������� 9x10 ������
    public const int BOARD_WIDTH = 9;
    public const int BOARD_HEIGHT = 10;
    
    private Piece[,] board = new Piece[BOARD_WIDTH, BOARD_HEIGHT];

    /// <summary>
    /// ��ʼ�����̵���׼����״̬��
    /// ����ϵ: ���½�Ϊ (0,0)���췽���£��ڷ����ϡ�
    /// </summary>
    public void InitializeDefaultSetup()
    {
        // 1. �������
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                board[x, y] = new Piece(PieceType.None, PlayerColor.None);
            }
        }

        // 2. ���ú췽���� (Red Player, bottom side, y = 0 to 4)
        // ���� (y=0)
        board[0, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);   // ��
        board[1, 0] = new Piece(PieceType.Horse, PlayerColor.Red);     // ��
        board[2, 0] = new Piece(PieceType.Elephant, PlayerColor.Red);  // ��
        board[3, 0] = new Piece(PieceType.Advisor, PlayerColor.Red);   // ʿ
        board[4, 0] = new Piece(PieceType.General, PlayerColor.Red);   // ˧
        board[5, 0] = new Piece(PieceType.Advisor, PlayerColor.Red);   // ʿ
        board[6, 0] = new Piece(PieceType.Elephant, PlayerColor.Red);  // ��
        board[7, 0] = new Piece(PieceType.Horse, PlayerColor.Red);     // ��
        board[8, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);   // ��
        
        // ���� (y=2)
        board[1, 2] = new Piece(PieceType.Cannon, PlayerColor.Red);    // ��
        board[7, 2] = new Piece(PieceType.Cannon, PlayerColor.Red);    // ��
        
        // ���� (y=3)
        board[0, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // ��
        board[2, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // ��
        board[4, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // ��
        board[6, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // ��
        board[8, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // ��

        // 3. ���úڷ����� (Black Player, top side, y = 5 to 9)
        // ���� (y=9)
        board[0, 9] = new Piece(PieceType.Chariot, PlayerColor.Black); // ��
        board[1, 9] = new Piece(PieceType.Horse, PlayerColor.Black);   // ��
        board[2, 9] = new Piece(PieceType.Elephant, PlayerColor.Black);// ��
        board[3, 9] = new Piece(PieceType.Advisor, PlayerColor.Black); // ʿ
        board[4, 9] = new Piece(PieceType.General, PlayerColor.Black); // ��
        board[5, 9] = new Piece(PieceType.Advisor, PlayerColor.Black); // ʿ
        board[6, 9] = new Piece(PieceType.Elephant, PlayerColor.Black);// ��
        board[7, 9] = new Piece(PieceType.Horse, PlayerColor.Black);   // ��
        board[8, 9] = new Piece(PieceType.Chariot, PlayerColor.Black); // ��

        // ���� (y=7)
        board[1, 7] = new Piece(PieceType.Cannon, PlayerColor.Black);  // �h
        board[7, 7] = new Piece(PieceType.Cannon, PlayerColor.Black);  // �h
        
        // ���� (y=6)
        board[0, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // ��
        board[2, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // ��
        board[4, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // ��
        board[6, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // ��
        board[8, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // ��
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