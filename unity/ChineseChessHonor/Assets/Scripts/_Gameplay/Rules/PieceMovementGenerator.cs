// 文件路径: Assets/Scripts/_Gameplay/Rules/PieceMovementGenerator.cs

using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 纯静态工具类，用于生成各种棋子在给定棋盘尺寸下的“理论”移动范围。
/// “理论”意味着不考虑棋盘上其他棋子的阻挡（马腿、象眼等由上层逻辑处理）。
/// 这个类的所有方法都是无状态的纯函数。
/// </summary>
public static class PieceMovementGenerator
{
    // 定义棋盘的常量边界
    private const int MinX = 0;
    private const int MinY = 0;

    #region 公共调用接口 (Public Interface)

    public static List<Vector2Int> GetTheoreticalMoves(PieceType type, Vector2Int fromPos, PlayerTeam team, Vector2Int boardSize)
    {
        switch (type)
        {
            case PieceType.General:
                return GetGeneralMoves(fromPos, team, boardSize);
            case PieceType.Advisor:
                return GetAdvisorMoves(fromPos, team, boardSize);
            case PieceType.Elephant:
                return GetElephantMoves(fromPos, team, boardSize);
            case PieceType.Horse:
                return GetHorseMoves(fromPos, boardSize);
            case PieceType.Chariot:
                return GetChariotMoves(fromPos, boardSize);
            case PieceType.Cannon:
                return GetChariotMoves(fromPos, boardSize); // 炮的移动规则和车一样
            case PieceType.Soldier:
                return GetSoldierMoves(fromPos, team, boardSize);
            default:
                return new List<Vector2Int>();
        }
    }

    #endregion

    #region 各棋子具体实现 (Private Implementations)

