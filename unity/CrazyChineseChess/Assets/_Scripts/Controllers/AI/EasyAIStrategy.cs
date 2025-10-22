// File: _Scripts/Controllers/AI/EasyAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class EasyAIStrategy : IAIStrategy
{
    public Vector2 DecisionTimeRange => new Vector2(0.5f, 4.0f);
    protected System.Random _random = new System.Random();

    public virtual AIController.MovePlan FindBestMove(PlayerColor assignedColor, BoardState logicalBoard,
                                                      List<GameManager.SimulatedPiece> myPieces,
                                                      List<GameManager.SimulatedPiece> opponentPieces)
    {
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            if (_random.NextDouble() < 0.8f)
            {
                var savingMove = FindSavingMove(assignedColor, logicalBoard, myPieces, kingPos);
                if (savingMove != null) return savingMove;
            }
        }

        double intentionRoll = _random.NextDouble();
        AIController.MovePlan chosenMove = null;

        if (intentionRoll < 0.5f) chosenMove = FindAttackMove(assignedColor, logicalBoard, myPieces);
        if (chosenMove == null && intentionRoll < 0.8f) chosenMove = FindEvadeMove(assignedColor, logicalBoard, myPieces);
        if (chosenMove == null) chosenMove = FindRandomMove(assignedColor, logicalBoard, myPieces);

        return chosenMove;
    }

    protected Vector2Int FindKingPosition(BoardState boardState, PlayerColor kingColor)
    {
        foreach (var pieceInfo in boardState.GetAllPieces())
        {
            if (pieceInfo.PieceData.Type == PieceType.General && pieceInfo.PieceData.Color == kingColor)
                return pieceInfo.Position;
        }
        return new Vector2Int(-1, -1);
    }

    protected AIController.MovePlan FindSavingMove(PlayerColor color, BoardState board, List<GameManager.SimulatedPiece> pieces, Vector2Int kingPos)
    {
        GameManager.SimulatedPiece kingPiece = pieces.FirstOrDefault(p => p.PieceData.Type == PieceType.General);
        if (kingPiece.Equals(default(GameManager.SimulatedPiece))) return null;

        PlayerColor opponentColor = (color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        var validKingMoves = RuleEngine.GetValidMoves(kingPiece.PieceData, kingPos, board);
        var safeKingMoves = validKingMoves.Where(move => !RuleEngine.IsPositionUnderAttack(move, opponentColor, board)).ToList();

        if (safeKingMoves.Count > 0)
        {
            return new AIController.MovePlan(kingPiece.PieceData, kingPos, safeKingMoves[_random.Next(0, safeKingMoves.Count)], 1000);
        }
        return null;
    }

    protected AIController.MovePlan FindAttackMove(PlayerColor color, BoardState board, List<GameManager.SimulatedPiece> pieces)
    {
        var captureMoves = new List<AIController.MovePlan>();
        foreach (var piece in pieces)
        {
            var validTargets = RuleEngine.GetValidMoves(piece.PieceData, piece.BoardPosition, board);
            foreach (var targetPos in validTargets)
            {
                Piece targetPiece = board.GetPieceAt(targetPos);
                if (targetPiece.Type != PieceType.None && targetPiece.Color != color)
                {
                    captureMoves.Add(new AIController.MovePlan(piece.PieceData, piece.BoardPosition, targetPos, 0));
                }
            }
        }
        if (captureMoves.Count > 0) return captureMoves[_random.Next(0, captureMoves.Count)];
        return null;
    }

    protected AIController.MovePlan FindEvadeMove(PlayerColor color, BoardState board, List<GameManager.SimulatedPiece> pieces)
    {
        PlayerColor opponentColor = (color == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
        var piecesInDanger = pieces.Where(p => RuleEngine.IsPositionUnderAttack(p.BoardPosition, opponentColor, board)).ToList();

        if (piecesInDanger.Count > 0)
        {
            var pieceToSave = piecesInDanger[_random.Next(0, piecesInDanger.Count)];
            var validTargets = RuleEngine.GetValidMoves(pieceToSave.PieceData, pieceToSave.BoardPosition, board);
            var safeMoves = validTargets.Where(targetPos => !RuleEngine.IsPositionUnderAttack(targetPos, opponentColor, board)).ToList();

            if (safeMoves.Count > 0)
            {
                return new AIController.MovePlan(pieceToSave.PieceData, pieceToSave.BoardPosition, safeMoves[_random.Next(0, safeMoves.Count)], 0);
            }
        }
        return null;
    }

    protected AIController.MovePlan FindRandomMove(PlayerColor color, BoardState board, List<GameManager.SimulatedPiece> pieces)
    {
        var allMoves = GetAllPossibleMovesFromSimulated(color, board, pieces);
        if (allMoves.Count > 0) return allMoves[_random.Next(0, allMoves.Count)];
        return null;
    }

    protected List<AIController.MovePlan> GetAllPossibleMovesFromSimulated(PlayerColor color, BoardState board, List<GameManager.SimulatedPiece> pieces)
    {
        var allMoves = new List<AIController.MovePlan>();
        foreach (var piece in pieces)
        {
            var validTargets = RuleEngine.GetValidMoves(piece.PieceData, piece.BoardPosition, board);
            foreach (var targetPos in validTargets)
            {
                int targetValue = 0;
                Piece targetPiece = board.GetPieceAt(targetPos);
                if (targetPiece.Type != PieceType.None && targetPiece.Color != color)
                {
                    targetValue = PieceValue.GetValue(targetPiece.Type);
                }
                allMoves.Add(new AIController.MovePlan(piece.PieceData, piece.BoardPosition, targetPos, targetValue));
            }
        }
        return allMoves;
    }
}