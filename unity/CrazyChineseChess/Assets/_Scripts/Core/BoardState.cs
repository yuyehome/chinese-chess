// File: _Scripts/Core/BoardState.cs
using UnityEngine;

public class BoardState
{
    // C#中的二维数组。我们将用 Piece 结构体填充它
    // [x, y] -> x是横坐标，y是纵坐标
    // 中国象棋是 9x10 的棋盘
    public const int BOARD_WIDTH = 9;
    public const int BOARD_HEIGHT = 10;
    
    private Piece[,] board = new Piece[BOARD_WIDTH, BOARD_HEIGHT];

    /// <summary>
    /// 初始化棋盘到标准开局状态。
    /// 坐标系: 左下角为 (0,0)，红方在下，黑方在上。
    /// </summary>
    public void InitializeDefaultSetup()
    {
        // 1. 清空棋盘
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                board[x, y] = new Piece(PieceType.None, PlayerColor.None);
            }
        }

        // 2. 放置红方棋子 (Red Player, bottom side, y = 0 to 4)
        // 底线 (y=0)
        board[0, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);   // 车
        board[1, 0] = new Piece(PieceType.Horse, PlayerColor.Red);     // 马
        board[2, 0] = new Piece(PieceType.Elephant, PlayerColor.Red);  // 象
        board[3, 0] = new Piece(PieceType.Advisor, PlayerColor.Red);   // 士
        board[4, 0] = new Piece(PieceType.General, PlayerColor.Red);   // 帅
        board[5, 0] = new Piece(PieceType.Advisor, PlayerColor.Red);   // 士
        board[6, 0] = new Piece(PieceType.Elephant, PlayerColor.Red);  // 象
        board[7, 0] = new Piece(PieceType.Horse, PlayerColor.Red);     // 马
        board[8, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);   // 车
        
        // 炮线 (y=2)
        board[1, 2] = new Piece(PieceType.Cannon, PlayerColor.Red);    // 炮
        board[7, 2] = new Piece(PieceType.Cannon, PlayerColor.Red);    // 炮
        
        // 兵线 (y=3)
        board[0, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // 兵
        board[2, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // 兵
        board[4, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // 兵
        board[6, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // 兵
        board[8, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);   // 兵

        // 3. 放置黑方棋子 (Black Player, top side, y = 5 to 9)
        // 底线 (y=9)
        board[0, 9] = new Piece(PieceType.Chariot, PlayerColor.Black); // 车
        board[1, 9] = new Piece(PieceType.Horse, PlayerColor.Black);   // 马
        board[2, 9] = new Piece(PieceType.Elephant, PlayerColor.Black);// 象
        board[3, 9] = new Piece(PieceType.Advisor, PlayerColor.Black); // 士
        board[4, 9] = new Piece(PieceType.General, PlayerColor.Black); // 将
        board[5, 9] = new Piece(PieceType.Advisor, PlayerColor.Black); // 士
        board[6, 9] = new Piece(PieceType.Elephant, PlayerColor.Black);// 象
        board[7, 9] = new Piece(PieceType.Horse, PlayerColor.Black);   // 马
        board[8, 9] = new Piece(PieceType.Chariot, PlayerColor.Black); // 车

        // 炮线 (y=7)
        board[1, 7] = new Piece(PieceType.Cannon, PlayerColor.Black);  // 砲
        board[7, 7] = new Piece(PieceType.Cannon, PlayerColor.Black);  // 砲
        
        // 卒线 (y=6)
        board[0, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // 卒
        board[2, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // 卒
        board[4, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // 卒
        board[6, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // 卒
        board[8, 6] = new Piece(PieceType.Soldier, PlayerColor.Black); // 卒
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