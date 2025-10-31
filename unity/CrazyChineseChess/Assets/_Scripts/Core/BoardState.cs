// File: _Scripts/Core/BoardState.cs

using UnityEngine;
using System.Collections.Generic;

public class BoardState
{
    public const int BOARD_WIDTH = 9;
    public const int BOARD_HEIGHT = 10;

    private Piece[,] board = new Piece[BOARD_WIDTH, BOARD_HEIGHT];

    public void InitializeDefaultSetup()
    {
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                board[x, y] = new Piece(PieceType.None, PlayerColor.None);
            }
        }

        #region Piece Placement
        board[0, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);
        board[1, 0] = new Piece(PieceType.Horse, PlayerColor.Red);
        board[2, 0] = new Piece(PieceType.Elephant, PlayerColor.Red);
        board[3, 0] = new Piece(PieceType.Advisor, PlayerColor.Red);
        board[4, 0] = new Piece(PieceType.General, PlayerColor.Red);
        board[5, 0] = new Piece(PieceType.Advisor, PlayerColor.Red);
        board[6, 0] = new Piece(PieceType.Elephant, PlayerColor.Red);
        board[7, 0] = new Piece(PieceType.Horse, PlayerColor.Red);
        board[8, 0] = new Piece(PieceType.Chariot, PlayerColor.Red);
        board[1, 2] = new Piece(PieceType.Cannon, PlayerColor.Red);
        board[7, 2] = new Piece(PieceType.Cannon, PlayerColor.Red);
        board[0, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);
        board[2, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);
        board[4, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);
        board[6, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);
        board[8, 3] = new Piece(PieceType.Soldier, PlayerColor.Red);

        board[0, 9] = new Piece(PieceType.Chariot, PlayerColor.Black);
        board[1, 9] = new Piece(PieceType.Horse, PlayerColor.Black);
        board[2, 9] = new Piece(PieceType.Elephant, PlayerColor.Black);
        board[3, 9] = new Piece(PieceType.Advisor, PlayerColor.Black);
        board[4, 9] = new Piece(PieceType.General, PlayerColor.Black);
        board[5, 9] = new Piece(PieceType.Advisor, PlayerColor.Black);
        board[6, 9] = new Piece(PieceType.Elephant, PlayerColor.Black);
        board[7, 9] = new Piece(PieceType.Horse, PlayerColor.Black);
        board[8, 9] = new Piece(PieceType.Chariot, PlayerColor.Black);
        board[1, 7] = new Piece(PieceType.Cannon, PlayerColor.Black);
        board[7, 7] = new Piece(PieceType.Cannon, PlayerColor.Black);
        board[0, 6] = new Piece(PieceType.Soldier, PlayerColor.Black);
        board[2, 6] = new Piece(PieceType.Soldier, PlayerColor.Black);
        board[4, 6] = new Piece(PieceType.Soldier, PlayerColor.Black);
        board[6, 6] = new Piece(PieceType.Soldier, PlayerColor.Black);
        board[8, 6] = new Piece(PieceType.Soldier, PlayerColor.Black);
        #endregion
    }

    /// <summary>
    /// 获取指定位置的静止棋子。
    /// </summary>
    public Piece GetPieceAt(Vector2Int position)
    {
        if (IsWithinBounds(position))
        {
            return board[position.x, position.y];
        }
        return new Piece(PieceType.None, PlayerColor.None);
    }

    /// <summary>
    /// 在指定位置设置一个静止棋子。
    /// 主要用于棋子移动完成或动态构建逻辑棋盘时。
    /// </summary>
    public void SetPieceAt(Vector2Int position, Piece piece)
    {
        if (IsWithinBounds(position))
        {
            board[position.x, position.y] = piece;
        }
    }

    /// <summary>
    /// 在棋盘上移动一个静止棋子（用于回合制模式）。
    /// </summary>
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        if (IsWithinBounds(from) && IsWithinBounds(to))
        {
            Piece movingPiece = board[from.x, from.y];
            board[to.x, to.y] = movingPiece;
            board[from.x, from.y] = new Piece(PieceType.None, PlayerColor.None);
        }
    }

    /// <summary>
    /// 从棋盘上移除一个静止棋子（将其设置为空）。
    /// </summary>
    public void RemovePieceAt(Vector2Int position)
    {
        if (IsWithinBounds(position))
        {
            board[position.x, position.y] = new Piece(PieceType.None, PlayerColor.None);
        }
    }

    /// <summary>
    /// 检查坐标是否在棋盘的有效范围内。
    /// </summary>
    public bool IsWithinBounds(Vector2Int position)
    {
        return position.x >= 0 && position.x < BOARD_WIDTH &&
               position.y >= 0 && position.y < BOARD_HEIGHT;
    }

    /// <summary>
    /// 创建并返回当前棋盘状态的一个深拷贝副本。
    /// </summary>
    public BoardState Clone()
    {
        BoardState newBoardState = new BoardState();
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                newBoardState.board[x, y] = this.board[x, y];
            }
        }
        return newBoardState;
    }

    /// <summary>
    /// [线程安全] 获取当前棋盘上所有非空棋子的位置和数据。
    /// </summary>
    /// <returns>一个包含所有棋子信息的列表。</returns>
    public List<(Piece PieceData, Vector2Int Position)> GetAllPieces()
    {
        var pieces = new List<(Piece, Vector2Int)>();
        for (int x = 0; x < BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BOARD_HEIGHT; y++)
            {
                if (board[x, y].Type != PieceType.None)
                {
                    pieces.Add((board[x, y], new Vector2Int(x, y)));
                }
            }
        }
        return pieces;
    }

}