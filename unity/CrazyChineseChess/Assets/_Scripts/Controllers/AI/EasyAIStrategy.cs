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
        if (allMoves.Count == 0) return null;

        var weightedMoves = new List<(AIController.MovePlan move, float weight)>();
        float totalWeight = 0f;

        foreach (var move in allMoves)
        {
            float weight = 2.0f; // 基础权重

            // 1. 棋子类型权重：优先移动进攻性棋子
            switch (move.PieceToMoveData.Type)
            {
                case PieceType.Chariot:
                    weight += 6.0f;
                    break;
                case PieceType.Horse:
                    weight += 5.0f;
                    break;
                case PieceType.Cannon:
                    weight += 4.0f;
                    break;
                case PieceType.Soldier:
                    weight += 3.0f;
                    break;
                    // 士、象、将的移动权重较低
            }

            // 2. 方向权重：优先向前移动（向敌方半场）
            int moveDirectionY = move.To.y - move.From.y;
            if (color == PlayerColor.Black && moveDirectionY < 0) // 黑方往下走是向前
            {
                weight += 3.0f;
            }
            else if (color == PlayerColor.Red && moveDirectionY > 0) // 红方往上走是向前
            {
                weight += 3.0f;
            }

            // 3. 避免在己方底线附近无意义移动 (特别是马)
            if (move.PieceToMoveData.Type == PieceType.Horse)
            {
                bool isAtBottomForBlack = color == PlayerColor.Black && move.From.y >= 7;
                bool isAtBottomForRed = color == PlayerColor.Red && move.From.y <= 2;
                if (isAtBottomForBlack || isAtBottomForRed)
                {
                    // 如果马在己方底线附近，并且目标点仍然在底线附近，则降低权重
                    bool targetIsAtBottomForBlack = color == PlayerColor.Black && move.To.y >= 7;
                    bool targetIsAtBottomForRed = color == PlayerColor.Red && move.To.y <= 2;
                    if (targetIsAtBottomForBlack || targetIsAtBottomForRed)
                    {
                        weight *= 0.1f; // 权重降为原来的10%
                    }
                }
            }

            weightedMoves.Add((move, weight));
            totalWeight += weight;
        }

        // 4. 根据权重进行随机选择
        float randomValue = (float)(_random.NextDouble() * totalWeight);
        foreach (var weightedMove in weightedMoves)
        {
            randomValue -= weightedMove.weight;
            if (randomValue <= 0)
            {
                return weightedMove.move;
            }
        }

        // 如果因为浮点数精度问题没选到，就返回最后一个
        return allMoves.LastOrDefault();

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