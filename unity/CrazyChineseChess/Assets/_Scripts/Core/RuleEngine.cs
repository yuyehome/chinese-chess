// File: _Scripts/Core/RuleEngine.cs
using System.Collections.Generic;
using UnityEngine;

public static class RuleEngine
{
    public static List<Vector2Int> GetValidMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        switch (piece.Type)
        {
            case PieceType.Chariot:
                return GetChariotMoves(piece, position, boardState);
            // case PieceType.Horse:
            //     return GetHorseMoves(piece, position, boardState);
            // ... 其他棋子待实现
        }
        return new List<Vector2Int>(); // 默认返回空列表
    }

    private static List<Vector2Int> GetChariotMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        
        // 定义四个方向: 上, 下, 左, 右
        int[] dirX = { 0, 0, -1, 1 };
        int[] dirY = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            for (int step = 1; ; step++)
            {
                Vector2Int nextPos = new Vector2Int(position.x + dirX[i] * step, position.y + dirY[i] * step);

                // 检查是否越界
                if (!boardState.IsWithinBounds(nextPos))
                    break; // 超出边界，换下一个方向

                Piece targetPiece = boardState.GetPieceAt(nextPos);

                if (targetPiece.Type == PieceType.None)
                {
                    // 目标位置是空的，可以移动
                    moves.Add(nextPos);
                }
                else
                {
                    // 目标位置有棋子
                    if (targetPiece.Color != piece.Color)
                    {
                        // 是敌方棋子，可以吃掉，然后停止这个方向的搜索
                        moves.Add(nextPos);
                    }
                    // 如果是友方棋子，则不能移动到该位置，也停止这个方向的搜索
                    break; 
                }
            }
        }
        return moves;
    }
}