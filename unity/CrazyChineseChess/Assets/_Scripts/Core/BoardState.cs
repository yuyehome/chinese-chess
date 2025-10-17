
using UnityEngine; // 我们需要Vector2Int

public class BoardState
{
    // C#中的二维数组。我们将用 Piece 结构体填充它
    // [x, y] -> x是横坐标，y是纵坐标
    // 中国象棋是 9x10 的棋盘
    public const int BOARD_WIDTH = 9;
    public const int BOARD_HEIGHT = 10;
    
    private Piece[,] board = new Piece[BOARD_WIDTH, BOARD_HEIGHT];

    // 初始化棋盘到标准开局状态
    public void InitializeDefaultSetup()
    {
        // 清空棋盘
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                board[x, y] = new Piece(PieceType.None, PlayerColor.None);
            }
        }

        // 这里是放置棋子的逻辑...
        // 举个例子:
        // 红方车
        board[0, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);
        board[8, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);
        // 黑方车
        board[0, 9] = new Piece(PieceType.Chariot, PlayerColor.Black);
        board[8, 9] = new Piece(PieceType.Chariot, PlayerColor.Black);

        // ... 请根据中国象棋规则，完成所有棋子的初始放置
    }
    
    // 获取指定位置的棋子
    public Piece GetPieceAt(Vector2Int position)
    {
        if (IsWithinBounds(position))
        {
            return board[position.x, position.y];
        }
        return new Piece(PieceType.None, PlayerColor.None);
    }

    // 移动棋子 (这只是一个数据操作，不包含规则校验)
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        if (IsWithinBounds(from) && IsWithinBounds(to))
        {
            Piece movingPiece = board[from.x, from.y];
            board[to.x, to.y] = movingPiece;
            board[from.x, from.y] = new Piece(PieceType.None, PlayerColor.None);
        }
    }
    
    // 检查坐标是否在棋盘内
    public bool IsWithinBounds(Vector2Int position)
    {
        return position.x >= 0 && position.x < BOARD_WIDTH && 
               position.y >= 0 && position.y < BOARD_HEIGHT;
    }
}