// File: _Scripts/GameManager.cs

using UnityEngine;
using System; // <--- ��������һ��

/// <summary>
/// ��Ϸ�ܹ�������Э�����к��������
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public BoardState CurrentBoardState { get; private set; }
    public GameModeController CurrentGameMode { get; private set; }
    public EnergySystem EnergySystem { get; private set; }

    private BoardRenderer boardRenderer;
    private bool isGameEnded = false;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
    }

    void Start()
    {
        boardRenderer = FindObjectOfType<BoardRenderer>();
        if (boardRenderer == null)
        {
            Debug.LogError("�������Ҳ��� BoardRenderer!");
            return;
        }

        CurrentBoardState = new BoardState();
        CurrentBoardState.InitializeDefaultSetup();
        EnergySystem = new EnergySystem();

        switch (GameModeSelector.SelectedMode)
        {
            case GameModeType.TurnBased:
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                Debug.Log("��Ϸ��ʼ���ѽ��롾��ͳ�غ��ơ�ģʽ��");
                break;
            case GameModeType.RealTime:
                CurrentGameMode = new RealTimeModeController(this, CurrentBoardState, boardRenderer, EnergySystem);
                Debug.Log("��Ϸ��ʼ���ѽ��롾ʵʱ��ս��ģʽ��");
                break;
            default:
                CurrentGameMode = new TurnBasedModeController(this, CurrentBoardState, boardRenderer);
                break;
        }

        boardRenderer.RenderBoard(CurrentBoardState);

        // Ȼ�������ǰ��ʵʱģʽ�������ٽ���ʵʱ״̬�ĳ�ʼ��
        if (CurrentGameMode is RealTimeModeController rtController)
        {
            rtController.InitializeRealTimeStates();
        }

    }

    private void Update()
    {
        if (isGameEnded) return;

        if (GameModeSelector.SelectedMode == GameModeType.RealTime)
        {
            EnergySystem?.Tick();
            if (CurrentGameMode is RealTimeModeController rtController)
            {
                rtController.Tick();
            }
        }
    }

    // ������������һ�������ص������ذ汾��������ô��ص��İ汾��������null
    // �����غ���ģʽ�Ĵ���������κθĶ�
    public void ExecuteMove(Vector2Int from, Vector2Int to)
    {
        ExecuteMove(from, to, null, null);
    }


    public void ExecuteMove(Vector2Int from, Vector2Int to, Action<PieceComponent, float> onProgressUpdate = null, Action<PieceComponent> onComplete = null)
    {
        if (isGameEnded) return;

        Piece targetPiece = CurrentBoardState.GetPieceAt(to);
        bool isCapture = targetPiece.Type != PieceType.None;

        if (isCapture)
        {
            boardRenderer.RemovePieceAt(to);
            if (targetPiece.Type == PieceType.General)
            {
                GameStatus status = (targetPiece.Color == PlayerColor.Black) ? GameStatus.RedWin : GameStatus.BlackWin;
                CurrentBoardState.MovePiece(from, to);
                boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture, onProgressUpdate, onComplete);
                HandleEndGame(status);
                return;
            }
        }

        CurrentBoardState.MovePiece(from, to);
        boardRenderer.MovePiece(from, to, CurrentBoardState, isCapture, onProgressUpdate, onComplete);
        CheckForCheck();
    }

    private void CheckForCheck()
    {
        if (CurrentGameMode is TurnBasedModeController turnBasedMode)
        {
            PlayerColor nextPlayer = turnBasedMode.GetCurrentPlayer();
            if (RuleEngine.IsKingInCheck(nextPlayer, CurrentBoardState))
            {
                Debug.Log($"������{nextPlayer} ����������������");
            }
        }
    }

    private void HandleEndGame(GameStatus status)
    {
        isGameEnded = true;
        Debug.Log($"��Ϸ���������: {status}");
        var playerInput = FindObjectOfType<PlayerInput>();
        if (playerInput != null) playerInput.enabled = false;
    }
}