// File: _Scripts/Controllers/AI/HardAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class HardAIStrategy : EasyAIStrategy, IAIStrategy
{
    public new Vector2 DecisionTimeRange => new Vector2(0.5f, 3.0f);

    // 设为 protected 以便子类访问
    protected new System.Random _random = new System.Random();

    protected const int CAPTURE_MULTIPLIER = 10;
    protected const int THREAT_MULTIPLIER = 2;
    protected const int SAVING_MULTIPLIER = 8;
    protected const int CENTER_CONTROL_BONUS = 5;
    protected const int SAFE_MOVE_BONUS = 2;

    public override AIController.MovePlan FindBestMove(PlayerColor assignedColor, BoardState logicalBoard,
                                                        List<GameManager.SimulatedPiece> myPieces,
                                                        List<GameManager.SimulatedPiece> opponentPieces)
    {
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            if (_random.NextDouble() < 0.9f)
            {
                var savingMove = FindSavingMove(assignedColor, logicalBoard, myPieces, kingPos);
                if (savingMove != null) return savingMove;
            }
        }

        var allMoves = GetAllPossibleMovesFromSimulated(assignedColor, logicalBoard, myPieces);
        if (allMoves.Count == 0) return null;

        int highestScore = int.MinValue;
        var bestMoves = new List<AIController.MovePlan>();

        foreach (var move in allMoves)
        {
            int score = EvaluateMove(move, assignedColor, logicalBoard);
            if (score > highestScore)
            {
                highestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (score == highestScore)
            {
                bestMoves.Add(move);
            }
        }

        if (bestMoves.Count > 0) return bestMoves[_random.Next(0, bestMoves.Count)];
        return null;
    }

    protected virtual int EvaluateMove(AIController.MovePlan move, PlayerColor myColor, BoardState board)
    {
        PlayerColor opponentColor = (myColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        int score = 0;
        score += move.TargetValue * CAPTURE_MULTIPLIER;
        BoardState futureBoard = board.Clone();
        futureBoard.MovePiece(move.From, move.To);

        var newThreats = RuleEngine.GetValidMoves(move.PieceToMoveData, move.To, futureBoard);
        foreach (var threatenedPos in newThreats)
        {
            Piece threatenedPiece = futureBoard.GetPieceAt(threatenedPos);
            if (threatenedPiece.Type != PieceType.None && threatenedPiece.Color == opponentColor)
                score += PieceValue.GetValue(threatenedPiece.Type) * THREAT_MULTIPLIER;
        }

        if (RuleEngine.IsPositionUnderAttack(move.To, opponentColor, futureBoard))
        {
            int myValue = PieceValue.GetValue(move.PieceToMoveData.Type);
            score -= myValue * CAPTURE_MULTIPLIER;
        }
        else score += SAFE_MOVE_BONUS;

        if (RuleEngine.IsPositionUnderAttack(move.From, opponentColor, board))
        {
            int myValue = PieceValue.GetValue(move.PieceToMoveData.Type);
            score += myValue * SAVING_MULTIPLIER;
        }

        score += GetPositionalValue(move.PieceToMoveData, move.To, myColor);
        return score;
    }

    protected virtual int GetPositionalValue(Piece pieceData, Vector2Int pos, PlayerColor myColor)
    {
        int value = 0;
        if (pos.x >= 3 && pos.x <= 5) value += CENTER_CONTROL_BONUS;
        if (pieceData.Type == PieceType.Soldier)
        {
            if (myColor == PlayerColor.Red && pos.y > 4) value += pos.y * 3;
            else if (myColor == PlayerColor.Black && pos.y < 5) value += (9 - pos.y) * 3;
        }
        return value;
    }
}