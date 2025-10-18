// File: _Scripts/Core/RuleEngine.cs
using System.Collections.Generic;
using UnityEngine;

public static class RuleEngine
{
    /// <summary>
    /// ��ȡָ�������ڵ�ǰ����״̬�µ����кϷ��ƶ�λ��
    /// </summary>
    public static List<Vector2Int> GetValidMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        switch (piece.Type)
        {
            case PieceType.Chariot:
                return GetChariotMoves(piece, position, boardState);
            case PieceType.Horse:
                return GetHorseMoves(piece, position, boardState);
            case PieceType.Elephant:
                return GetElephantMoves(piece, position, boardState);
            case PieceType.Advisor:
                return GetAdvisorMoves(piece, position, boardState);
            case PieceType.General:
                return GetGeneralMoves(piece, position, boardState);
            case PieceType.Cannon:
                return GetCannonMoves(piece, position, boardState);
            case PieceType.Soldier:
                return GetSoldierMoves(piece, position, boardState);
        }
        return new List<Vector2Int>();
    }

    #region Piece-Specific Move Logic

    // �� (Chariot)
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
                if (targetPiece.Type == PieceType.None)
                {
                    moves.Add(nextPos);
                }
                else
                {
                    if (targetPiece.Color != piece.Color) moves.Add(nextPos);
                    break;
                }
            }
        }
        return moves;
    }

    // �� (Horse)
    private static List<Vector2Int> GetHorseMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        // 8��Ǳ�����Ͷ�Ӧ������λ��
        int[] dx = { 1, 2, 2, 1, -1, -2, -2, -1 };
        int[] dy = { 2, 1, -1, -2, -2, -1, 1, 2 };
        int[] legX = { 0, 1, 1, 0, 0, -1, -1, 0 };
        int[] legY = { 1, 0, 0, -1, -1, 0, 0, 1 };

        for (int i = 0; i < 8; i++)
        {
            Vector2Int targetPos = new Vector2Int(position.x + dx[i], position.y + dy[i]);
            Vector2Int legPos = new Vector2Int(position.x + legX[i], position.y + legY[i]);

            // ������������Ƿ���������
            if (!boardState.IsWithinBounds(targetPos) || !boardState.IsWithinBounds(legPos)) continue;

            // ��������Ƿ񱻱�ס
            if (boardState.GetPieceAt(legPos).Type != PieceType.None) continue;

            // �������Ƿ����ѷ�����
            Piece targetPiece = boardState.GetPieceAt(targetPos);
            if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color)
            {
                moves.Add(targetPos);
            }
        }
        return moves;
    }

    // �� (Elephant)
    private static List<Vector2Int> GetElephantMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        // 4��Ǳ�����Ͷ�Ӧ������λ��
        int[] dx = { 2, 2, -2, -2 };
        int[] dy = { 2, -2, 2, -2 };
        int[] eyeX = { 1, 1, -1, -1 };
        int[] eyeY = { 1, -1, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            Vector2Int targetPos = new Vector2Int(position.x + dx[i], position.y + dy[i]);
            Vector2Int eyePos = new Vector2Int(position.x + eyeX[i], position.y + eyeY[i]);

            if (!boardState.IsWithinBounds(targetPos) || !boardState.IsWithinBounds(eyePos)) continue;

            // ����Ƿ����
            if ((piece.Color == PlayerColor.Red && targetPos.y > 4) || (piece.Color == PlayerColor.Black && targetPos.y < 5)) continue;

            // ��������Ƿ���
            if (boardState.GetPieceAt(eyePos).Type != PieceType.None) continue;
            
            Piece targetPiece = boardState.GetPieceAt(targetPos);
            if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color)
            {
                moves.Add(targetPos);
            }
        }
        return moves;
    }

    // ʿ (Advisor)
    private static List<Vector2Int> GetAdvisorMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int[] dx = { 1, 1, -1, -1 };
        int[] dy = { 1, -1, 1, -1 };

        for (int i = 0; i < 4; i++)
        {
            Vector2Int targetPos = new Vector2Int(position.x + dx[i], position.y + dy[i]);

            if (!boardState.IsWithinBounds(targetPos)) continue;
            
            // ����Ƿ��ھŹ�����
            bool inPalace = targetPos.x >= 3 && targetPos.x <= 5 &&
                            ((piece.Color == PlayerColor.Red && targetPos.y >= 0 && targetPos.y <= 2) ||
                             (piece.Color == PlayerColor.Black && targetPos.y >= 7 && targetPos.y <= 9));
            if (!inPalace) continue;
            
            Piece targetPiece = boardState.GetPieceAt(targetPos);
            if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color)
            {
                moves.Add(targetPos);
            }
        }
        return moves;
    }

    // ��/˧ (General)
    private static List<Vector2Int> GetGeneralMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int[] dx = { 0, 0, 1, -1 };
        int[] dy = { 1, -1, 0, 0 };

        // �����ƶ�
        for (int i = 0; i < 4; i++)
        {
            Vector2Int targetPos = new Vector2Int(position.x + dx[i], position.y + dy[i]);
            if (!boardState.IsWithinBounds(targetPos)) continue;

            bool inPalace = targetPos.x >= 3 && targetPos.x <= 5 &&
                            ((piece.Color == PlayerColor.Red && targetPos.y >= 0 && targetPos.y <= 2) ||
                             (piece.Color == PlayerColor.Black && targetPos.y >= 7 && targetPos.y <= 9));
            if (!inPalace) continue;

            Piece targetPiece = boardState.GetPieceAt(targetPos);
            if (targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color)
            {
                moves.Add(targetPos);
            }
        }

        // ����������
        PlayerColor opponentColor = (piece.Color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        int step = (opponentColor == PlayerColor.Black) ? 1 : -1;
        bool canSeeOpponentGeneral = true;
        Vector2Int opponentGeneralPos = Vector2Int.zero;

        for (int y = position.y + step; boardState.IsWithinBounds(new Vector2Int(position.x, y)); y += step)
        {
            Piece p = boardState.GetPieceAt(new Vector2Int(position.x, y));
            if (p.Type != PieceType.None)
            {
                if (p.Type == PieceType.General && p.Color == opponentColor)
                {
                    opponentGeneralPos = new Vector2Int(position.x, y);
                    break; 
                }
                canSeeOpponentGeneral = false;
                break;
            }
        }
        if (canSeeOpponentGeneral && opponentGeneralPos != Vector2Int.zero)
        {
            moves.Add(opponentGeneralPos);
        }

        return moves;
    }
    
    // �� (Cannon)
    private static List<Vector2Int> GetCannonMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int[] dirX = { 0, 0, -1, 1 };
        int[] dirY = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            bool hasJumped = false; // �Ƿ��Ѿ�Խ��һ���ڼ�
            for (int step = 1; ; step++)
            {
                Vector2Int nextPos = new Vector2Int(position.x + dirX[i] * step, position.y + dirY[i] * step);
                if (!boardState.IsWithinBounds(nextPos)) break;
                
                Piece targetPiece = boardState.GetPieceAt(nextPos);
                if (targetPiece.Type == PieceType.None)
                {
                    if (!hasJumped) moves.Add(nextPos); // ûԽ���ڼ�ʱ���ո��ǺϷ��ƶ���
                }
                else
                {
                    if (!hasJumped)
                    {
                        hasJumped = true; // ��һ���������ӣ���Ϊ�ڼ�
                    }
                    else
                    {
                        // �ڶ����������ӣ�����ǵз������ԳԵ�
                        if (targetPiece.Color != piece.Color)
                        {
                            moves.Add(nextPos);
                        }
                        break; // ���۵��ң�ֹͣ����
                    }
                }
            }
        }
        return moves;
    }
    
    // �� (Soldier)
    private static List<Vector2Int> GetSoldierMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        int forwardDir = (piece.Color == PlayerColor.Red) ? 1 : -1;
        bool isCrossedRiver = (piece.Color == PlayerColor.Red && position.y >= 5) || (piece.Color == PlayerColor.Black && position.y <= 4);

        // 1. ǰ��
        Vector2Int forwardPos = new Vector2Int(position.x, position.y + forwardDir);
        if (boardState.IsWithinBounds(forwardPos))
        {
            Piece targetPiece = boardState.GetPieceAt(forwardPos);
            if(targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color)
            {
                moves.Add(forwardPos);
            }
        }

        // 2. ���� (���Ӻ�)
        if (isCrossedRiver)
        {
            // ����
            Vector2Int leftPos = new Vector2Int(position.x - 1, position.y);
             if (boardState.IsWithinBounds(leftPos))
            {
                Piece targetPiece = boardState.GetPieceAt(leftPos);
                if(targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color)
                {
                    moves.Add(leftPos);
                }
            }
            // ����
            Vector2Int rightPos = new Vector2Int(position.x + 1, position.y);
             if (boardState.IsWithinBounds(rightPos))
            {
                Piece targetPiece = boardState.GetPieceAt(rightPos);
                if(targetPiece.Type == PieceType.None || targetPiece.Color != piece.Color)
                {
                    moves.Add(rightPos);
                }
            }
        }
        return moves;
    }
    
    #endregion
}