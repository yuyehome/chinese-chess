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
        Debug.Log($"[AIController] AI��������Ϊ {assignedColor} ��������ɣ�ʹ�ò���: {aiStrategy.GetType().Name}������Ƶ��: {decisionTimeRange.x}-{decisionTimeRange.y}s��");
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

            Debug.Log("[AI] ��ʼ�ں�̨�߳�˼��...");
            bestMove = await Task.Run(() => strategy.FindBestMove(assignedColor, logicalBoard, myPieces, opponentPieces));
        }
        else
        {
            Debug.Log("[AI] ʹ�ÿ��ֿ��ƶ����������˼����");
        }

        if (this == null || gameManager.IsGameEnded)
        {
            isThinking = false;
            return;
        }

        if (bestMove != null)
        {
            Debug.Log($"[AI] ˼�����: �ƶ� {bestMove.PieceToMoveData.Type} �� {bestMove.From} �� {bestMove.To}��");
            gameManager.RequestMove(assignedColor, bestMove.From, bestMove.To);
        }

        isThinking = false;
    }
}