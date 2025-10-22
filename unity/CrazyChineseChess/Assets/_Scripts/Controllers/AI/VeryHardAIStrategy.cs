// File: _Scripts/Controllers/AI/VeryHardAIStrategy.cs

using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class VeryHardAIStrategy : HardAIStrategy, IAIStrategy
{
    public new Vector2 DecisionTimeRange => new Vector2(0.5f, 2.0f);
    private System.Random _random = new System.Random();

    private static List<List<Vector2Int>> openingBook;
    private List<Vector2Int> currentOpening;
    private int openingMoveIndex = 0;
    private bool useOpeningBook = true;
    private const int SEARCH_DEPTH = 2;

    public VeryHardAIStrategy()
    {
        InitializeOpeningBook();
        SelectRandomOpening();
    }

    public AIController.MovePlan TryGetOpeningBookMove(GameManager gameManager, PlayerColor assignedColor)
    {
        if (!useOpeningBook || currentOpening == null || openingMoveIndex >= currentOpening.Count) return null;

        Vector2Int from = currentOpening[openingMoveIndex];
        Vector2Int to = currentOpening[openingMoveIndex + 1];
        PieceComponent pieceToMove = gameManager.BoardRenderer.GetPieceComponentAt(from);

        if (pieceToMove != null && pieceToMove.PieceData.Color == assignedColor && RuleEngine.GetValidMoves(pieceToMove.PieceData, from, gameManager.GetLogicalBoardState()).Contains(to))
        {
            openingMoveIndex += 2;
            return new AIController.MovePlan(pieceToMove.PieceData, from, to, 0);
        }
        else
        {
            useOpeningBook = false;
            return null;
        }
    }

    public override AIController.MovePlan FindBestMove(PlayerColor assignedColor, BoardState logicalBoard,
                                                        List<GameManager.SimulatedPiece> myPieces,
                                                        List<GameManager.SimulatedPiece> opponentPieces)
    {
        PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;

        Vector2Int kingPos = FindKingPosition(logicalBoard, assignedColor);
        if (kingPos != new Vector2Int(-1, -1) && RuleEngine.IsPositionUnderAttack(kingPos, opponentColor, logicalBoard))
        {
            // 100% 救驾
            return FindSavingMove(assignedColor, logicalBoard, myPieces, kingPos);
        }

        return FindBestMoveWithMinimax(assignedColor, logicalBoard, myPieces, opponentPieces);
    }

    private AIController.MovePlan FindBestMoveWithMinimax(PlayerColor assignedColor, BoardState logicalBoard,
                                                           List<GameManager.SimulatedPiece> myPieces,
                                                           List<GameManager.SimulatedPiece> opponentPieces)
    {
        var allMoves = GetAllPossibleMovesFromSimulated(assignedColor, logicalBoard, myPieces);
        if (allMoves.Count == 0) return null;

        int bestScore = int.MinValue;
        var bestMoves = new List<AIController.MovePlan>();

        foreach (var move in allMoves)
        {
            BoardState futureBoard = logicalBoard.Clone();
            futureBoard.MovePiece(move.From, move.To);
            int score = Minimax(futureBoard, SEARCH_DEPTH - 1, false, assignedColor);

            if (score > bestScore)
            {
                bestScore = score;
                bestMoves.Clear();
                bestMoves.Add(move);
            }
            else if (score == bestScore)
            {
                bestMoves.Add(move);
            }
        }

        if (bestMoves.Count > 0) return bestMoves[_random.Next(0, bestMoves.Count)];
        return null;
    }

    private int Minimax(BoardState board, int depth, bool isMaximizingPlayer, PlayerColor myColor)
    {
        if (depth == 0) return EvaluateBoardState(board, myColor);

        PlayerColor currentColor = isMaximizingPlayer ? myColor : (myColor == PlayerColor.Red ? PlayerColor.Black : PlayerColor.Red);
        var pieces = GetSimulatedPiecesFromBoard(currentColor, board);
        var allMoves = GetAllPossibleMovesFromSimulated(currentColor, board, pieces);

        if (allMoves.Count == 0) return EvaluateBoardState(board, myColor);

        int bestValue = isMaximizingPlayer ? int.MinValue : int.MaxValue;
        foreach (var move in allMoves)
        {
            BoardState futureBoard = board.Clone();
            futureBoard.MovePiece(move.From, move.To);
            int eval = Minimax(futureBoard, depth - 1, !isMaximizingPlayer, myColor);
            if (isMaximizingPlayer) bestValue = Mathf.Max(bestValue, eval);
            else bestValue = Mathf.Min(bestValue, eval);
        }
        return bestValue;
    }

    private int EvaluateBoardState(BoardState board, PlayerColor myColor)
    {
        int totalScore = 0;
        foreach (var pieceInfo in board.GetAllPieces())
        {
            int pieceScore = PieceValue.GetValue(pieceInfo.PieceData.Type) + GetPositionalValue(pieceInfo.PieceData, pieceInfo.Position, pieceInfo.PieceData.Color);
            if (pieceInfo.PieceData.Color == myColor) totalScore += pieceScore;
            else totalScore -= pieceScore;
        }
        return totalScore;
    }

    private List<GameManager.SimulatedPiece> GetSimulatedPiecesFromBoard(PlayerColor color, BoardState board)
    {
        var pieces = new List<GameManager.SimulatedPiece>();
        foreach (var pieceInfo in board.GetAllPieces())
        {
            if (pieceInfo.PieceData.Color == color)
            {
                pieces.Add(new GameManager.SimulatedPiece { PieceData = pieceInfo.PieceData, BoardPosition = pieceInfo.Position });
            }
        }
        return pieces;
    }

    private void InitializeOpeningBook()
    {
        if (openingBook != null) return;
        openingBook = new List<List<Vector2Int>>
        {
            new List<Vector2Int> { new Vector2Int(1, 7), new Vector2Int(4, 7), new Vector2Int(1, 9), new Vector2Int(2, 7) },
            new List<Vector2Int> { new Vector2Int(2, 9), new Vector2Int(4, 7), new Vector2Int(7, 9), new Vector2Int(6, 7) },
            new List<Vector2Int> { new Vector2Int(1, 9), new Vector2Int(2, 7) },
            new List<Vector2Int>()
        };
    }

    private void SelectRandomOpening()
    {
        if (openingBook == null || openingBook.Count == 0)
        {
            useOpeningBook = false;
            currentOpening = new List<Vector2Int>();
            return;
        }
        currentOpening = openingBook[_random.Next(0, openingBook.Count)];
        Debug.Log($"[AI-VeryHard] 已选择开局库，共 {currentOpening.Count / 2} 步。");
    }
}