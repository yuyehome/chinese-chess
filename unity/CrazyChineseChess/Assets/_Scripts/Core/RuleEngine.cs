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
            // ... �������Ӵ�ʵ��
        }
        return new List<Vector2Int>(); // Ĭ�Ϸ��ؿ��б�
    }

    private static List<Vector2Int> GetChariotMoves(Piece piece, Vector2Int position, BoardState boardState)
    {
        var moves = new List<Vector2Int>();
        
        // �����ĸ�����: ��, ��, ��, ��
        int[] dirX = { 0, 0, -1, 1 };
        int[] dirY = { 1, -1, 0, 0 };

        for (int i = 0; i < 4; i++)
        {
            for (int step = 1; ; step++)
            {
                Vector2Int nextPos = new Vector2Int(position.x + dirX[i] * step, position.y + dirY[i] * step);

                // ����Ƿ�Խ��
                if (!boardState.IsWithinBounds(nextPos))
                    break; // �����߽磬����һ������

                Piece targetPiece = boardState.GetPieceAt(nextPos);

                if (targetPiece.Type == PieceType.None)
                {
                    // Ŀ��λ���ǿյģ������ƶ�
                    moves.Add(nextPos);
                }
                else
                {
                    // Ŀ��λ��������
                    if (targetPiece.Color != piece.Color)
                    {
                        // �ǵз����ӣ����ԳԵ���Ȼ��ֹͣ������������
                        moves.Add(nextPos);
                    }
                    // ������ѷ����ӣ������ƶ�����λ�ã�Ҳֹͣ������������
                    break; 
                }
            }
        }
        return moves;
    }
}