    // 将/帅
    private static List<Vector2Int> GetGeneralMoves(Vector2Int fromPos, PlayerTeam team, Vector2Int boardSize)
    {
        var moves = new List<Vector2Int>();
        // 定义九宫格范围
        int minPalaceX = 3;
        int maxPalaceX = 5;
        int minPalaceY = (team == PlayerTeam.Red) ? 0 : 7;
        int maxPalaceY = (team == PlayerTeam.Red) ? 2 : 9;

        // 上下左右移动一格
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            var toPos = new Vector2Int(fromPos.x + dx[i], fromPos.y + dy[i]);
            if (toPos.x >= minPalaceX && toPos.x <= maxPalaceX && toPos.y >= minPalaceY && toPos.y <= maxPalaceY)
            {
                moves.Add(toPos);
            }
        }
        return moves;
    }

    // 士
    private static List<Vector2Int> GetAdvisorMoves(Vector2Int fromPos, PlayerTeam team, Vector2Int boardSize)
    {
        var moves = new List<Vector2Int>();
        // 定义九宫格范围
        int minPalaceX = 3;
        int maxPalaceX = 5;
        int minPalaceY = (team == PlayerTeam.Red) ? 0 : 7;
        int maxPalaceY = (team == PlayerTeam.Red) ? 2 : 9;

        // 斜向移动一格
        int[] dx = { 1, 1, -1, -1 };
        int[] dy = { 1, -1, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            var toPos = new Vector2Int(fromPos.x + dx[i], fromPos.y + dy[i]);
            if (toPos.x >= minPalaceX && toPos.x <= maxPalaceX && toPos.y >= minPalaceY && toPos.y <= maxPalaceY)
            {
                moves.Add(toPos);
            }
        }
        return moves;
    }

    // 象
    private static List<Vector2Int> GetElephantMoves(Vector2Int fromPos, PlayerTeam team, Vector2Int boardSize)
    {
        var moves = new List<Vector2Int>();
        // 走"田"字
        int[] dx = { 2, 2, -2, -2 };
        int[] dy = { 2, -2, 2, -2 };

        // 定义过河线
        int riverY = 4;

        for (int i = 0; i < 4; i++)
        {
            var toPos = new Vector2Int(fromPos.x + dx[i], fromPos.y + dy[i]);

            // 检查边界
            if (IsOutOfBounds(toPos, boardSize)) continue;

            // 检查是否过河
            if (team == PlayerTeam.Red && toPos.y > riverY) continue;
            if (team == PlayerTeam.Black && toPos.y < riverY + 1) continue;

            moves.Add(toPos);
        }
        return moves;
    }

    // 马
    private static List<Vector2Int> GetHorseMoves(Vector2Int fromPos, Vector2Int boardSize)
    {
        var moves = new List<Vector2Int>();
        // 8个方向的"日"字
        int[] dx = { 1, 1, 2, 2, -1, -1, -2, -2 };
        int[] dy = { 2, -2, 1, -1, 2, -2, 1, -1 };

        for (int i = 0; i < 8; i++)
        {
            var toPos = new Vector2Int(fromPos.x + dx[i], fromPos.y + dy[i]);
            if (!IsOutOfBounds(toPos, boardSize))
            {
                moves.Add(toPos);
            }
        }
        return moves;
    }

    // 车 (和炮的移动)
    private static List<Vector2Int> GetChariotMoves(Vector2Int fromPos, Vector2Int boardSize)
    {
        var moves = new List<Vector2Int>();
        int maxX = boardSize.x - 1;
        int maxY = boardSize.y - 1;

        // 向右
        for (int x = fromPos.x + 1; x <= maxX; x++) moves.Add(new Vector2Int(x, fromPos.y));
        // 向左
        for (int x = fromPos.x - 1; x >= MinX; x--) moves.Add(new Vector2Int(x, fromPos.y));
        // 向上
        for (int y = fromPos.y + 1; y <= maxY; y++) moves.Add(new Vector2Int(fromPos.x, y));
        // 向下
        for (int y = fromPos.y - 1; y >= MinY; y--) moves.Add(new Vector2Int(fromPos.x, y));

        return moves;
    }

    // 兵
    private static List<Vector2Int> GetSoldierMoves(Vector2Int fromPos, PlayerTeam team, Vector2Int boardSize)
    {
        var moves = new List<Vector2Int>();
        int riverY = 4; // 红方过河线

        if (team == PlayerTeam.Red)
        {
            // 永远可以向前
            var forward = new Vector2Int(fromPos.x, fromPos.y + 1);
            if (!IsOutOfBounds(forward, boardSize)) moves.Add(forward);

            // 过河后可以左右移动
            if (fromPos.y > riverY)
            {
                var left = new Vector2Int(fromPos.x - 1, fromPos.y);
                if (!IsOutOfBounds(left, boardSize)) moves.Add(left);

                var right = new Vector2Int(fromPos.x + 1, fromPos.y);
                if (!IsOutOfBounds(right, boardSize)) moves.Add(right);
            }
        }
        else // Black Team
        {
            // 永远可以向前
            var forward = new Vector2Int(fromPos.x, fromPos.y - 1);
            if (!IsOutOfBounds(forward, boardSize)) moves.Add(forward);

            // 过河后可以左右移动 (黑方的河界是y=5, 过河后y<5)
            if (fromPos.y < riverY + 1)
            {
                var left = new Vector2Int(fromPos.x - 1, fromPos.y);
                if (!IsOutOfBounds(left, boardSize)) moves.Add(left);

                var right = new Vector2Int(fromPos.x + 1, fromPos.y);
                if (!IsOutOfBounds(right, boardSize)) moves.Add(right);
            }
        }
        return moves;
    }

    #endregion

    #region 辅助工具 (Helper Utilities)

    // 检查坐标是否越界
    private static bool IsOutOfBounds(Vector2Int pos, Vector2Int boardSize)
    {
        return pos.x < MinX || pos.x >= boardSize.x || pos.y < MinY || pos.y >= boardSize.y;
    }

    #endregion
}