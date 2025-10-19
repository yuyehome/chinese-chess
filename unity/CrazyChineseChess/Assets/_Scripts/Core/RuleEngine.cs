// File: _Scripts/Core/RuleEngine.cs
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 【已修正和恢复】规则引擎，移除了“自杀”限制，严格遵循棋子自身的基础移动和攻击规则。
/// </summary>
public static class RuleEngine
{
    #region Public API

    /// <summary>
    /// 获取一个棋子所有可能的移动点。
    /// 该方法现在只检查棋子本身的移动规则。
    /// </summary>
    public static List<Vector2Int> GetValidMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        switch (piece.Type)
        {
            case PieceType.Chariot: return GetChariotMoves(piece, position, boardState);
            case PieceType.Horse: return GetHorseMoves(piece, position, boardState);
            case PieceType.Elephant: return GetElephantMoves(piece, position, boardState);
            case PieceType.Advisor: return GetAdvisorMoves(piece, position, boardState);
            case PieceType.General: return GetGeneralMoves(piece, position, boardState);
            case PieceType.Cannon: return GetCannonMoves(piece, position, boardState);
            case PieceType.Soldier: return GetSoldierMoves(piece, position, boardState);
        }
        return new List<Vector2Int>();
    }

    /// <summary>
    /// 检查指定颜色的王是否正处于被将军的状态。
    /// </summary>
    public static bool IsKingInCheck(PlayerColor kingColor, BoardState boardState)
    {
        Vector2Int kingPos = FindKingPosition(kingColor, boardState);
        if (kingPos == new Vector2Int(-1, -1)) return false;

        PlayerColor attackerColor = (kingColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        return IsPositionUnderAttack(kingPos, attackerColor, boardState);
    }

    #endregion

    #region Helper Methods

    private static Vector2Int FindKingPosition(PlayerColor kingColor, BoardState boardState)
    {
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Piece piece = boardState.GetPieceAt(new Vector2Int(x, y));
                if (piece.Type == PieceType.General && piece.Color == kingColor)
                {
                    return new Vector2Int(x, y);
                }
            }
        }
        return new Vector2Int(-1, -1);
    }

    private static bool IsPositionUnderAttack(Vector2Int position, PlayerColor attackerColor, BoardState boardState)
    {
        for (int x = 0; x < BoardState.BOARD_WIDTH; x++)
        {
            for (int y = 0; y < BoardState.BOARD_HEIGHT; y++)
            {
                Piece piece = boardState.GetPieceAt(new Vector2Int(x, y));
                if (piece.Color == attackerColor)
                {
                    var moves = GetValidMoves(piece, new Vector2Int(x, y), boardState);
                    if (moves.Contains(position))
                    {
                        return true;
                    }
                }
            }
        }
        return false;
    }

    #endregion

    #region Piece-Specific Move Logic (已全部恢复)

    private static List<Vector2Int> GetChariotMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int[] dirX = { 0, 0, -1, 1 };
        int[] dirY = { 1, -1, 0, 0 };
        for (int i = 0; i < 4; i++)
        {
            for (int step = 1; ; step++)
            {
                Vector2Int nextPos = new Vector2Int(position.x + dirX[i] * step, position.y + dirY[i] * step);
                if (!boardState.IsWithinBounds(nextPos)) break;
                Piece targetPiece = boardState.GetPieceAt(nextPos);
                if (targetPiece.Type == PieceType.None) moves.Add(nextPos);
                else { if (targetPiece.Color != piece.Color) moves.Add(nextPos); break; }
            }
        }
        return moves;
    }

    private static List<Vector2Int> GetHorseMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int[] dx = { 1, 2, 2, 1, -1, -2, -2, -1 };
        int[] dy = { 2, 1, -1, -2, -2, -1, 1, 2 };
        int[] legX = { 0, 1, 1, 0, 0, -1, -1, 0 };
        int[] legY = { 1, 0, 0, -1, -1, 0, 0, 1 };
        for (int i = 0; i < 8; i++)
        {
            Vector2Int targetPos = new Vector2Int(position.x + dx[i], position.y + dy[i]);
            Vector2Int legPos = new Vector2Int(position.x + legX[i], position.y + legY[i]);
            if (!boardState.IsWithinBounds(targetPos) || !boardState.IsWithinBounds(legPos)) continue;
            if (boardState.GetPieceAt(legPos).Type != PieceType.None) continue;
            Piece targetPiece = boardState.GetPieceAt(targetPos);
            if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color) moves.Add(targetPos);
        }
        return moves;
    }

    private static List<Vector2Int> GetElephantMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int[] dx = { 2, 2, -2, -2 };
        int[] dy = { 2, -2, 2, -2 };
        int[] eyeX = { 1, 1, -1, -1 };
        int[] eyeY = { 1, -1, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            Vector2Int targetPos = new Vector2Int(position.x + dx[i], position.y + dy[i]);
            Vector2Int eyePos = new Vector2Int(position.x + eyeX[i], position.y + eyeY[i]);
            if (!boardState.IsWithinBounds(targetPos) || !boardState.IsWithinBounds(eyePos)) continue;
            if ((piece.Color == PlayerColor.Red && targetPos.y > 4) || (piece.Color == PlayerColor.Black && targetPos.y < 5)) continue;
            if (boardState.GetPieceAt(eyePos).Type != PieceType.None) continue;
            Piece targetPiece = boardState.GetPieceAt(targetPos);
            if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color) moves.Add(targetPos);
        }
        return moves;
    }

    private static List<Vector2Int> GetAdvisorMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int[] dx = { 1, 1, -1, -1 };
        int[] dy = { 1, -1, 1, -1 };
        for (int i = 0; i < 4; i++)
        {
            Vector2Int targetPos = new Vector2Int(position.x + dx[i], position.y + dy[i]);
            if (!boardState.IsWithinBounds(targetPos)) continue;
            bool inPalace = targetPos.x >= 3 && targetPos.x <= 5 && ((piece.Color == PlayerColor.Red && targetPos.y >= 0 && targetPos.y <= 2) || (piece.Color == PlayerColor.Black && targetPos.y >= 7 && targetPos.y <= 9));
            if (!inPalace) continue;
            Piece targetPiece = boardState.GetPieceAt(targetPos);
            if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color) moves.Add(targetPos);
        }
        return moves;
    }

    private static List<Vector2Int> GetGeneralMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };
        for (int i = 0; i < 4; i++)
        {
            Vector2Int targetPos = new Vector2Int(position.x + dx[i], position.y + dy[i]);
            if (!boardState.IsWithinBounds(targetPos)) continue;
            bool inPalace = targetPos.x >= 3 && targetPos.x <= 5 && ((piece.Color == PlayerColor.Red && targetPos.y >= 0 && targetPos.y <= 2) || (piece.Color == PlayerColor.Black && targetPos.y >= 7 && targetPos.y <= 9));
            if (!inPalace) continue;
            Piece targetPiece = boardState.GetPieceAt(targetPos);
            if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color) moves.Add(targetPos);
        }
        PlayerColor opponentColor = (piece.Color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        int step = (opponentColor == PlayerColor.Black) ? 1 : -1;
        for (int y = position.y + step; boardState.IsWithinBounds(new Vector2Int(position.x, y)); y += step)
        {
            Piece p = boardState.GetPieceAt(new Vector2Int(position.x, y));
            if (p.Type != PieceType.None)
            {
                if (p.Type == PieceType.General && p.Color == opponentColor) moves.Add(new Vector2Int(position.x, y));
                break;
            }
        }
        return moves;
    }

    private static List<Vector2Int> GetCannonMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int[] dirX = { 0, 0, -1, 1 };
        int[] dirY = { 1, -1, 0, 0 };
        for (int i = 0; i < 4; i++)
        {
            bool hasJumped = false;
            for (int step = 1; ; step++)
            {
                Vector2Int nextPos = new Vector2Int(position.x + dirX[i] * step, position.y + dirY[i] * step);
                if (!boardState.IsWithinBounds(nextPos)) break;
                Piece targetPiece = boardState.GetPieceAt(nextPos);
                if (targetPiece.Type == PieceType.None)
                {
                    if (!hasJumped) moves.Add(nextPos);
                }
                else
                {
                    if (!hasJumped) hasJumped = true;
                    else { if (targetPiece.Color != piece.Color) moves.Add(nextPos); break; }
                }
            }
        }
        return moves;
    }

    private static List<Vector2Int> GetSoldierMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int forwardDir = (piece.Color == PlayerColor.Red) ? 1 : -1;
        bool isCrossedRiver = (piece.Color == PlayerColor.Red && position.y >= 5) || (piece.Color == PlayerColor.Black && position.y <= 4);
        Vector2Int forwardPos = new Vector2Int(position.x, position.y + forwardDir);
        if (boardState.IsWithinBounds(forwardPos))
        {
            Piece targetPiece = boardState.GetPieceAt(forwardPos);
            if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color) moves.Add(forwardPos);
        }
        if (isCrossedRiver)
        {
            Vector2Int leftPos = new Vector2Int(position.x - 1, position.y);
            if (boardState.IsWithinBounds(leftPos))
            {
                Piece targetPiece = boardState.GetPieceAt(leftPos);
                if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color) moves.Add(leftPos);
            }
            Vector2Int rightPos = new Vector2Int(position.x + 1, position.y);
            if (boardState.IsWithinBounds(rightPos))
            {
                Piece targetPiece = boardState.GetPieceAt(rightPos);
                if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color) moves.Add(rightPos);
            }
        }
        return moves;
    }

    #endregion
}