// File: _Scripts/Controllers/AIController.cs

using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class AIController : MonoBehaviour, IPlayerController
{
    private PlayerColor assignedColor;
    private GameManager gameManager;
    private float decisionTimer;
    private IAIStrategy strategy;
    private Vector2 decisionTimeRange;
    private bool isSetup = false;
    private bool isThinking = false;

    public class MovePlan
    {
        public Piece PieceToMoveData;
        public Vector2Int From;
        public Vector2Int To;
        public int TargetValue;

        public MovePlan(Piece pieceData, Vector2Int from, Vector2Int to, int value)
        {
            this.PieceToMoveData = pieceData;
            this.From = from;
            this.To = to;
            this.TargetValue = value;
        }
    }

    public void Initialize(PlayerColor color, GameManager manager)
    {
        this.assignedColor = color;
        this.gameManager = manager;
    }

    public void SetupAI(IAIStrategy aiStrategy)
    {
        this.strategy = aiStrategy;
        this.decisionTimeRange = aiStrategy.DecisionTimeRange;
        ResetDecisionTimer();
        isSetup = true;
        Debug.Log($"[AIController] AI控制器已为 {assignedColor} 方配置完成，使用策略: {aiStrategy.GetType().Name}，决策频率: {decisionTimeRange.x}-{decisionTimeRange.y}s。");
    }

    private void Update()
    {
        if (!isSetup || gameManager == null || gameManager.IsGameEnded || isThinking) return;

        decisionTimer -= Time.deltaTime;
        if (decisionTimer <= 0)
        {
            ResetDecisionTimer();
            MakeDecisionAsync();
        }
    }

    private void ResetDecisionTimer()
    {
        if (decisionTimeRange == Vector2.zero) return;
        decisionTimer = Random.Range(decisionTimeRange.x, decisionTimeRange.y);
    }

    private async void MakeDecisionAsync()
    {
        if (!gameManager.CanSpendEnergy(assignedColor)) return;

        isThinking = true;
        MovePlan bestMove = null;

        if (strategy is VeryHardAIStrategy vhStrategy)
        {
            bestMove = vhStrategy.TryGetOpeningBookMove(gameManager, assignedColor);
        }

        if (bestMove == null)
        {
            BoardState logicalBoard = gameManager.GetLogicalBoardState();
            PlayerColor opponentColor = (assignedColor == PlayerColor.Red) ? PlayerColor.Black : PlayerColor.Red;
            List<GameManager.SimulatedPiece> myPieces = gameManager.GetSimulatedPiecesOfColorFromBoard(assignedColor, logicalBoard);
            List<GameManager.SimulatedPiece> opponentPieces = gameManager.GetSimulatedPiecesOfColorFromBoard(opponentColor, logicalBoard);

            Debug.Log("[AI] 开始在后台线程思考...");
            bestMove = await Task.Run(() => strategy.FindBestMove(assignedColor, logicalBoard, myPieces, opponentPieces));
        }
        else
        {
            Debug.Log("[AI] 使用开局库移动，跳过深度思考。");
        }

        if (this == null || gameManager.IsGameEnded)
        {
            isThinking = false;
            return;
        }

        if (bestMove != null)
        {
            Debug.Log($"[AI] 思考完成: 移动 {bestMove.PieceToMoveData.Type} 从 {bestMove.From} 到 {bestMove.To}。");
            gameManager.RequestMove(assignedColor, bestMove.From, bestMove.To);
        }

        isThinking = false;
    }
}